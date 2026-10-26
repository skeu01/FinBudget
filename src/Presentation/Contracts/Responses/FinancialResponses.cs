namespace Presentation.Contracts.Responses;

/// <summary>Perfil público del usuario autenticado.</summary>
public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Role, DateTimeOffset CreatedAt);

/// <summary>Hogar visible para el usuario autenticado.</summary>
public sealed record HouseholdResponse(
    Guid Id,
    string Name,
    string CurrencyCode,
    string TimeZone,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt);

/// <summary>Miembro de un hogar.</summary>
public sealed record HouseholdMemberResponse(
    Guid Id,
    Guid UserId,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    DateTimeOffset JoinedAt);

/// <summary>Categoría o subcategoría de un hogar.</summary>
public sealed record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Kind,
    bool IsActive,
    int SortOrder);

/// <summary>Gasto registrado en un hogar.</summary>
public sealed record ExpenseResponse(
    Guid Id,
    Guid CategoryId,
    decimal Amount,
    string CurrencyCode,
    DateOnly OccurredOn,
    string? Description,
    string? Merchant,
    string? Notes,
    Guid CreatedByMemberId,
    IReadOnlyList<Domain.ValueObjects.ExpenseItem>? Items);

/// <summary>Ingreso registrado en un hogar.</summary>
public sealed record IncomeResponse(
    Guid Id,
    Guid CategoryId,
    decimal Amount,
    string CurrencyCode,
    DateOnly OccurredOn,
    string? Description,
    string? Source,
    string? Notes,
    Guid CreatedByMemberId);

/// <summary>Presupuesto mensual de una categoría.</summary>
public sealed record BudgetResponse(
    Guid Id,
    Guid CategoryId,
    int Year,
    int Month,
    decimal AmountLimit,
    decimal AlertThresholdPercent,
    decimal Spent,
    decimal Remaining,
    string CurrencyCode,
    bool AlertThresholdReached,
    bool IsOverspent);

/// <summary>Objetivo de ahorro con progreso agregado.</summary>
public sealed record SavingsGoalResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal TargetAmount,
    decimal ContributedAmount,
    string CurrencyCode,
    DateOnly? TargetDate,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>Aportación registrada para un objetivo.</summary>
public sealed record SavingsContributionResponse(
    Guid Id,
    decimal Amount,
    string CurrencyCode,
    DateOnly ContributedOn,
    string? Notes,
    Guid CreatedByMemberId);
