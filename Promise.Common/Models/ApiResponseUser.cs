namespace Promise.Common.Models;

public class ApiResponseUser : ApiResponse
{
    public long Id { get; set; }
    public string? Login { get; set; }
}
