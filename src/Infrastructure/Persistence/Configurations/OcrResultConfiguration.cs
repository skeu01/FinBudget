using Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class OcrResultConfiguration : IEntityTypeConfiguration<OcrResult>
{
    public void Configure(EntityTypeBuilder<OcrResult> builder)
    {
        builder.ConfigureEntity("ocr_results");
        builder.HasIndex(result => new { result.ReceiptId, result.AttemptNumber }).IsUnique();
        builder.HasIndex(result => new { result.Status, result.StartedAt });
        builder.Property(result => result.AttemptNumber).IsRequired();
        builder.Property(result => result.Provider).HasMaxLength(80).IsRequired();
        builder.Property(result => result.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(result => result.ErrorCode).HasMaxLength(80);
        builder.Property(result => result.StartedAt).IsRequired();
        builder.Property(result => result.CompletedAt);
        builder.ToTable(table => table.HasCheckConstraint("ck_ocr_results_attempt_positive", "\"attempt_number\" > 0"));
        builder.Property(result => result.ExtractedData)
            .HasConversion(
                data => JsonSerializer.Serialize(data, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Domain.ValueObjects.OcrExtractedData>(
                    json,
                    (JsonSerializerOptions?)null))
            .HasColumnType("jsonb");
        builder.HasOne<Receipt>()
            .WithMany()
            .HasForeignKey(result => result.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
