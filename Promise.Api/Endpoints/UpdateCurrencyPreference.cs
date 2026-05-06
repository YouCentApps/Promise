using System.Data.Common;
using System.Text.Json;

namespace Promise.Api.Endpoints;

internal static class UpdateCurrencyPreference
{
    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();

            CurrencyPreferenceUpdate? request = null;
            try
            {
                request = await context.Request.ReadFromJsonAsync<CurrencyPreferenceUpdate>().ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error reading currency preference update request: " + ex);
                return Results.Json(new { success = false, error = "Server error..." });
            }

            var user = request?.User;
            if (request is null || user is null || user.Id < 1 ||
                string.IsNullOrWhiteSpace(user.Login) ||
                string.IsNullOrWhiteSpace(user.Password))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                MainLogger.LogError("Error reading user from currency preference update request");
                return Results.Json(new { success = false, error = "No data or wrong data provided" });
            }

            // Authenticate user
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Login == user.Login).ConfigureAwait(false);
            if (dbUser is null || dbUser.Password is null || dbUser.Salt is null || dbUser.Id != user.Id)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found" });
            }

            var hash = Security.GetPasswordHash(user.Password, dbUser.Salt);
            if (hash != dbUser.Password)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Results.Json(new { success = false, error = "Wrong password!" });
            }

            // Update currency preference
            bool updated = await CurrencyService.UpdateUserCurrencyAsync(db, user.Id, request.CurrencyId).ConfigureAwait(false);

            if (!updated)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "Failed to update currency preference. Currency may not exist." });
            }

            return Results.Json(new { success = true });
        }
        catch (DbException ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error in UpdateCurrencyPreference: " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
    }
}
