namespace DeliverTableInfrastructure.TemplateData;

public sealed record CommissionStatementCreditNoteData(
    string StatementNumber,
    string OrderNumber,
    string IssuedAt,
    string TotalTtc,
    string Currency);
