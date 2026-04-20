using System.Collections.ObjectModel;

namespace Promise.Lib.Models;

public class ApiResponseUserTransactions : ApiResponseUser
{
    public Collection<SimpleUserTransaction>? Transactions { get; init; }
}

public class SimpleUserTransaction
{
    public long? Id { get; set; }
    public string? SenderLogin { get; set; }
    public string? ReceiverLogin { get; set; }
    public int Cents { get; set; }
    public DateTime Date { get; set; }
    public string? Memo { get; set; }
}
