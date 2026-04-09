namespace DeliverTableInfrastructure.TemplateData;

public record PasswordResetData(
    string ResetLink,
    string UserName);
