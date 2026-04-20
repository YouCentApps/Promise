namespace Promise.Common.Models;

public class Currency
{
    public byte Id { get; set; }
    public string? Code { get; set; }
    public string? Number { get; set; }
    public string? Name { get; set; }
}

public class Language
{
    public int Id { get; set; }
    public string? NameEng { get; set; }
    public string? NameCode { get; set; }
    public string? NameNative { get; set; }
}

public class User
{
    public long Id { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public string? Salt { get; set; }
    public DateTime CreationDate { get; set; }
}

public class Balance
{
    public long UserId { get; set; }
    public long Cents { get; set; }
}

public class PromiseLimit
{
    public long UserId { get; set; }
    public long Cents { get; set; }
}

public class PromiseTransaction
{
    public long Id { get; set; }
    public long SenderId { get; set; }
    public long ReceiverId { get; set; }
    public int Cents { get; set; }
    public DateTime Date { get; set; }
    public string? Hash { get; set; }
    public bool IsBlockchain { get; set; }
    public string? Memo { get; set; }
}

public class Rate
{
    public byte CurrencyId { get; set; }
    public double AmountFor100 { get; set; }
    public DateTime UpdateDate { get; set; }
}

public class UserSetting
{
    public long UserId { get; set; }
    public int LanguageId { get; set; }
    public byte CurrencyId { get; set; }
    public bool IsDarkTheme { get; set; }
}

public class PersonalData
{
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string? Tel { get; set; }
    public string? Secret { get; set; }
    public string? EmailHash { get; set; }
    public string? TelHash { get; set; }
    public string? SecretHash { get; set; }
    public string? Salt { get; set; }
    public string? EmailMasked { get; set; }
    public string? TelMasked { get; set; }
}

public class AccessRestore
{
    public long UserId { get; set; }
    public int UseSecretTryNumber { get; set; }
    public DateTime? UseSecretTryDate { get; set; }
    public int UseEmailTryNumber { get; set; }
    public DateTime? UseEmailTryDate { get; set; }
    public int UseTelTryNumber { get; set; }
    public DateTime? UseTelTryDate { get; set; }
}

// Lookup types for Merchant operations

public class MerchantPaymentRequestType
{
    public byte Id { get; set; }
    public string? Name { get; set; }
}

public class MerchantPaymentRequestStatus
{
    public byte Id { get; set; }
    public string? Name { get; set; }
}

public class MerchantSubscriptionStatus
{
    public byte Id { get; set; }
    public string? Name { get; set; }
}

public class MerchantTransactionType
{
    public byte Id { get; set; }
    public string? Name { get; set; }
}

// Merchant entities

public class Merchant
{
    public long UserId { get; set; }
    public string? Name { get; set; }
    public string? Website { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecretHash { get; set; }
    public string? Salt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class MerchantPaymentRequest
{
    public long Id { get; set; }
    public string? Token { get; set; }
    public long MerchantId { get; set; }
    public int AmountCents { get; set; }
    public string? Description { get; set; }
    public byte TypeId { get; set; }
    public byte StatusId { get; set; }
    public Uri? CallbackUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime? SubscriptionExpiresDate { get; set; }
}

public class MerchantSubscription
{
    public long Id { get; set; }
    public long MerchantId { get; set; }
    public long SubscriberId { get; set; }
    public long PaymentRequestId { get; set; }
    public int AmountCents { get; set; }
    public int IntervalDays { get; set; }
    public DateTime NextChargeDate { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public byte StatusId { get; set; }
    public DateTime ConsentDate { get; set; }
    public DateTime? CancelledDate { get; set; }
}

public class MerchantTransaction
{
    public long Id { get; set; }
    public long MerchantId { get; set; }
    public long PayerId { get; set; }
    public long? SubscriptionId { get; set; }
    public long PaymentRequestId { get; set; }
    public long PromiseTransactionId { get; set; }
    public int AmountCents { get; set; }
    public byte TypeId { get; set; }
    public DateTime Date { get; set; }
}

