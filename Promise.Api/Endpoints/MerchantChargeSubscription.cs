namespace Promise.Api.Endpoints;


internal static class MerchantChargeSubscription
{
    private const byte SubscriptionStatusActive = 1;
    private const byte SubscriptionStatusExpired = 3;
    private const byte TransactionTypeCharge = 1;

    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var request = await context.Request.ReadFromJsonAsync<SubscriptionChargeRequest>().ConfigureAwait(false);
            if (request?.Auth is null || request.SubscriptionId < 1)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var merchant = MerchantAuth.Authenticate(db, request.Auth);
            if (merchant is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Invalid merchant credentials." });
            }

            var subscription = db.MerchantSubscriptions.FirstOrDefault(s => s.Id == request.SubscriptionId && s.MerchantId == merchant.UserId);
            if (subscription is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Subscription not found." });
            }
            if (subscription.StatusId != SubscriptionStatusActive)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Subscription is not active." });
            }
            if (subscription.NextChargeDate > DateTime.UtcNow)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Next charge date has not arrived yet." });
            }
            if (subscription.ExpiresDate.HasValue && subscription.ExpiresDate.Value < DateTime.UtcNow)
            {
                subscription.StatusId = SubscriptionStatusExpired;
                await db.SaveChangesAsync().ConfigureAwait(false);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Subscription has expired." });
            }

            var payerBalance = db.Balances.FirstOrDefault(b => b.UserId == subscription.SubscriberId);
            if (payerBalance is null || payerBalance.Cents < subscription.AmountCents)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Subscriber has insufficient balance." });
            }

            var hash = Security.GetHash(subscription.SubscriberId + "-" + merchant.UserId + "-" + subscription.AmountCents + "-" + DateTime.UtcNow.Ticks);
            var promiseTx = new PromiseTransaction
            {
                SenderId = subscription.SubscriberId,
                ReceiverId = merchant.UserId,
                Cents = subscription.AmountCents,
                Date = DateTime.UtcNow,
                Hash = hash,
                IsBlockchain = false,
                Memo = "Subscription charge #" + subscription.Id
            };
            db.PromiseTransactions.Add(promiseTx);

            payerBalance.Cents -= subscription.AmountCents;
            var merchantBalance = db.Balances.FirstOrDefault(b => b.UserId == merchant.UserId);
            merchantBalance?.Cents += subscription.AmountCents;

            subscription.NextChargeDate = DateTime.UtcNow.AddDays(subscription.IntervalDays);

            await db.SaveChangesAsync().ConfigureAwait(false);

            var merchantTx = new MerchantTransaction
            {
                MerchantId = merchant.UserId,
                PayerId = subscription.SubscriberId,
                SubscriptionId = subscription.Id,
                PaymentRequestId = subscription.PaymentRequestId,
                PromiseTransactionId = promiseTx.Id,
                AmountCents = subscription.AmountCents,
                TypeId = TransactionTypeCharge,
                Date = DateTime.UtcNow
            };
            db.MerchantTransactions.Add(merchantTx);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new ApiResponsePaymentResult
            {
                Success = true,
                SubscriptionId = subscription.Id,
                PromiseTransactionId = promiseTx.Id
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in MerchantChargeSubscription: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
