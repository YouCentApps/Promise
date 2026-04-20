namespace Promise.Api.Endpoints;

internal static class MerchantQueries
{
    public static async Task<IResult> GetPaymentRequests(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var auth = await context.Request.ReadFromJsonAsync<MerchantApiAuth>().ConfigureAwait(false);
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var merchant = MerchantAuth.Authenticate(db, auth);
            if (merchant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid merchant credentials." });
            }

            var requests = db.MerchantPaymentRequests
                .Where(r => r.MerchantId == merchant.UserId)
                .OrderByDescending(r => r.CreatedDate)
                .Take(100)
                .Select(r => new ApiResponsePaymentRequest
                {
                    Success = true,
                    Id = r.Id,
                    Token = r.Token,
                    AmountCents = r.AmountCents,
                    Description = r.Description,
                    TypeId = r.TypeId,
                    StatusId = r.StatusId,
                    CreatedDate = r.CreatedDate,
                    ExpiresDate = r.ExpiresDate,
                    IntervalDays = r.IntervalDays,
                    SubscriptionExpiresDate = r.SubscriptionExpiresDate
                })
                .ToList();

            return Results.Json(new ApiResponseMerchantList<ApiResponsePaymentRequest> { Success = true, Items = new System.Collections.ObjectModel.Collection<ApiResponsePaymentRequest>(requests) });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in MerchantQueries.GetPaymentRequests: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }

    public static async Task<IResult> GetSubscriptions(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var auth = await context.Request.ReadFromJsonAsync<MerchantApiAuth>().ConfigureAwait(false);
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var merchant = MerchantAuth.Authenticate(db, auth);
            if (merchant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid merchant credentials." });
            }

            var subs = db.MerchantSubscriptions
                .Where(s => s.MerchantId == merchant.UserId)
                .OrderByDescending(s => s.ConsentDate)
                .Take(100)
                .Select(s => new ApiResponseSubscription
                {
                    Success = true,
                    Id = s.Id,
                    MerchantId = s.MerchantId,
                    AmountCents = s.AmountCents,
                    IntervalDays = s.IntervalDays,
                    NextChargeDate = s.NextChargeDate,
                    ExpiresDate = s.ExpiresDate,
                    StatusId = s.StatusId,
                    ConsentDate = s.ConsentDate
                })
                .ToList();

            return Results.Json(new ApiResponseMerchantList<ApiResponseSubscription> { Success = true, Items = new System.Collections.ObjectModel.Collection<ApiResponseSubscription>(subs) });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in MerchantQueries.GetSubscriptions: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }

    public static async Task<IResult> GetTransactions(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var auth = await context.Request.ReadFromJsonAsync<MerchantApiAuth>().ConfigureAwait(false);
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var merchant = MerchantAuth.Authenticate(db, auth);
            if (merchant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid merchant credentials." });
            }

            var txs = db.MerchantTransactions
                .Where(t => t.MerchantId == merchant.UserId)
                .OrderByDescending(t => t.Date)
                .Take(100)
                .Select(t => new ApiResponseMerchantTransaction
                {
                    Success = true,
                    Id = t.Id,
                    PayerId = t.PayerId,
                    SubscriptionId = t.SubscriptionId,
                    PaymentRequestId = t.PaymentRequestId,
                    AmountCents = t.AmountCents,
                    TypeId = t.TypeId,
                    Date = t.Date
                })
                .ToList();

            return Results.Json(new ApiResponseMerchantList<ApiResponseMerchantTransaction> { Success = true, Items = new System.Collections.ObjectModel.Collection<ApiResponseMerchantTransaction>(txs) });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in MerchantQueries.GetTransactions: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
