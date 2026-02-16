namespace DeliverTableServer.Models;

public class User
{
    public enum Role
    {
        CUSTOMER,
        ADMINISTRATOR,
        RESTAURANT_OWNER
    }

    public enum Status
    {
        ACTIVE,
        SUSPENDED,
        BANNED
    }

    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";

    public Role role { get; set; } = Role.CUSTOMER;

    public Status status { get; set; } = Status.ACTIVE;

    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
}