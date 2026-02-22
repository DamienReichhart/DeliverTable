namespace DeliverTableSharedLibrary.Dtos.Auth;

public class UserResponse
{
    public int Id {get; init;}
    public string? Email { get; set; } = null;
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
}