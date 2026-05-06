namespace Promise.Api.Helpers;

internal static class CurrencyService
{
    /// <summary>
    /// Convert Promise cents to user's currency amount
    /// </summary>
    public static decimal ConvertPromisesToCurrency(long promiseCents, double amountFor100)
    {
        // AmountFor100 is how much currency equals 100 Promise cents
        return (decimal)(promiseCents * amountFor100 / 100.0);
    }

    /// <summary>
    /// Convert currency amount to Promise cents (rounded to whole number)
    /// </summary>
    public static long ConvertCurrencyToPromises(decimal currencyAmount, double amountFor100)
    {
        // Convert currency to Promise cents and round to nearest whole number
        return (long)Math.Round((double)(currencyAmount * 100 / (decimal)amountFor100));
    }

    /// <summary>
    /// Get user's currency and exchange rate
    /// </summary>
    public static async Task<(Currency? currency, Rate? rate)> GetUserCurrencyInfoAsync(PromiseDb db, long userId)
    {
        ArgumentNullException.ThrowIfNull(db);
        var userSettings = await db.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId).ConfigureAwait(false);

        if (userSettings == null)
            return (null, null);

        var currency = await db.Currencies
            .FirstOrDefaultAsync(c => c.Id == userSettings.CurrencyId).ConfigureAwait(false);

        var rate = await db.Rates
            .FirstOrDefaultAsync(r => r.CurrencyId == userSettings.CurrencyId).ConfigureAwait(false);

        return (currency, rate);
    }

    /// <summary>
    /// Get all available currencies
    /// </summary>
    public static async Task<List<Currency>> GetAllCurrenciesAsync(PromiseDb db)
    {
        ArgumentNullException.ThrowIfNull(db);
        return await db.Currencies.ToListAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Update user's currency preference
    /// </summary>
    public static async Task<bool> UpdateUserCurrencyAsync(PromiseDb db, long userId, byte currencyId)
    {
        ArgumentNullException.ThrowIfNull(db);
        var userSettings = await db.UserSettings
            .FirstOrDefaultAsync(us => us.UserId == userId).ConfigureAwait(false);

        if (userSettings == null)
            return false;

        // Verify currency exists
        var currencyExists = await db.Currencies.AnyAsync(c => c.Id == currencyId).ConfigureAwait(false);
        if (!currencyExists)
            return false;

        userSettings.CurrencyId = currencyId;
        await db.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }
}
