namespace Promise.Api.Helpers;

internal static class MerchantAuth
{
    public static Merchant? Authenticate(PromiseDb db, MerchantApiAuth? auth)
    {
        if (auth?.ApiKey is null || auth.ApiSecret is null) return null;
        var merchant = db.Merchants.FirstOrDefault(m => m.ApiKey == auth.ApiKey && m.IsActive);
        if (merchant is null) return null;
        var hash = Security.GetPasswordHash(auth.ApiSecret, merchant.Salt!);
        if (hash != merchant.ApiSecretHash) return null;
        return merchant;
    }
}
