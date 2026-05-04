using DeliverTableInfrastructure.Models;

namespace DeliverTableInfrastructure.Extensions;

public static class UserExtensions
{
    public static string GetFullName(this User user) =>
        $"{user.FirstName} {user.LastName}".Trim();
}
