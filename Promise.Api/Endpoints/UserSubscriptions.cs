namespace Promise.Api.Endpoints;

internal static class UserSubscriptions
{
    private const byte SubscriptionStatusActive = 1;
    private const byte SubscriptionStatusCancelled = 2;

    public static async Task<IResult> GetSubscriptions(HttpContext context, string? jwtSecret)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Results.Json(new { success = false, error = "Server error..." });
            }

            var request = await context.Request.ReadFromJsonAsync<User>().ConfigureAwait(false);
            if (request?.Login is null || request.Password is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }

            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (!Security.ValidateBearerAccessToken(jwt, request.Login, jwtSecret))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Unauthorized." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var user = db.Users.FirstOrDefault(u => u.Login == request.Login);
            if (user is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }

            var subs = db.MerchantSubscriptions
                .Where(s => s.SubscriberId == user.Id)
                .OrderByDescending(s => s.ConsentDate)
                .Take(100)
                .ToList()
                .Select(s =>
                {
                    var merchant = db.Merchants.FirstOrDefault(m => m.UserId == s.MerchantId);
                    return new ApiResponseSubscription
                    {
                        Success = true,
                        Id = s.Id,
                        MerchantName = merchant?.Name,
                        MerchantId = s.MerchantId,
                        AmountCents = s.AmountCents,
                        IntervalDays = s.IntervalDays,
                        NextChargeDate = s.NextChargeDate,
                        ExpiresDate = s.ExpiresDate,
                        StatusId = s.StatusId,
                        ConsentDate = s.ConsentDate
                    };
                })
                .ToList();

            return Results.Json(new ApiResponseMerchantList<ApiResponseSubscription> { Success = true, Items = new System.Collections.ObjectModel.Collection<ApiResponseSubscription>(subs) });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in UserSubscriptions.GetSubscriptions: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }

    public static async Task<IResult> CancelSubscription(HttpContext context, string? jwtSecret, long subscriptionId)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Results.Json(new { success = false, error = "Server error..." });
            }

            var request = await context.Request.ReadFromJsonAsync<User>().ConfigureAwait(false);
            if (request?.Login is null || request.Password is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No data or wrong data provided." });
            }

            var jwt = context.Request.Headers[Security.AuthorizationHttpHeader].ToString();
            if (!Security.ValidateBearerAccessToken(jwt, request.Login, jwtSecret))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Results.Json(new { success = false, error = "Unauthorized." });
            }

            using var db = context.RequestServices.GetRequiredService<PromiseDb>();
            var user = db.Users.FirstOrDefault(u => u.Login == request.Login);
            if (user is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }

            var subscription = db.MerchantSubscriptions.FirstOrDefault(s => s.Id == subscriptionId && s.SubscriberId == user.Id);
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

            subscription.StatusId = SubscriptionStatusCancelled;
            subscription.CancelledDate = DateTime.UtcNow;
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new { success = true });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            MainLogger.LogError("Error in UserSubscriptions.CancelSubscription: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
