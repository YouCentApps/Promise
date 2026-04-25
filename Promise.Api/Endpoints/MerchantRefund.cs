using System.Data.Common;

namespace Promise.Api.Endpoints;

internal static class MerchantRefund
{
    private const byte TransactionTypeCharge = 1;
    private const byte TransactionTypeRefund = 2;

    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            var request = await context.Request.ReadFromJsonAsync<MerchantRefundRequest>().ConfigureAwait(false);
            if (request?.Auth is null || request.MerchantTransactionId < 1)
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

            var originalTx = db.MerchantTransactions.FirstOrDefault(t => t.Id == request.MerchantTransactionId && t.MerchantId == merchant.UserId);
            if (originalTx is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "Transaction not found." });
            }
            if (originalTx.TypeId != TransactionTypeCharge)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Can only refund a charge transaction." });
            }

            // Check if already refunded
            var existingRefund = db.MerchantTransactions.FirstOrDefault(t =>
                t.PaymentRequestId == originalTx.PaymentRequestId &&
                t.PayerId == originalTx.PayerId &&
                t.TypeId == TransactionTypeRefund &&
                t.AmountCents == originalTx.AmountCents);
            if (existingRefund is not null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "This transaction has already been refunded." });
            }

            var merchantBalance = db.Balances.FirstOrDefault(b => b.UserId == merchant.UserId);
            if (merchantBalance is null || merchantBalance.Cents < originalTx.AmountCents)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Insufficient merchant balance for refund." });
            }

            // Create reverse PromiseTransaction
            var hash = Security.GetHash(merchant.UserId + "-" + originalTx.PayerId + "-" + originalTx.AmountCents + "-refund-" + DateTime.UtcNow.Ticks);
            var promiseTx = new PromiseTransaction
            {
                SenderId = merchant.UserId,
                ReceiverId = originalTx.PayerId,
                Cents = originalTx.AmountCents,
                Date = DateTime.UtcNow,
                Hash = hash,
                IsBlockchain = false,
                Memo = "Refund for transaction #" + originalTx.Id
            };
            db.PromiseTransactions.Add(promiseTx);

            merchantBalance.Cents -= originalTx.AmountCents;
            var payerBalance = db.Balances.FirstOrDefault(b => b.UserId == originalTx.PayerId);
            payerBalance?.Cents += originalTx.AmountCents;

            await db.SaveChangesAsync().ConfigureAwait(false);

            var refundTx = new MerchantTransaction
            {
                MerchantId = merchant.UserId,
                PayerId = originalTx.PayerId,
                SubscriptionId = originalTx.SubscriptionId,
                PaymentRequestId = originalTx.PaymentRequestId,
                PromiseTransactionId = promiseTx.Id,
                AmountCents = originalTx.AmountCents,
                TypeId = TransactionTypeRefund,
                Date = DateTime.UtcNow
            };
            db.MerchantTransactions.Add(refundTx);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new ApiResponsePaymentResult
            {
                Success = true,
                PromiseTransactionId = promiseTx.Id
            });
        }
        catch (DbException ex)
        {
            MainLogger.LogError("Error in MerchantRefund: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
