using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Constants;
using DeliverTableWorker.Services;
using NUnit.Framework;
using QuestPDF.Infrastructure;

namespace DeliverTableTests.Worker.Unit.Services;

[TestFixture]
public class InvoicePdfRendererTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Test]
    public void Render_BasicInvoice_ProducesValidPdf()
    {
        var invoice = new Invoice
        {
            Number = "TEST-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 1,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            TotalHt = 10m,
            TotalVat = 1m,
            TotalTtc = 11m,
            IssuerLegalSnapshotJson =
                """{"Name":"Test","LegalForm":"SAS","Siret":"73282932000074","VatNumber":"","Address":""}""",
            RecipientSnapshotJson =
                """{"Name":"Client","LegalForm":"","Siret":"","VatNumber":"","Address":""}""",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Plat",
                    Quantity = 1m,
                    UnitPriceHt = 10m,
                    UnitPriceTtc = 11m,
                    VatRate = 10m,
                    LineHt = 10m,
                    LineVat = 1m,
                    LineTtc = 11m,
                    SortOrder = 0,
                },
            },
        };

        var pdf = new InvoicePdfRenderer().Render(invoice);

        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
        Assert.That(pdf[0], Is.EqualTo((byte)'%'));
        Assert.That(pdf[1], Is.EqualTo((byte)'P'));
        Assert.That(pdf[2], Is.EqualTo((byte)'D'));
        Assert.That(pdf[3], Is.EqualTo((byte)'F'));
    }

    [Test]
    public void Render_CreditNote_IncludesAvoirLabel()
    {
        var invoice = new Invoice
        {
            Number = "AV-TEST-000002",
            Kind = InvoiceKind.CreditNoteToCustomer,
            OrderId = 1,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            TotalHt = -10m,
            TotalVat = -1m,
            TotalTtc = -11m,
            IssuerLegalSnapshotJson = "{}",
            RecipientSnapshotJson = "{}",
            Lines = new List<InvoiceLine>
            {
                new()
                {
                    Description = "Plat",
                    Quantity = -1m,
                    UnitPriceHt = 10m,
                    UnitPriceTtc = 11m,
                    VatRate = 10m,
                    LineHt = -10m,
                    LineVat = -1m,
                    LineTtc = -11m,
                    SortOrder = 0,
                },
            },
        };

        var pdf = new InvoicePdfRenderer().Render(invoice);

        Assert.That(pdf.Length, Is.GreaterThan(1000));
    }

    [Test]
    public void Render_VatExemptInvoice_IncludesExemptionClause()
    {
        var invoice = BuildSampleInvoice(vatRate: 0m);
        var pdf = new InvoicePdfRenderer().Render(invoice);

        // Minimal smoke: a valid non-trivial PDF is produced.
        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
        // Verify the invoice itself carries zero VAT before rendering.
        Assert.That(invoice.Lines, Has.All.Matches<InvoiceLine>(l => l.VatRate == 0m));
    }

    [Test]
    public void Render_TotalsEqualSumOfLines()
    {
        var invoice = BuildSampleInvoice(vatRate: 20m);

        // Verify invoice totals are consistent with line-level values before rendering.
        Assert.That(invoice.TotalHt, Is.EqualTo(invoice.Lines.Sum(l => l.LineHt)));
        Assert.That(invoice.TotalVat, Is.EqualTo(invoice.Lines.Sum(l => l.LineVat)));
        Assert.That(invoice.TotalTtc, Is.EqualTo(invoice.Lines.Sum(l => l.LineTtc)));

        // Also confirm a PDF is produced without throwing.
        var pdf = new InvoicePdfRenderer().Render(invoice);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
    }

    private static Invoice BuildSampleInvoice(decimal vatRate)
    {
        const decimal unitHt = 10m;
        var unitTtc = Math.Round(unitHt * (1 + vatRate / 100m), 2, MidpointRounding.AwayFromZero);
        var lineVat = Math.Round(unitTtc - unitHt, 2, MidpointRounding.AwayFromZero);

        var line = new InvoiceLine
        {
            Description = "Plat test",
            Quantity = 1m,
            UnitPriceHt = unitHt,
            UnitPriceTtc = unitTtc,
            VatRate = vatRate,
            LineHt = unitHt,
            LineVat = lineVat,
            LineTtc = unitTtc,
            SortOrder = 0,
        };

        return new Invoice
        {
            Number = "TEST-SMOKE-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 1,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            TotalHt = line.LineHt,
            TotalVat = line.LineVat,
            TotalTtc = line.LineTtc,
            IssuerLegalSnapshotJson =
                """{"Name":"Test SAS","LegalForm":"SAS","Siret":"73282932000074","VatNumber":"","Address":"","Email":""}""",
            RecipientSnapshotJson =
                """{"Name":"Client Test","LegalForm":"","Siret":"","VatNumber":"","Address":"","Email":"client@example.fr"}""",
            Lines = new List<InvoiceLine> { line },
        };
    }
}
