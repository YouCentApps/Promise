using System.Data.Common;

namespace Promise.Api.Endpoints;

internal static class PayByToken
{
    private const byte TypeOneTime = 1;
    private const byte TypeSubscription = 2;
    private const byte StatusPending = 1;
    private const byte StatusCompleted = 2;
    private const byte SubscriptionStatusActive = 1;
    private const byte TransactionTypeCharge = 1;

    public static async Task<IResult> RunGet(HttpContext context, string token)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var paymentRequest = db.MerchantPaymentRequests.FirstOrDefault(r => r.Token == token);
            if (paymentRequest is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Payment request not found." });
            }
            if (paymentRequest.StatusId != StatusPending)
            {
                return Results.Json(new ApiResponsePaymentRequestInfo
                {
                    Success = false,
                    Error = "Payment request is no longer pending.",
                    StatusId = paymentRequest.StatusId
                });
            }
            if (paymentRequest.ExpiresDate.HasValue && paymentRequest.ExpiresDate.Value < DateTime.UtcNow)
            {
                return Results.Json(new ApiResponsePaymentRequestInfo { Success = false, Error = "Payment request has expired.", StatusId = 3 });
            }

            var merchant = db.Merchants.FirstOrDefault(m => m.UserId == paymentRequest.MerchantId);

            return Results.Json(new ApiResponsePaymentRequestInfo
            {
                Success = true,
                MerchantName = merchant?.Name,
                AmountCents = paymentRequest.AmountCents,
                Description = paymentRequest.Description,
                TypeId = paymentRequest.TypeId,
                StatusId = paymentRequest.StatusId,
                IntervalDays = paymentRequest.IntervalDays
            });
        }
        catch (DbException ex)
        {
            MainLogger.LogError("Error in PayByToken GET: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }

    public static async Task<IResult> RunPost(HttpContext context, string? jwtSecret)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Results.Json(new { success = false, error = "Server error..." });
            }

            var request = await context.Request.ReadFromJsonAsync<PayByTokenRequest>().ConfigureAwait(false);
            if (request?.User?.Login is null || request.User.Password is null || string.IsNullOrWhiteSpace(request.Token))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }

            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (!Security.ValidateBearerAccessToken(jwt, request.User.Login, jwtSecret))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Unauthorized." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();

            var payer = db.Users.FirstOrDefault(u => u.Login == request.User.Login);
            if (payer is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }

            var paymentRequest = db.MerchantPaymentRequests.FirstOrDefault(r => r.Token == request.Token);
            if (paymentRequest is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Payment request not found." });
            }
            if (paymentRequest.StatusId != StatusPending)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Payment request is no longer pending." });
            }
            if (paymentRequest.ExpiresDate.HasValue && paymentRequest.ExpiresDate.Value < DateTime.UtcNow)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Payment request has expired." });
            }
            if (payer.Id == paymentRequest.MerchantId)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Cannot pay to yourself." });
            }

            var payerBalance = db.Balances.FirstOrDefault(b => b.UserId == payer.Id);
            if (payerBalance is null || payerBalance.Cents < paymentRequest.AmountCents)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Insufficient balance." });
            }

            // Execute the payment via a PromiseTransaction
            var hash = Security.GetHash(payer.Id + "-" + paymentRequest.MerchantId + "-" + paymentRequest.AmountCents + "-" + DateTime.UtcNow.Ticks);
            var promiseTx = new PromiseTransaction
            {
                SenderId = payer.Id,
                ReceiverId = paymentRequest.MerchantId,
                Cents = paymentRequest.AmountCents,
                Date = DateTime.UtcNow,
                Hash = hash,
                IsBlockchain = false,
                Memo = "Payment: " + (paymentRequest.Description ?? paymentRequest.Token)
            };
            db.PromiseTransactions.Add(promiseTx);

            // Update balances
            payerBalance.Cents -= paymentRequest.AmountCents;
            var merchantBalance = db.Balances.FirstOrDefault(b => b.UserId == paymentRequest.MerchantId);
            merchantBalance?.Cents += paymentRequest.AmountCents;

            long? subscriptionId = null;

            if (paymentRequest.TypeId == TypeSubscription)
            {
                var subscription = new MerchantSubscription
                {
                    MerchantId = paymentRequest.MerchantId,
                    SubscriberId = payer.Id,
                    PaymentRequestId = paymentRequest.Id,
                    AmountCents = paymentRequest.AmountCents,
                    IntervalDays = paymentRequest.IntervalDays ?? 30,
                    NextChargeDate = DateTime.UtcNow.AddDays(paymentRequest.IntervalDays ?? 30),
                    ExpiresDate = paymentRequest.SubscriptionExpiresDate,
                    StatusId = SubscriptionStatusActive,
                    ConsentDate = DateTime.UtcNow
                };
                db.MerchantSubscriptions.Add(subscription);
                await db.SaveChangesAsync().ConfigureAwait(false);
                subscriptionId = subscription.Id;
            }
            else
            {
                await db.SaveChangesAsync().ConfigureAwait(false);
            }

            // Mark payment request completed (for one-time) or keep pending info via subscription
            if (paymentRequest.TypeId == TypeOneTime)
            {
                paymentRequest.StatusId = StatusCompleted;
            }
            else
            {
                paymentRequest.StatusId = StatusCompleted;
            }

            var merchantTx = new MerchantTransaction
            {
                MerchantId = paymentRequest.MerchantId,
                PayerId = payer.Id,
                SubscriptionId = subscriptionId,
                PaymentRequestId = paymentRequest.Id,
                PromiseTransactionId = promiseTx.Id,
                AmountCents = paymentRequest.AmountCents,
                TypeId = TransactionTypeCharge,
                Date = DateTime.UtcNow
            };
            db.MerchantTransactions.Add(merchantTx);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new ApiResponsePaymentResult
            {
                Success = true,
                SubscriptionId = subscriptionId,
                PromiseTransactionId = promiseTx.Id
            });
        }
        catch (DbException ex)
        {
            MainLogger.LogError("Error in PayByToken POST: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
