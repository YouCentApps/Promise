namespace Promise.Api.Endpoints;

internal static class MerchantCreatePaymentRequest
{
    private const byte TypeOneTime = 1;
    private const byte TypeSubscription = 2;
    private const byte StatusPending = 1;

    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var request = await context.Request.ReadFromJsonAsync<CreatePaymentRequestInput>().ConfigureAwait(false);
            if (request?.Auth is null || request.AmountCents < 1 || (request.TypeId != TypeOneTime && request.TypeId != TypeSubscription))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }
            if (request.TypeId == TypeSubscription && (request.IntervalDays is null || request.IntervalDays < 1))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Subscription requires IntervalDays >= 1." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var merchant = MerchantAuth.Authenticate(db, request.Auth);
            if (merchant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid merchant credentials." });
            }

            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            var paymentRequest = new MerchantPaymentRequest
            {
                Token = token,
                MerchantId = merchant.UserId,
                AmountCents = request.AmountCents,
                Description = request.Description,
                TypeId = request.TypeId,
                StatusId = StatusPending,
                CallbackUrl = request.CallbackUrl,
                CreatedDate = DateTime.UtcNow,
                ExpiresDate = request.ExpiresDate,
                IntervalDays = request.IntervalDays,
                SubscriptionExpiresDate = request.SubscriptionExpiresDate
            };

            db.MerchantPaymentRequests.Add(paymentRequest);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new ApiResponsePaymentRequest
            {
                Success = true,
                Id = paymentRequest.Id,
                Token = token,
                PaymentUrl = new Uri("/pay/" + token, UriKind.Relative),
                AmountCents = paymentRequest.AmountCents,
                Description = paymentRequest.Description,
                TypeId = paymentRequest.TypeId,
                StatusId = paymentRequest.StatusId,
                CreatedDate = paymentRequest.CreatedDate,
                ExpiresDate = paymentRequest.ExpiresDate,
                IntervalDays = paymentRequest.IntervalDays,
                SubscriptionExpiresDate = paymentRequest.SubscriptionExpiresDate
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in MerchantCreatePaymentRequest: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
