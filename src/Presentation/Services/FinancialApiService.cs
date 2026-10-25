using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;

namespace Presentation.Services;

public sealed class FinancialApiService(FinancialDbContext db) : IFinancialApiService
{
    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        return Map(user);
    }

    public async Task<UserResponse> UpdateCurrentUserAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        user.ChangeDisplayName(request.DisplayName);
        await db.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken ct) =>
        (await db.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct)).Select(Map).ToList();

    public async Task<IReadOnlyList<HouseholdResponse>> ListHouseholdsAsync(Guid userId, CancellationToken ct)
    {
        var households = await db.HouseholdMembers.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == MembershipStatus.Active)
            .Join(db.Households.AsNoTracking(), member => member.HouseholdId, household => household.Id,
                (_, household) => household)
            .Where(x => x.ArchivedAt == null)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return households.Select(Map).ToList();
    }

    public async Task<HouseholdResponse> GetHouseholdAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        return Map(await FindHouseholdAsync(householdId, ct));
    }

    public async Task<HouseholdResponse> CreateHouseholdAsync(Guid userId, CreateHouseholdRequest request, CancellationToken ct)
    {
        var household = new Household(request.Name, new CurrencyCode(request.CurrencyCode), request.TimeZone, userId);
        var owner = new HouseholdMember(household.Id, userId, HouseholdRole.Owner);
        await db.Households.AddAsync(household, ct);
        await db.HouseholdMembers.AddAsync(owner, ct);
        await db.SaveChangesAsync(ct);
        return Map(household);
    }

    public async Task<HouseholdResponse> UpdateHouseholdAsync(Guid userId, Guid householdId, UpdateHouseholdRequest request, CancellationToken ct)
    {
        _ = await RequireManagerAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        household.Rename(request.Name);
        await db.SaveChangesAsync(ct);
        return Map(household);
    }

    public async Task ArchiveHouseholdAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        _ = await RequireOwnerAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        household.Archive(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<HouseholdMemberResponse>> ListMembersAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var rows = await db.HouseholdMembers.AsNoTracking()
            .Where(x => x.HouseholdId == householdId && x.Status == MembershipStatus.Active)
            .Join(db.Users.AsNoTracking(), member => member.UserId, user => user.Id,
                (member, user) => new { Member = member, User = user })
            .ToListAsync(ct);
        return rows.OrderBy(x => x.User.DisplayName).Select(x => Map(x.Member, x.User)).ToList();
    }

    public async Task<HouseholdMemberResponse> AddMemberAsync(Guid userId, Guid householdId, AddHouseholdMemberRequest request, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        if (user.Status != UserStatus.Active)
            throw new InvalidOperationException("Only active users can join a household.");
        if (!Enum.TryParse<HouseholdRole>(request.Role, true, out var role) ||
            role == HouseholdRole.Owner)
            throw new ArgumentException("New members may be assigned Admin or Member.");

        var existing = await db.HouseholdMembers.SingleOrDefaultAsync(
            x => x.HouseholdId == householdId && x.UserId == request.UserId, ct);
        if (existing is { Status: MembershipStatus.Active })
            throw new InvalidOperationException("User is already an active member.");
        if (existing is not null)
            throw new InvalidOperationException("Removed memberships cannot be reactivated; invitation support is required.");

        var membership = new HouseholdMember(householdId, user.Id, role);
        await db.HouseholdMembers.AddAsync(membership, ct);
        await db.SaveChangesAsync(ct);
        return Map(membership, user);
    }

    public async Task<HouseholdMemberResponse> UpdateMemberAsync(Guid userId, Guid householdId, Guid memberId, UpdateHouseholdMemberRequest request, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var member = await FindMemberAsync(householdId, memberId, ct);
        if (!Enum.TryParse<HouseholdRole>(request.Role, true, out var role) || role == HouseholdRole.Owner)
            throw new ArgumentException("Role must be Admin or Member.");
        if (member.Role == HouseholdRole.Owner)
            throw new InvalidOperationException("Transfer household ownership before changing the owner's role.");
        member.ChangeRole(role);
        await db.SaveChangesAsync(ct);
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == member.UserId, ct);
        return Map(member, user);
    }

    public async Task RemoveMemberAsync(Guid userId, Guid householdId, Guid memberId, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var member = await FindMemberAsync(householdId, memberId, ct);
        if (member.Role == HouseholdRole.Owner)
            throw new InvalidOperationException("Transfer household ownership before removing the owner.");
        member.Remove(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var categories = await db.Categories.AsNoTracking().Where(x => x.HouseholdId == householdId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);
        return categories.Select(Map).ToList();
    }

    public async Task<CategoryResponse> CreateCategoryAsync(Guid userId, Guid householdId, SaveCategoryRequest request, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var kind = ParseEnum<CategoryKind>(request.Kind);
        await ValidateCategoryParentAsync(householdId, request.ParentCategoryId, kind, null, ct);
        var category = new Category(householdId, request.Name, kind, request.ParentCategoryId, request.SortOrder);
        await db.Categories.AddAsync(category, ct);
        await db.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid userId, Guid householdId, Guid categoryId, SaveCategoryRequest request, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var category = await FindCategoryAsync(householdId, categoryId, ct);
        var kind = ParseEnum<CategoryKind>(request.Kind);
        if (category.Kind != kind)
            throw new InvalidOperationException("Category type cannot be changed.");
        await ValidateCategoryParentAsync(householdId, request.ParentCategoryId, kind, categoryId, ct);
        category.Rename(request.Name);
        category.Reorder(request.SortOrder);
        category.SetParent(request.ParentCategoryId);
        await db.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task DeactivateCategoryAsync(Guid userId, Guid householdId, Guid categoryId, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        (await FindCategoryAsync(householdId, categoryId, ct)).Deactivate();
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ExpenseResponse>> ListExpensesAsync(Guid userId, Guid householdId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        ValidateDateRange(from, to);
        var query = db.Expenses.AsNoTracking().Where(x => x.HouseholdId == householdId && x.DeletedAt == null);
        if (from.HasValue) query = query.Where(x => x.OccurredOn >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredOn <= to.Value);
        return (await query.OrderByDescending(x => x.OccurredOn).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<ExpenseResponse> GetExpenseAsync(Guid userId, Guid householdId, Guid expenseId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var item = await db.Expenses.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == expenseId && x.HouseholdId == householdId && x.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Expense not found.");
        return Map(item);
    }

    public async Task<ExpenseResponse> CreateExpenseAsync(Guid userId, Guid householdId, SaveExpenseRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        var category = await RequireCategoryKindAsync(householdId, request.CategoryId, CategoryKind.Expense, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        var entity = new Expense(householdId, category.Id, member.Id,
            new Money(request.Amount, household.Currency), request.OccurredOn,
            request.Description, request.Merchant, request.Notes);
        await db.Expenses.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ExpenseResponse> UpdateExpenseAsync(Guid userId, Guid householdId, Guid expenseId, SaveExpenseRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var entity = await FindExpenseAsync(householdId, expenseId, ct);
        EnsureCreatorOrManager(member, entity.CreatedByMemberId);
        var household = await FindHouseholdAsync(householdId, ct);
        var category = await RequireCategoryKindAsync(householdId, request.CategoryId, CategoryKind.Expense, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        entity.Update(category.Id, new Money(request.Amount, household.Currency), request.OccurredOn,
            request.Description, request.Merchant, request.Notes);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteExpenseAsync(Guid userId, Guid householdId, Guid expenseId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var entity = await FindExpenseAsync(householdId, expenseId, ct);
        EnsureCreatorOrManager(member, entity.CreatedByMemberId);
        entity.Delete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IncomeResponse>> ListIncomesAsync(Guid userId, Guid householdId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        ValidateDateRange(from, to);
        var query = db.Incomes.AsNoTracking().Where(x => x.HouseholdId == householdId && x.DeletedAt == null);
        if (from.HasValue) query = query.Where(x => x.OccurredOn >= from.Value);
        if (to.HasValue) query = query.Where(x => x.OccurredOn <= to.Value);
        return (await query.OrderByDescending(x => x.OccurredOn).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<IncomeResponse> GetIncomeAsync(Guid userId, Guid householdId, Guid incomeId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var item = await db.Incomes.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == incomeId && x.HouseholdId == householdId && x.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Income not found.");
        return Map(item);
    }

    public async Task<IncomeResponse> CreateIncomeAsync(Guid userId, Guid householdId, SaveIncomeRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        var category = await RequireCategoryKindAsync(householdId, request.CategoryId, CategoryKind.Income, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        var entity = new Income(householdId, category.Id, member.Id,
            new Money(request.Amount, household.Currency), request.OccurredOn,
            request.Description, request.Source, request.Notes);
        await db.Incomes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IncomeResponse> UpdateIncomeAsync(Guid userId, Guid householdId, Guid incomeId, SaveIncomeRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var entity = await FindIncomeAsync(householdId, incomeId, ct);
        EnsureCreatorOrManager(member, entity.CreatedByMemberId);
        var household = await FindHouseholdAsync(householdId, ct);
        var category = await RequireCategoryKindAsync(householdId, request.CategoryId, CategoryKind.Income, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        entity.Update(category.Id, new Money(request.Amount, household.Currency), request.OccurredOn,
            request.Description, request.Source, request.Notes);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteIncomeAsync(Guid userId, Guid householdId, Guid incomeId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var entity = await FindIncomeAsync(householdId, incomeId, ct);
        EnsureCreatorOrManager(member, entity.CreatedByMemberId);
        entity.Delete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BudgetResponse>> ListBudgetsAsync(Guid userId, Guid householdId, int year, int month, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var period = new MonthPeriod(year, month);
        var budgets = (await db.Budgets.AsNoTracking()
            .Where(x => x.HouseholdId == householdId).ToListAsync(ct))
            .Where(x => x.Period.Year == year && x.Period.Month == month)
            .ToList();
        var result = new List<BudgetResponse>(budgets.Count);
        foreach (var budget in budgets)
        {
            var spent = await db.Expenses.AsNoTracking()
                .Where(x => x.HouseholdId == householdId && x.CategoryId == budget.CategoryId &&
                            x.DeletedAt == null && x.OccurredOn >= period.StartDate && x.OccurredOn <= period.EndDate)
                .SumAsync(x => (decimal?)x.Amount.Amount, ct) ?? 0m;
            result.Add(Map(budget, spent));
        }
        return result;
    }

    public async Task<BudgetResponse> CreateBudgetAsync(Guid userId, Guid householdId, SaveBudgetRequest request, CancellationToken ct)
    {
        var member = await RequireManagerAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        var category = await RequireCategoryKindAsync(householdId, request.CategoryId, CategoryKind.Expense, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        var period = new MonthPeriod(request.Year, request.Month);
        var existingBudgets = await db.Budgets.AsNoTracking()
            .Where(x => x.HouseholdId == householdId && x.CategoryId == category.Id)
            .ToListAsync(ct);
        if (existingBudgets.Any(x => x.Period.Year == request.Year && x.Period.Month == request.Month))
            throw new InvalidOperationException("A budget already exists for this category and month.");
        var budget = new Budget(householdId, category.Id, period,
            new Money(request.AmountLimit, household.Currency),
            new Percentage(request.AlertThresholdPercent), member.Id);
        await db.Budgets.AddAsync(budget, ct);
        await db.SaveChangesAsync(ct);
        return Map(budget, 0);
    }

    public async Task<BudgetResponse> UpdateBudgetAsync(Guid userId, Guid householdId, Guid budgetId, SaveBudgetRequest request, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var budget = await db.Budgets.SingleOrDefaultAsync(x => x.Id == budgetId && x.HouseholdId == householdId, ct)
            ?? throw new KeyNotFoundException("Budget not found.");
        if (budget.CategoryId != request.CategoryId || budget.Period.Year != request.Year || budget.Period.Month != request.Month)
            throw new InvalidOperationException("Budget category and period cannot be changed.");
        var household = await FindHouseholdAsync(householdId, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        budget.Revise(new Money(request.AmountLimit, household.Currency), new Percentage(request.AlertThresholdPercent));
        await db.SaveChangesAsync(ct);
        var spent = await db.Expenses.AsNoTracking().Where(x => x.HouseholdId == householdId &&
            x.CategoryId == budget.CategoryId && x.DeletedAt == null &&
            x.OccurredOn >= budget.Period.StartDate && x.OccurredOn <= budget.Period.EndDate)
            .SumAsync(x => (decimal?)x.Amount.Amount, ct) ?? 0m;
        return Map(budget, spent);
    }

    public async Task DeleteBudgetAsync(Guid userId, Guid householdId, Guid budgetId, CancellationToken ct)
    {
        await RequireManagerAsync(userId, householdId, ct);
        var budget = await db.Budgets.SingleOrDefaultAsync(x => x.Id == budgetId && x.HouseholdId == householdId, ct)
            ?? throw new KeyNotFoundException("Budget not found.");
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SavingsGoalResponse>> ListSavingsGoalsAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        var goals = await db.SavingsGoals.AsNoTracking().Where(x => x.HouseholdId == householdId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var result = new List<SavingsGoalResponse>(goals.Count);
        foreach (var goal in goals)
        {
            var contributed = await SumContributionsAsync(householdId, goal.Id, ct);
            result.Add(Map(goal, contributed));
        }
        return result;
    }

    public async Task<SavingsGoalResponse> CreateSavingsGoalAsync(Guid userId, Guid householdId, SaveSavingsGoalRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var household = await FindHouseholdAsync(householdId, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        var goal = new SavingsGoal(householdId, request.Name,
            new Money(request.TargetAmount, household.Currency), member.Id, request.TargetDate, request.Description);
        await db.SavingsGoals.AddAsync(goal, ct);
        await db.SaveChangesAsync(ct);
        return Map(goal, 0);
    }

    public async Task<SavingsGoalResponse> UpdateSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, SaveSavingsGoalRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var goal = await FindGoalAsync(householdId, goalId, ct);
        EnsureCreatorOrManager(member, goal.CreatedByMemberId);
        var household = await FindHouseholdAsync(householdId, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        goal.Revise(request.Name, request.Description, new Money(request.TargetAmount, household.Currency), request.TargetDate);
        await db.SaveChangesAsync(ct);
        return Map(goal, await SumContributionsAsync(householdId, goalId, ct));
    }

    public async Task<SavingsGoalResponse> CompleteSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var goal = await FindGoalAsync(householdId, goalId, ct);
        EnsureCreatorOrManager(member, goal.CreatedByMemberId);
        goal.Complete(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Map(goal, await SumContributionsAsync(householdId, goalId, ct));
    }

    public async Task<SavingsGoalResponse> CancelSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var goal = await FindGoalAsync(householdId, goalId, ct);
        EnsureCreatorOrManager(member, goal.CreatedByMemberId);
        goal.Cancel();
        await db.SaveChangesAsync(ct);
        return Map(goal, await SumContributionsAsync(householdId, goalId, ct));
    }

    public async Task<IReadOnlyList<SavingsContributionResponse>> ListContributionsAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken ct)
    {
        await RequireMemberAsync(userId, householdId, ct);
        _ = await FindGoalAsync(householdId, goalId, ct);
        var contributions = await db.SavingsGoalContributions.AsNoTracking()
            .Where(x => x.HouseholdId == householdId && x.SavingsGoalId == goalId)
            .OrderByDescending(x => x.ContributedOn)
            .ToListAsync(ct);
        return contributions.Select(Map).ToList();
    }

    public async Task<SavingsContributionResponse> AddContributionAsync(Guid userId, Guid householdId, Guid goalId, AddSavingsContributionRequest request, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        var goal = await FindGoalAsync(householdId, goalId, ct);
        if (goal.Status != SavingsGoalStatus.Active)
            throw new InvalidOperationException("Contributions can only be added to active goals.");
        var household = await FindHouseholdAsync(householdId, ct);
        EnsureCurrency(household.Currency, request.CurrencyCode);
        var contribution = new SavingsGoalContribution(householdId, goalId, member.Id,
            new Money(request.Amount, household.Currency), request.ContributedOn, request.Notes);
        await db.SavingsGoalContributions.AddAsync(contribution, ct);
        await db.SaveChangesAsync(ct);
        return Map(contribution);
    }

    private async Task<HouseholdMember> RequireMemberAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        var member = await db.HouseholdMembers.SingleOrDefaultAsync(
            x => x.HouseholdId == householdId && x.UserId == userId && x.Status == MembershipStatus.Active, ct)
            ?? throw new KeyNotFoundException("Household not found.");
        var household = await FindHouseholdAsync(householdId, ct);
        if (household.IsArchived) throw new KeyNotFoundException("Household not found.");
        return member;
    }

    private async Task<HouseholdMember> RequireManagerAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        if (member.Role is not (HouseholdRole.Owner or HouseholdRole.Admin))
            throw new UnauthorizedAccessException("Household manager role required.");
        return member;
    }

    private async Task<HouseholdMember> RequireOwnerAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        var member = await RequireMemberAsync(userId, householdId, ct);
        if (member.Role != HouseholdRole.Owner)
            throw new UnauthorizedAccessException("Household owner role required.");
        return member;
    }

    private async Task<Household> FindHouseholdAsync(Guid householdId, CancellationToken ct) =>
        await db.Households.SingleOrDefaultAsync(x => x.Id == householdId, ct)
        ?? throw new KeyNotFoundException("Household not found.");

    private async Task<HouseholdMember> FindMemberAsync(Guid householdId, Guid memberId, CancellationToken ct) =>
        await db.HouseholdMembers.SingleOrDefaultAsync(
            x => x.HouseholdId == householdId && x.Id == memberId && x.Status == MembershipStatus.Active, ct)
        ?? throw new KeyNotFoundException("Household member not found.");

    private async Task<Category> FindCategoryAsync(Guid householdId, Guid categoryId, CancellationToken ct) =>
        await db.Categories.SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == categoryId, ct)
        ?? throw new KeyNotFoundException("Category not found.");

    private async Task<Category> RequireCategoryKindAsync(Guid householdId, Guid categoryId, CategoryKind kind, CancellationToken ct)
    {
        var category = await FindCategoryAsync(householdId, categoryId, ct);
        if (!category.IsActive || category.Kind != kind)
            throw new ArgumentException($"An active {kind} category is required.");
        return category;
    }

    private async Task ValidateCategoryParentAsync(Guid householdId, Guid? parentId, CategoryKind kind, Guid? currentId, CancellationToken ct)
    {
        if (!parentId.HasValue) return;
        if (parentId == currentId) throw new ArgumentException("A category cannot be its own parent.");
        var parent = await FindCategoryAsync(householdId, parentId.Value, ct);
        if (!parent.IsActive || parent.Kind != kind || parent.ParentCategoryId.HasValue)
            throw new ArgumentException("Subcategories must have an active top-level parent of the same type.");
    }

    private async Task<Expense> FindExpenseAsync(Guid householdId, Guid expenseId, CancellationToken ct) =>
        await db.Expenses.SingleOrDefaultAsync(
            x => x.HouseholdId == householdId && x.Id == expenseId && x.DeletedAt == null, ct)
        ?? throw new KeyNotFoundException("Expense not found.");

    private async Task<Income> FindIncomeAsync(Guid householdId, Guid incomeId, CancellationToken ct) =>
        await db.Incomes.SingleOrDefaultAsync(
            x => x.HouseholdId == householdId && x.Id == incomeId && x.DeletedAt == null, ct)
        ?? throw new KeyNotFoundException("Income not found.");

    private async Task<SavingsGoal> FindGoalAsync(Guid householdId, Guid goalId, CancellationToken ct) =>
        await db.SavingsGoals.SingleOrDefaultAsync(x => x.HouseholdId == householdId && x.Id == goalId, ct)
        ?? throw new KeyNotFoundException("Savings goal not found.");

    private async Task<decimal> SumContributionsAsync(Guid householdId, Guid goalId, CancellationToken ct) =>
        await db.SavingsGoalContributions.AsNoTracking()
            .Where(x => x.HouseholdId == householdId && x.SavingsGoalId == goalId)
            .SumAsync(x => (decimal?)x.Amount.Amount, ct) ?? 0m;

    private static void EnsureCreatorOrManager(HouseholdMember currentMember, Guid creatorMemberId)
    {
        if (currentMember.Id != creatorMemberId && currentMember.Role is not (HouseholdRole.Owner or HouseholdRole.Admin))
            throw new UnauthorizedAccessException("Only the creator or a household manager can change this resource.");
    }

    private static void EnsureCurrency(CurrencyCode householdCurrency, string requestCurrency)
    {
        if (householdCurrency != new CurrencyCode(requestCurrency))
            throw new ArgumentException("Transaction currency must match the household currency.");
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum =>
        Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(T).Name} value.");

    private static void ValidateDateRange(DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && to.HasValue && from > to)
            throw new ArgumentException("The start date must not be after the end date.");
    }

    private static UserResponse Map(User x) => new(x.Id, x.Email.Value, x.DisplayName, x.Role.ToString(), x.CreatedAt);
    private static HouseholdResponse Map(Household x) => new(x.Id, x.Name, x.Currency.Value, x.TimeZone, x.CreatedAt, x.ArchivedAt);
    private static HouseholdMemberResponse Map(HouseholdMember member, User user) =>
        new(member.Id, user.Id, user.Email.Value, user.DisplayName, member.Role.ToString(), member.Status.ToString(), member.JoinedAt);
    private static CategoryResponse Map(Category x) => new(x.Id, x.ParentCategoryId, x.Name, x.Kind.ToString(), x.IsActive, x.SortOrder);
    private static ExpenseResponse Map(Expense x) =>
        new(x.Id, x.CategoryId, x.Amount.Amount, x.Amount.Currency.Value, x.OccurredOn, x.Description, x.Merchant, x.Notes, x.CreatedByMemberId);
    private static IncomeResponse Map(Income x) =>
        new(x.Id, x.CategoryId, x.Amount.Amount, x.Amount.Currency.Value, x.OccurredOn, x.Description, x.Source, x.Notes, x.CreatedByMemberId);
    private static BudgetResponse Map(Budget x, decimal spent) =>
        new(x.Id, x.CategoryId, x.Period.Year, x.Period.Month, x.AmountLimit.Amount,
            x.AlertThreshold.Value, spent, x.AmountLimit.Amount - spent, x.AmountLimit.Currency.Value,
            x.IsAlertThresholdReached(new Money(spent, x.AmountLimit.Currency)),
            x.IsOverspent(new Money(spent, x.AmountLimit.Currency)));
    private static SavingsGoalResponse Map(SavingsGoal x, decimal contributed) =>
        new(x.Id, x.Name, x.Description, x.TargetAmount.Amount, contributed, x.TargetAmount.Currency.Value,
            x.TargetDate, x.Status.ToString(), x.CreatedAt);
    private static SavingsContributionResponse Map(SavingsGoalContribution x) =>
        new(x.Id, x.Amount.Amount, x.Amount.Currency.Value, x.ContributedOn, x.Notes, x.CreatedByMemberId);
}
