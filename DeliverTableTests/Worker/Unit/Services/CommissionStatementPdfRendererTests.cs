using DeliverTableTests.Server.Factories;
using DeliverTableWorker.Services;
using NUnit.Framework;
using QuestPDF.Infrastructure;

namespace DeliverTableTests.Worker.Unit.Services;

[TestFixture]
public class CommissionStatementPdfRendererTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Test]
    public void Render_ReturnsNonEmptyPdfBytes_ForInvoice()
    {
        var statement = CommissionStatementFactory.CreateInvoice(7, 2026, 5);
        statement.IssuerLegalSnapshotJson =
            """{"Name":"Platform","LegalForm":"SAS","Siret":"123","VatNumber":"FR1","Address":"X","Email":""}""";
        statement.RecipientSnapshotJson =
            """{"Name":"Resto","LegalForm":"SARL","Siret":"456","VatNumber":"FR2","Address":"Y","Email":""}""";
        statement.Lines.Add(
            new()
            {
                OrderId = 1,
                OrderNumber = "1",
                OrderCompletedAt = new DateTime(2026, 5, 10),
                OrderTotalAmount = 100m,
                CommissionRateSnapshot = 0.10m,
                VatRate = 20m,
                LineHt = 10m,
                LineVat = 2m,
                LineTtc = 12m,
            }
        );
        statement.TotalHt = 10m;
        statement.TotalVat = 2m;
        statement.TotalTtc = 12m;

        var sut = new CommissionStatementPdfRenderer();
        var bytes = sut.Render(statement);

        Assert.That(bytes.Length, Is.GreaterThan(1000));
        Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 0, 5), Is.EqualTo("%PDF-"));
    }
}
