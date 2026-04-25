namespace Promise.Web.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class Settings(IMyEnvironment myEnvironment) : ISettings
{
    private readonly IMyEnvironment myEnv = myEnvironment;

    public string ApiUrl
    {
        get
        {
            if (myEnv.IsProduction())
            {
                return Api.UrlProd;
            }
            if (myEnv.IsDevelopment())
            {
                return Api.UrlDev;
            }
            return string.Empty;
        }
    }
}