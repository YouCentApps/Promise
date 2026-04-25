namespace Promise.Web;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class MyEnvironment : IMyEnvironment
{
    public const string Prod = "Production";
    public const string Dev = "Development";
    public const string Unknown = "Unknown";

    public bool IsNative() => false;
    public bool IsWeb() => true;
    public bool IsCutOff() => false;

    public bool IsDevelopment()
    {
        return GetEnvironment() == Dev;
    }
    public bool IsProduction()
    {
        return GetEnvironment() == Prod;
    }
    public string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Unknown;
    }
}
