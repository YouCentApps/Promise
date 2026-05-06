namespace Promise.Common.Models;

public class ApiResponseMerchant : ApiResponse
{
    public long UserId { get; set; }
    public string? Name { get; set; }
    public string? Website { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool IsActive { get; set; }
}

public class ApiResponsePaymentRequest : ApiResponse
{
    public long Id { get; set; }
    public string? Token { get; set; }
    public Uri? PaymentUrl { get; set; }
    public int AmountCents { get; set; }
    public string? Description { get; set; }
    public byte TypeId { get; set; }
    public byte StatusId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public int? IntervalDays { get; set; }
    public DateTime? SubscriptionExpiresDate { get; set; }
}

public class ApiResponsePaymentRequestInfo : ApiResponse
{
    public string? MerchantName { get; set; }
    public int AmountCents { get; set; }
    public string? Description { get; set; }
    public byte TypeId { get; set; }
    public byte StatusId { get; set; }
    public int? IntervalDays { get; set; }
}

public class ApiResponsePaymentResult : ApiResponse
{
    public long? SubscriptionId { get; set; }
    public long PromiseTransactionId { get; set; }
}

public class ApiResponseSubscription : ApiResponse
{
    public long Id { get; set; }
    public string? MerchantName { get; set; }
    public long MerchantId { get; set; }
    public int AmountCents { get; set; }
    public int IntervalDays { get; set; }
    public DateTime NextChargeDate { get; set; }
    public DateTime? ExpiresDate { get; set; }
    public byte StatusId { get; set; }
    public DateTime ConsentDate { get; set; }
}

public class ApiResponseMerchantTransaction : ApiResponse
{
    public long Id { get; set; }
    public long PayerId { get; set; }
    public string? PayerLogin { get; set; }
    public long? SubscriptionId { get; set; }
    public long PaymentRequestId { get; set; }
    public int AmountCents { get; set; }
    public byte TypeId { get; set; }
    public DateTime Date { get; set; }
}

public class ApiResponseMerchantList<T> : ApiResponse
{

    public System.Collections.ObjectModel.Collection <T>? Items { get; init; }

}
