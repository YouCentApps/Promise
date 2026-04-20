namespace Promise.Common.Models;

public class ApiResponseUserInfo : ApiResponseUser
{
    public long Balance { get; set; }
    public long PromiseLimit { get; set; }
}
