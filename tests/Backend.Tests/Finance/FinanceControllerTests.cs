using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Contracts.Requests;
using Presentation.Contracts.Responses;
using Presentation.Controllers;
using Presentation.Services;
using Xunit;

namespace Backend.Tests.Finance;

public sealed class FinanceControllerTests
{
    [Fact]
    public async Task CreateExpense_ReturnsCreatedRouteAndForwardsRequest()
    {
        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var request = new SaveExpenseRequest
        {
            CategoryId = Guid.NewGuid(),
            Amount = 45.75m,
            CurrencyCode = "EUR",
            OccurredOn = new DateOnly(2026, 10, 25)
        };
        var service = new Mock<IFinancialApiService>();
        service.Setup(mock => mock.CreateExpenseAsync(userId, householdId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpenseResponse(expenseId, request.CategoryId, request.Amount, "EUR",
                request.OccurredOn, null, null, null, Guid.NewGuid(), null));
        var controller = new ExpensesController(service.Object);
        SetUser(controller, userId);

        var action = await controller.Create(householdId, request, CancellationToken.None);

        var created = action.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("GetExpense");
        created.RouteValues.Should().Contain("expenseId", expenseId);
        service.VerifyAll();
    }

    [Fact]
    public async Task CreateBudget_ReturnsCreatedRouteForMonth()
    {
        var userId = Guid.NewGuid();
        var householdId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var request = new SaveBudgetRequest
        {
            CategoryId = Guid.NewGuid(),
            Year = 2026,
            Month = 10,
            AmountLimit = 500,
            AlertThresholdPercent = 80,
            CurrencyCode = "EUR"
        };
        var service = new Mock<IFinancialApiService>();
        service.Setup(mock => mock.CreateBudgetAsync(userId, householdId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetResponse(budgetId, request.CategoryId, request.Year, request.Month,
                request.AmountLimit, request.AlertThresholdPercent, 0, 500, "EUR", false, false));
        var controller = new BudgetsController(service.Object);
        SetUser(controller, userId);

        var action = await controller.Create(householdId, request, CancellationToken.None);

        var created = action.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("ListBudgets");
        created.RouteValues.Should().Contain("year", 2026).And.Contain("month", 10);
        service.VerifyAll();
    }

    private static void SetUser(ControllerBase controller, Guid userId) =>
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"))
            }
        };
}
