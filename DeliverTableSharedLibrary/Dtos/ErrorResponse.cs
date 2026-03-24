namespace DeliverTableSharedLibrary.Dtos;

public class ErrorResponse
{
    public string Error { get; set; } = "";
    public int Status { get; set; } = 500;
}
