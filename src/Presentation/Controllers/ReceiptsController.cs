using Infrastructure.Ocr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Contracts.Responses;

namespace Presentation.Controllers;

[Authorize]
[Route("api/v1/households/{householdId:guid}/receipts")]
[RequestSizeLimit(26 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 26 * 1024 * 1024)]
public sealed class ReceiptsController(IReceiptOcrService service) : ApiControllerBase
{
    /// <summary>Sube un ticket en JPEG, PNG o PDF y devuelve los datos detectados para revisión.</summary>
    [HttpPost(Name = "UploadReceipt")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ReceiptOcrResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReceiptOcrResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReceiptOcrResponse>> Upload(
        Guid householdId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest();
        }

        await using var content = file.OpenReadStream();
        var result = await service.UploadAndAnalyzeAsync(
            CurrentUserId,
            householdId,
            content,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);
        var response = Map(result);

        return result.OcrStatus == Domain.Enums.OcrStatus.Failed
            ? UnprocessableEntity(response)
            : CreatedAtRoute("GetReceiptOcr", new { householdId, receiptId = response.Id }, response);
    }

    /// <summary>Consulta el resultado más reciente del análisis de un recibo.</summary>
    [HttpGet("{receiptId:guid}", Name = "GetReceiptOcr")]
    [ProducesResponseType(typeof(ReceiptOcrResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReceiptOcrResponse>> Get(
        Guid householdId,
        Guid receiptId,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(CurrentUserId, householdId, receiptId, cancellationToken);
        return Ok(Map(result));
    }

    private static ReceiptOcrResponse Map(ReceiptOcrSnapshot result) =>
        new(
            result.Id,
            result.HouseholdId,
            result.OriginalFilename,
            result.MediaType,
            result.SizeBytes,
            result.ReceiptStatus,
            result.Provider,
            result.OcrStatus,
            result.AttemptNumber,
            result.OccurredOn,
            result.Merchant,
            result.Total,
            result.Vat,
            result.Currency,
            result.Confidence,
            result.ErrorCode,
            result.UploadedAt);
}
