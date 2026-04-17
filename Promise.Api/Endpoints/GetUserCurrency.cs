using Microsoft.EntityFrameworkCore;

namespace Promise.Api;

internal static class GetUserCurrency
{
    public static async Task<IResult> Run(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            using var db = context.RequestServices.GetRequiredService<PromiseDb>();

            User? user = null;
            try
            {
                user = await context.Request.ReadFromJsonAsync<User>().ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                MainLogger.LogError("Error reading user from get currency request: " + ex);
                return Results.Json(new { success = false, error = "Server error..." });
            }
#pragma warning restore CA1031

            if (user is null || user.Id < 1 || string.IsNullOrWhiteSpace(user.Login))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Results.Json(new { success = false, error = "No user data provided" });
            }

            // Verify user exists
            var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Login == user.Login && u.Id == user.Id).ConfigureAwait(false);
            if (dbUser is null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Results.Json(new { success = false, error = "User not found" });
            }

            // Get user currency info
            var (currency, rate) = await CurrencyService.GetUserCurrencyInfoAsync(db, user.Id).ConfigureAwait(false);

            // Get all available currencies
            var allCurrencies = await CurrencyService.GetAllCurrenciesAsync(db).ConfigureAwait(false);

            if (allCurrencies.Count == 0)
            {
                return Results.Json(new { success = false, error = "No currencies available in system" });
            }

            // If user doesn't have settings yet, use first available currency as default
            if (currency is null || rate is null)
            {
                currency = allCurrencies.First();
                rate = await db.Rates.FirstOrDefaultAsync(r => r.CurrencyId == currency.Id).ConfigureAwait(false);

                if (rate is null)
                {
                    return Results.Json(new { success = false, error = "No exchange rates available" });
                }
            }

            return Results.Json(new 
            { 
                success = true, 
                currencyId = currency.Id,
                currencyCode = currency.Code,
                currencyName = currency.Name,
                amountFor100 = rate.AmountFor100,
                updateDate = rate.UpdateDate,
                availableCurrencies = allCurrencies
            });
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            MainLogger.LogError("Error in GetUserCurrency: " + ex);
            return Results.Json(new { success = false, error = "Server error..." });
        }
#pragma warning restore CA1031
    }
}
