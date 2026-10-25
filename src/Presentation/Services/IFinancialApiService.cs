using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;

namespace Presentation.Services;

public interface IFinancialApiService
{
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserResponse> UpdateCurrentUserAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserResponse>> ListUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<HouseholdResponse>> ListHouseholdsAsync(Guid userId, CancellationToken cancellationToken);
    Task<HouseholdResponse> GetHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
    Task<HouseholdResponse> CreateHouseholdAsync(Guid userId, CreateHouseholdRequest request, CancellationToken cancellationToken);
    Task<HouseholdResponse> UpdateHouseholdAsync(Guid userId, Guid householdId, UpdateHouseholdRequest request, CancellationToken cancellationToken);
    Task ArchiveHouseholdAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HouseholdMemberResponse>> ListMembersAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
    Task<HouseholdMemberResponse> AddMemberAsync(Guid userId, Guid householdId, AddHouseholdMemberRequest request, CancellationToken cancellationToken);
    Task<HouseholdMemberResponse> UpdateMemberAsync(Guid userId, Guid householdId, Guid memberId, UpdateHouseholdMemberRequest request, CancellationToken cancellationToken);
    Task RemoveMemberAsync(Guid userId, Guid householdId, Guid memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryResponse>> ListCategoriesAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
    Task<CategoryResponse> CreateCategoryAsync(Guid userId, Guid householdId, SaveCategoryRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateCategoryAsync(Guid userId, Guid householdId, Guid categoryId, SaveCategoryRequest request, CancellationToken cancellationToken);
    Task DeactivateCategoryAsync(Guid userId, Guid householdId, Guid categoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExpenseResponse>> ListExpensesAsync(Guid userId, Guid householdId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<ExpenseResponse> GetExpenseAsync(Guid userId, Guid householdId, Guid expenseId, CancellationToken cancellationToken);
    Task<ExpenseResponse> CreateExpenseAsync(Guid userId, Guid householdId, SaveExpenseRequest request, CancellationToken cancellationToken);
    Task<ExpenseResponse> UpdateExpenseAsync(Guid userId, Guid householdId, Guid expenseId, SaveExpenseRequest request, CancellationToken cancellationToken);
    Task DeleteExpenseAsync(Guid userId, Guid householdId, Guid expenseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<IncomeResponse>> ListIncomesAsync(Guid userId, Guid householdId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<IncomeResponse> GetIncomeAsync(Guid userId, Guid householdId, Guid incomeId, CancellationToken cancellationToken);
    Task<IncomeResponse> CreateIncomeAsync(Guid userId, Guid householdId, SaveIncomeRequest request, CancellationToken cancellationToken);
    Task<IncomeResponse> UpdateIncomeAsync(Guid userId, Guid householdId, Guid incomeId, SaveIncomeRequest request, CancellationToken cancellationToken);
    Task DeleteIncomeAsync(Guid userId, Guid householdId, Guid incomeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BudgetResponse>> ListBudgetsAsync(Guid userId, Guid householdId, int year, int month, CancellationToken cancellationToken);
    Task<BudgetResponse> CreateBudgetAsync(Guid userId, Guid householdId, SaveBudgetRequest request, CancellationToken cancellationToken);
    Task<BudgetResponse> UpdateBudgetAsync(Guid userId, Guid householdId, Guid budgetId, SaveBudgetRequest request, CancellationToken cancellationToken);
    Task DeleteBudgetAsync(Guid userId, Guid householdId, Guid budgetId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavingsGoalResponse>> ListSavingsGoalsAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);
    Task<SavingsGoalResponse> CreateSavingsGoalAsync(Guid userId, Guid householdId, SaveSavingsGoalRequest request, CancellationToken cancellationToken);
    Task<SavingsGoalResponse> UpdateSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, SaveSavingsGoalRequest request, CancellationToken cancellationToken);
    Task<SavingsGoalResponse> CompleteSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken cancellationToken);
    Task<SavingsGoalResponse> CancelSavingsGoalAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavingsContributionResponse>> ListContributionsAsync(Guid userId, Guid householdId, Guid goalId, CancellationToken cancellationToken);
    Task<SavingsContributionResponse> AddContributionAsync(Guid userId, Guid householdId, Guid goalId, AddSavingsContributionRequest request, CancellationToken cancellationToken);
}
