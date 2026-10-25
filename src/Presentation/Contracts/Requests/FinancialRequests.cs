using System.ComponentModel.DataAnnotations;

namespace Presentation.Contracts.Requests;

/// <summary>Datos de perfil editables por el propio usuario.</summary>
public sealed record UpdateUserProfileRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string DisplayName { get; init; }
}

/// <summary>Datos necesarios para registrar un hogar.</summary>
public sealed record CreateHouseholdRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string TimeZone { get; init; }
}

/// <summary>Datos editables de un hogar.</summary>
public sealed record UpdateHouseholdRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }
}

/// <summary>Datos para agregar un usuario existente a un hogar.</summary>
public sealed record AddHouseholdMemberRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string Role { get; init; } = "Member";
}

/// <summary>Datos para cambiar el rol de un miembro.</summary>
public sealed record UpdateHouseholdMemberRequest
{
    [Required]
    public required string Role { get; init; }
}

/// <summary>Datos para crear o editar una categoría.</summary>
public sealed record SaveCategoryRequest
{
    [Required, StringLength(80, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required]
    public required string Kind { get; init; }

    public Guid? ParentCategoryId { get; init; }

    public int SortOrder { get; init; }
}

/// <summary>Datos para crear o editar un gasto.</summary>
public sealed record SaveExpenseRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }

    [Required]
    public DateOnly OccurredOn { get; init; }

    [StringLength(200)]
    public string? Description { get; init; }

    [StringLength(160)]
    public string? Merchant { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }
}

/// <summary>Datos para crear o editar un ingreso.</summary>
public sealed record SaveIncomeRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }

    [Required]
    public DateOnly OccurredOn { get; init; }

    [StringLength(200)]
    public string? Description { get; init; }

    [StringLength(160)]
    public string? Source { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }
}

/// <summary>Datos para crear o actualizar un presupuesto mensual.</summary>
public sealed record SaveBudgetRequest
{
    [Required]
    public Guid CategoryId { get; init; }

    [Range(1, 9999)]
    public int Year { get; init; }

    [Range(1, 12)]
    public int Month { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal AmountLimit { get; init; }

    [Range(typeof(decimal), "0.01", "100")]
    public decimal AlertThresholdPercent { get; init; } = 80;

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }
}

/// <summary>Datos para crear o editar un objetivo de ahorro.</summary>
public sealed record SaveSavingsGoalRequest
{
    [Required, StringLength(120, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal TargetAmount { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }

    public DateOnly? TargetDate { get; init; }
}

/// <summary>Datos para registrar una aportación a un objetivo.</summary>
public sealed record AddSavingsContributionRequest
{
    [Range(typeof(decimal), "0.01", "999999999999.99")]
    public decimal Amount { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public required string CurrencyCode { get; init; }

    [Required]
    public DateOnly ContributedOn { get; init; }

    [StringLength(2000)]
    public string? Notes { get; init; }
}
