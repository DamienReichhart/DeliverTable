namespace DeliverTableSharedLibrary.Dtos.Auth;

public class ConnectionResponse
{
    public required string Token { get; set; } = "";
    public required UserResponse User { get; set; }
}