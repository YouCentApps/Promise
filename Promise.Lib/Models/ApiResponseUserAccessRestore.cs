namespace Promise.Lib;

public class ApiResponseUserAccessRestore : ApiResponseUser
{
    public string? NewPassword { get; set; }
    public string? MaskedEmail { get; set; }
    public string? MaskedTel { get; set; }
}

