using DeliverTableInfrastructure.Models;

namespace DeliverTableServer.Common;

public static class BillingAddressHelper
{
    public static bool HasCompleteBillingAddress(User user) =>
        !string.IsNullOrWhiteSpace(user.BillingAddressLine1)
        && !string.IsNullOrWhiteSpace(user.BillingPostalCode)
        && !string.IsNullOrWhiteSpace(user.BillingCity)
        && !string.IsNullOrWhiteSpace(user.BillingCountry);

    public static string FormatBillingAddressForSnapshot(User user)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(user.BillingAddressLine1))
            lines.Add(user.BillingAddressLine1.Trim());
        if (!string.IsNullOrWhiteSpace(user.BillingAddressLine2))
            lines.Add(user.BillingAddressLine2.Trim());

        var postalCity = string.Join(" ", new[]
        {
            (user.BillingPostalCode ?? string.Empty).Trim(),
            (user.BillingCity ?? string.Empty).Trim(),
        }.Where(s => s.Length > 0));
        if (postalCity.Length > 0)
            lines.Add(postalCity);

        if (!string.IsNullOrWhiteSpace(user.BillingCountry))
            lines.Add(user.BillingCountry.Trim());

        return string.Join("\n", lines);
    }
}
