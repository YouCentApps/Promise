namespace Promise.Native.Services;

public sealed class MyEnvironment : IMyEnvironment
{

    public const string Prod = "Production";
    public const string Dev = "Development";
    public const string Unknown = "Unknown";
    public const string MauiEnviroment = "MAUI_ENVIRONMENT";

    public bool IsNative() => true;
    public bool IsWeb() => false;
    public bool IsCutOff()
    {
        var current = Connectivity.Current.NetworkAccess;
        return current != NetworkAccess.Internet;
    }

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
        return Environment.GetEnvironmentVariable(MauiEnviroment) ?? Unknown;
    }


}