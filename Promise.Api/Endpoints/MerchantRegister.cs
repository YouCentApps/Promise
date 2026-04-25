using System.Data.Common;

namespace Promise.Api.Endpoints;

internal static class MerchantRegister
{
    public static async Task<IResult> Run(HttpContext context, string? jwtSecret)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            if (jwtSecret is null)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("No secret provided for JWT");
                return Results.Json(new { success = false, error = "Server error... Please try again later." });
            }
            var request = await context.Request.ReadFromJsonAsync<MerchantRegisterRequest>().ConfigureAwait(false);
            if (request?.User?.Login is null || request.User.Password is null || string.IsNullOrWhiteSpace(request.MerchantName))
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
            var dbUser = db.Users.FirstOrDefault(u => u.Login == request.User.Login);
            if (dbUser is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found." });
            }

            var existing = db.Merchants.FirstOrDefault(m => m.UserId == dbUser.Id);
            if (existing is not null)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                return Results.Json(new { success = false, error = "User is already a merchant." });
            }

            var apiKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var apiSecret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var salt = Security.GetSalt();
            var apiSecretHash = Security.GetPasswordHash(apiSecret, salt);

            var merchant = new Merchant
            {
                UserId = dbUser.Id,
                Name = request.MerchantName,
                Website = request.Website,
                ApiKey = apiKey,
                ApiSecretHash = apiSecretHash,
                Salt = salt,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            db.Merchants.Add(merchant);
            await db.SaveChangesAsync().ConfigureAwait(false);

            return Results.Json(new ApiResponseMerchant
            {
                Success = true,
                UserId = dbUser.Id,
                Name = merchant.Name,
                Website = merchant.Website,
                ApiKey = apiKey,
                ApiSecret = apiSecret,
                IsActive = true
            });
        }
        catch (DbException ex)
        {
            MainLogger.LogError("Error in MerchantRegister: " + ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
