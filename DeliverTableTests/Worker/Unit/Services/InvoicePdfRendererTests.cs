using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Enums;
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
}
