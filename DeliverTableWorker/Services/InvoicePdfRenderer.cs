using System.Text.Json;
using DeliverTableInfrastructure.Models;
using DeliverTableSharedLibrary.Dtos.Invoice;
using DeliverTableSharedLibrary.Enums;
using DeliverTableWorker.Constants;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DeliverTableWorker.Services;

public sealed class InvoicePdfRenderer : IInvoicePdfRenderer
{
    public byte[] Render(Invoice invoice)
    {
        var issuer =
            JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.IssuerLegalSnapshotJson)
            ?? new InvoiceLegalSnapshotDto("", "", "", "", "", "");
        var recipient =
            JsonSerializer.Deserialize<InvoiceLegalSnapshotDto>(invoice.RecipientSnapshotJson)
            ?? new InvoiceLegalSnapshotDto("", "", "", "", "", "");

        var isCreditNote =
            invoice.Kind == InvoiceKind.CreditNoteToCustomer
            || invoice.Kind == InvoiceKind.CommissionCreditNoteToRestaurant;
        var isVatExempt = invoice.Lines.All(l => l.VatRate == 0m);

        var title = isCreditNote ? "AVOIR" : "FACTURE";

        var doc = Document.Create(container =>
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
                            col.Item().Text(title).Bold().FontSize(18);
                            col.Item().Text($"N° {invoice.Number}");
                            col.Item().Text($"Date : {invoice.IssuedAt:dd/MM/yyyy}");
                            col.Item().Text($"Commande #{invoice.OrderId}");
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
                                    cd.RelativeColumn(4);
                                    cd.RelativeColumn(1);
                                    cd.RelativeColumn(2);
                                    if (!isVatExempt)
                                        cd.RelativeColumn(1);
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn(2);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Text("Description").Bold();
                                    h.Cell().AlignRight().Text("Qté").Bold();
                                    h.Cell().AlignRight().Text("PU HT").Bold();
                                    if (!isVatExempt)
                                        h.Cell().AlignRight().Text("TVA %").Bold();
                                    h.Cell().AlignRight().Text("Total HT").Bold();
                                    h.Cell().AlignRight().Text("Total TTC").Bold();
                                });
                                foreach (var line in invoice.Lines.OrderBy(l => l.SortOrder))
                                {
                                    table.Cell().Text(line.Description);
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.Quantity.ToString("0.###"));
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.UnitPriceHt.ToString("0.00 €"));
                                    if (!isVatExempt)
                                        table.Cell()
                                            .AlignRight()
                                            .Text($"{line.VatRate:0.#} %");
                                    table.Cell()
                                        .AlignRight()
                                        .Text(line.LineHt.ToString("0.00 €"));
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
                                totals.Item().Text($"Total HT : {invoice.TotalHt:0.00 €}");
                                totals.Item().Text($"Total TVA : {invoice.TotalVat:0.00 €}");
                                totals.Item().Text($"Total TTC : {invoice.TotalTtc:0.00 €}").Bold();
                            });

                        if (isVatExempt)
                        {
                            col.Item()
                                .PaddingTop(10)
                                .Text(PdfStrings.VatExemptClause)
                                .Italic();
                        }

                        if (isCreditNote && invoice.RelatedInvoice is not null)
                        {
                            col.Item()
                                .PaddingTop(10)
                                .Text(
                                    $"Référence facture d'origine : {invoice.RelatedInvoice.Number}"
                                )
                                .Italic();
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(PdfStrings.InvoiceFooterStripe)
                    .FontSize(8);
            });
        });

        return doc.GeneratePdf();
    }
}
