using System.Security.Claims;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Ocr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Xunit;

namespace Backend.Tests.Ocr;

public sealed class ReceiptsControllerTests
{
    [Fact]
    public async Task Upload_ReturnsCreatedWithExtractedReceipt()
    {
        var householdId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var service = new Mock<IReceiptOcrService>();
        service.Setup(mock => mock.UploadAndAnalyzeAsync(
                It.IsAny<Guid>(), householdId, It.IsAny<Stream>(), "receipt.pdf", "application/pdf", 12,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot(receiptId, householdId, OcrStatus.Succeeded, null));
        var controller = CreateController(service.Object);
        var file = new FormFile(new MemoryStream("receipt-data"u8.ToArray()), 0, 12, "file", "receipt.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var action = await controller.Upload(householdId, file, CancellationToken.None);

        var created = action.Result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        created.RouteName.Should().Be("GetReceiptOcr");
        created.RouteValues.Should().ContainKey("receiptId").WhoseValue.Should().Be(receiptId);
        created.Value.Should().NotBeNull();
        service.VerifyAll();
    }

    [Fact]
    public async Task Upload_ReturnsUnprocessableEntityWhenRecognitionFails()
    {
        var householdId = Guid.NewGuid();
        var service = new Mock<IReceiptOcrService>();
        service.Setup(mock => mock.UploadAndAnalyzeAsync(
                It.IsAny<Guid>(), householdId, It.IsAny<Stream>(), "receipt.jpg", "image/jpeg", 4,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Snapshot(Guid.NewGuid(), householdId, OcrStatus.Failed, "no_text_detected"));
        var controller = CreateController(service.Object);
        var file = new FormFile(new MemoryStream([1, 2, 3, 4]), 0, 4, "file", "receipt.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var action = await controller.Upload(householdId, file, CancellationToken.None);

        var failed = action.Result.Should().BeOfType<UnprocessableEntityObjectResult>().Subject;
        failed.Value.Should().NotBeNull();
        service.VerifyAll();
    }

    private static ReceiptsController CreateController(IReceiptOcrService service)
    {
        var controller = new ReceiptsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test"))
                }
            }
        };
        return controller;
    }

    private static ReceiptOcrSnapshot Snapshot(Guid id, Guid householdId, OcrStatus status, string? errorCode) =>
        new(id, householdId, "receipt.pdf", "application/pdf", 12, "NeedsReview", "Tesseract", status,
            1, new DateOnly(2026, 10, 25), "Market", 25.50m, 2.10m, "EUR", 95, errorCode,
            DateTimeOffset.UtcNow);
}
