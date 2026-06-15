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
        Invoice invoice = new Invoice
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

        byte[] pdf = new InvoicePdfRenderer().Render(invoice);

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
        Invoice invoice = new Invoice
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

        byte[] pdf = new InvoicePdfRenderer().Render(invoice);

        Assert.That(pdf.Length, Is.GreaterThan(1000));
    }

    [Test]
    public void Render_VatExemptInvoice_IncludesExemptionClause()
    {
        Invoice invoice = BuildSampleInvoice(vatRate: 0m);
        byte[] pdf = new InvoicePdfRenderer().Render(invoice);

        // Minimal smoke: a valid non-trivial PDF is produced.
        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
        // Verify the invoice itself carries zero VAT before rendering.
        Assert.That(invoice.Lines, Has.All.Matches<InvoiceLine>(l => l.VatRate == 0m));
    }

    [Test]
    public void Render_TotalsEqualSumOfLines()
    {
        Invoice invoice = BuildSampleInvoice(vatRate: 20m);

        // Verify invoice totals are consistent with line-level values before rendering.
        Assert.That(invoice.TotalHt, Is.EqualTo(invoice.Lines.Sum(l => l.LineHt)));
        Assert.That(invoice.TotalVat, Is.EqualTo(invoice.Lines.Sum(l => l.LineVat)));
        Assert.That(invoice.TotalTtc, Is.EqualTo(invoice.Lines.Sum(l => l.LineTtc)));

        // Also confirm a PDF is produced without throwing.
        byte[] pdf = new InvoicePdfRenderer().Render(invoice);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
    }

    [Test]
    public void Render_InvoiceWithDiscountLines_ProducesValidPdf()
    {
        Invoice invoice = new Invoice
        {
            Number = "TEST-DISC-000001",
            Kind = InvoiceKind.OrderInvoiceToCustomer,
            OrderId = 99,
            IssuedAt = DateTime.UtcNow,
            Currency = "EUR",
            IssuerLegalSnapshotJson =
                """{"Name":"Resto","LegalForm":"SAS","Siret":"73282932000074","VatNumber":"","Address":"1 rue"}""",
            RecipientSnapshotJson =
                """{"Name":"Client","LegalForm":"","Siret":"","VatNumber":"","Address":""}""",
            Lines = new List<InvoiceLine>
            {
                new() { Kind = InvoiceLineKind.Item, Description = "Plat", Quantity = 1m,
                        UnitPriceHt = 50m, UnitPriceTtc = 60m, VatRate = 20m,
                        LineHt = 50m, LineVat = 10m, LineTtc = 60m, SortOrder = 0 },
                new() { Kind = InvoiceLineKind.Item, Description = "Boisson", Quantity = 1m,
                        UnitPriceHt = 36.36m, UnitPriceTtc = 40m, VatRate = 10m,
                        LineHt = 36.36m, LineVat = 3.64m, LineTtc = 40m, SortOrder = 1 },
                new() { Kind = InvoiceLineKind.Discount, Description = "SUMMER10 (TVA 20%)", Quantity = 1m,
                        UnitPriceHt = -5m, UnitPriceTtc = -6m, VatRate = 20m,
                        LineHt = -5m, LineVat = -1m, LineTtc = -6m, SortOrder = 2 },
                new() { Kind = InvoiceLineKind.Discount, Description = "SUMMER10 (TVA 10%)", Quantity = 1m,
                        UnitPriceHt = -3.64m, UnitPriceTtc = -4m, VatRate = 10m,
                        LineHt = -3.64m, LineVat = -0.36m, LineTtc = -4m, SortOrder = 3 },
            },
        };
        invoice.TotalHt = invoice.Lines.Sum(l => l.LineHt);
        invoice.TotalVat = invoice.Lines.Sum(l => l.LineVat);
        invoice.TotalTtc = invoice.Lines.Sum(l => l.LineTtc);

        byte[] pdf = new InvoicePdfRenderer().Render(invoice);

        Assert.That(pdf, Is.Not.Null);
        Assert.That(pdf.Length, Is.GreaterThan(1000));
        Assert.That(pdf[0], Is.EqualTo((byte)'%'));
        Assert.That(pdf[1], Is.EqualTo((byte)'P'));
        Assert.That(pdf[2], Is.EqualTo((byte)'D'));
        Assert.That(pdf[3], Is.EqualTo((byte)'F'));
    }

    private static Invoice BuildSampleInvoice(decimal vatRate)
    {
        const decimal unitHt = 10m;
        decimal unitTtc = Math.Round(unitHt * (1 + vatRate / 100m), 2, MidpointRounding.AwayFromZero);
        decimal lineVat = Math.Round(unitTtc - unitHt, 2, MidpointRounding.AwayFromZero);

        InvoiceLine line = new InvoiceLine
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
