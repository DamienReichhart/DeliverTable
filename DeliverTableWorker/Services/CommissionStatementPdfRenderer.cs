using System.Text.Json;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DeliverTableWorker.Services;

public sealed class CommissionStatementPdfRenderer : ICommissionStatementPdfRenderer
{
    private static readonly string[] MoisFrancaisNames =
    [
        "janvier",
        "février",
        "mars",
        "avril",
        "mai",
        "juin",
        "juillet",
        "août",
        "septembre",
        "octobre",
        "novembre",
        "décembre",
    ];

    private static string MoisFrancais(int month) => MoisFrancaisNames[month - 1];

    public byte[] Render(CommissionStatement statement)
    {
        InvoiceLegalSnapshotDto issuer =
            JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(statement.IssuerLegalSnapshotJson)
            ?? new InvoiceLegalSnapshotDto("", "", "", "", "", "");
        InvoiceLegalSnapshotDto recipient =
            JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(statement.RecipientSnapshotJson)
            ?? new InvoiceLegalSnapshotDto("", "", "", "", "", "");

        bool isCreditNote = statement.Kind == CommissionStatementKind.CreditNote;
        string title = isCreditNote ? "AVOIR DE COMMISSIONS" : "RELEVÉ DE COMMISSIONS";

        int year = statement.PeriodYear;
        int month = statement.PeriodMonth;
        string mois = MoisFrancais(month);
        int lastDay = DateTime.DaysInMonth(year, month);
        string periodBanner = $"Période du 1er {mois} {year} au {lastDay} {mois} {year}";

        decimal vatRate = statement.Lines.Count > 0 ? statement.Lines[0].VatRate : 20m;
        string vatLabel = $"TVA ({vatRate:0.#}%)";

        Document doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(issuer.Name).Bold().FontSize(14);
                        if (
                            !string.IsNullOrEmpty(issuer.LegalForm)
                            || !string.IsNullOrEmpty(issuer.Siret)
                        )
                            col.Item().Text($"{issuer.LegalForm} — SIRET {issuer.Siret}");
                        if (!string.IsNullOrEmpty(issuer.VatNumber))
                            col.Item().Text($"TVA {issuer.VatNumber}");
                        if (!string.IsNullOrEmpty(issuer.Address))
                            col.Item().Text(issuer.Address);
                    });
                    row.RelativeItem()
                        .AlignRight()
                        .Column(col =>
                        {
                            col.Item().Text(title).Bold().FontSize(16);
                            col.Item().Text($"N° {statement.Number}");
                            col.Item().Text($"Date : {statement.IssuedAt:dd/MM/yyyy}");
                            col.Item().Text(periodBanner).Italic();
                        });
                });

                page.Content()
                    .PaddingVertical(10)
                    .Column(col =>
                    {
                        col.Item().Text("Destinataire :").Bold();
                        col.Item().Text(recipient.Name);
                        if (!string.IsNullOrEmpty(recipient.Siret))
                            col.Item().Text($"SIRET {recipient.Siret}");
                        if (!string.IsNullOrEmpty(recipient.Address))
                            col.Item().Text(recipient.Address);
                        if (!string.IsNullOrEmpty(recipient.Email))
                            col.Item().Text(recipient.Email);

                        col.Item()
                            .PaddingTop(15)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(2); // N° commande
                                    cd.RelativeColumn(2); // Date livraison
                                    cd.RelativeColumn(2); // Montant TTC
                                    cd.RelativeColumn(1); // Taux
                                    cd.RelativeColumn(2); // Commission HT
                                    cd.RelativeColumn(1); // TVA
                                    cd.RelativeColumn(2); // Commission TTC
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Text("N° commande").Bold();
                                    h.Cell().AlignRight().Text("Date livraison").Bold();
                                    h.Cell().AlignRight().Text("Montant TTC").Bold();
                                    h.Cell().AlignRight().Text("Taux").Bold();
                                    h.Cell().AlignRight().Text("Commission HT").Bold();
                                    h.Cell().AlignRight().Text("TVA").Bold();
                                    h.Cell().AlignRight().Text("Commission TTC").Bold();
                                });

                                foreach (CommissionStatementLine? line in statement.Lines.OrderBy(l => l.SortOrder))
                                {
                                    table.Cell().Text(line.OrderNumber);
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.OrderCompletedAt.ToString("dd/MM/yyyy"));
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.OrderTotalAmount.ToString("0.00 €"));
                                    table.Cell()
                                        .AlignRight()
                                        .Text($"{line.CommissionRateSnapshot * 100m:0.#} %");
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.LineHt.ToString("0.00 €"));
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.LineVat.ToString("0.00 €"));
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.LineTtc.ToString("0.00 €"));
                                }
                            });

                        col.Item()
                            .PaddingTop(15)
                            .AlignRight()
                            .Column(totals =>
                            {
                                totals.Item().Text($"Total HT : {statement.TotalHt:0.00 €}");
                                totals
                                    .Item()
                                    .Text($"{vatLabel} : {statement.TotalVat:0.00 €}");
                                totals
                                    .Item()
                                    .Text($"Total TTC : {statement.TotalTtc:0.00 €}")
                                    .Bold();
                            });
                    });

                page.Footer()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item()
                            .AlignCenter()
                            .Text($"Document n° {statement.Number}")
                            .FontSize(8);
                        if (!string.IsNullOrEmpty(issuer.Name))
                        {
                            string legalLine = issuer.Name;
                            if (!string.IsNullOrEmpty(issuer.LegalForm))
                                legalLine += $" — {issuer.LegalForm}";
                            if (!string.IsNullOrEmpty(issuer.Siret))
                                legalLine += $" — SIRET {issuer.Siret}";
                            if (!string.IsNullOrEmpty(issuer.VatNumber))
                                legalLine += $" — TVA {issuer.VatNumber}";
                            col.Item().AlignCenter().Text(legalLine).FontSize(8);
                        }
                    });
            });
        });

        return doc.GeneratePdf();
    }
}
