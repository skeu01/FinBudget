using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Ocr;
using Xunit;

namespace Backend.Tests.Ocr;

public sealed class OcrTextParserTests
{
    [Fact]
    public void Parse_ExtractsMercadonaReceiptTotalsAndVatBreakdown()
    {
        const string text = """
            MERCADONA, S.A.
            A-46103834
            POL. IND. ATALAYA Y EL MORON S/N
            14600 MONTORO
            TELÉFONO: 957162514
            28/05/2025 14:09 OP: 1868064
            FACTURA SIMPLIFICADA: 3630-011-156272
            Descripción P. Unit Imp.(€)
            1 LS TORTILLA 1/2 P/CA 4,50
            1 PAÑUELOS 4 CAP 10X10 1,05
            1 PERLAS TIERNOS RECUE 1,45
            1 MINI TAQUITOS JAMON 2,65
            2 SALMOREJO FRESCO 1,25 2,50
            2 AGUA FRIO 0,60 1,20
            2 PAN VIENA 0,38 0,76
            TOTAL (E) 14,11
            ENTREGA EFECTIVO 15,00
            DEVOLUCIÓN 0,89
            IVA BASE IMPONIBLE (€) CUOTA (€)
            4% 0,73 0,03
            10% 9,86 0,99
            21% 2,07 0,43
            TOTAL 12,66 1,45
            """;

        var result = OcrTextParser.Parse(text, new CurrencyCode("EUR"), 88);

        result.Merchant.Should().Be("MERCADONA, S.A.");
        result.OccurredOn.Should().Be(new DateOnly(2025, 5, 28));
        result.Total?.Amount.Should().Be(14.11m);
        result.Vat?.Amount.Should().Be(1.45m);
        result.ItemDetails.Should().Equal(
            new ExpenseItem("LS TORTILLA 1/2 P/CA", 1, 4.50m),
            new ExpenseItem("PAÑUELOS 4 CAP 10X10", 1, 1.05m),
            new ExpenseItem("PERLAS TIERNOS RECUE", 1, 1.45m),
            new ExpenseItem("MINI TAQUITOS JAMON", 1, 2.65m),
            new ExpenseItem("SALMOREJO FRESCO", 2, 2.50m),
            new ExpenseItem("AGUA FRIO", 2, 1.20m),
            new ExpenseItem("PAN VIENA", 2, 0.76m));
    }

    [Fact]
    public void TesseractTsvParser_PreservesRecognizedLineBoundaries()
    {
        const string tsv = """
            level	page_num	block_num	par_num	line_num	word_num	left	top	width	height	conf	text
            5	1	1	1	1	1	0	0	10	10	90	MERCADONA,
            5	1	1	1	1	2	11	0	10	10	90	S.A.
            5	1	1	1	2	1	0	12	10	10	90	TOTAL
            5	1	1	1	2	2	11	12	10	10	90	14,11
            """;

        var result = TesseractOcrProvider.ParseTsv(tsv);

        result.Text.Should().Be($"MERCADONA, S.A.{Environment.NewLine}TOTAL 14,11");
        result.Confidence.Should().Be(90);
    }
}
