namespace Promise.Lib.Models;

public class UserData
{
    public User? User { get; set; }
    public PersonalData? PersonalData { get; set; }
}

public class UserUpdate
{
    public User? OldUser { get; set; }
    public User? NewUser { get; set; }
}

public class UserTransaction
{
    public User? Sender { get; set; }
    public User? Receiver { get; set; }
    public int Cents { get; set; }
    public string? Memo { get; set; }
}

public class TransactionsHistoryInfo
{
    public User? User { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public bool IsOldFirst { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}

public class RestoreAccessInfo
{
    public string? Username { get; set; }
    public string? UseData { get; set; }
}

public class CurrencyPreferenceUpdate
{
    public User? User { get; set; }
    public byte CurrencyId { get; set; }
}

// Merchant request wrappers

public class MerchantRegisterRequest
{
    public User? User { get; set; }
    public string? MerchantName { get; set; }
    public string? Website { get; set; }
}

public class MerchantApiAuth
{
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
}

public class CreatePaymentRequestInput
{
    public MerchantApiAuth? Auth { get; set; }
    public int AmountCents { get; set; }
    public string? Description { get; set; }
    public byte TypeId { get; set; }
    public Uri? CallbackUrl { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime? SubscriptionExpiresDate { get; set; }
}

public class PayByTokenRequest
{
    public User? User { get; set; }
    public string? Token { get; set; }
}

public class SubscriptionChargeRequest
{
    public MerchantApiAuth? Auth { get; set; }
    public long SubscriptionId { get; set; }
}

public class MerchantRefundRequest
{
    public MerchantApiAuth? Auth { get; set; }
    public long MerchantTransactionId { get; set; }
}
