namespace DeliverTableInfrastructure.TemplateData;

public sealed record CommissionStatementInvoiceData(
    string StatementNumber,
    string PeriodLabel,
    string IssuedAt,
    string TotalTtc,
    string Currency);
