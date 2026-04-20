namespace Promise.Common;

public static class Api
{
    private const string localPort = "7800";
    public static readonly string UrlDev = "http://localhost:" + localPort;
    public static readonly string UrlDevAndroid = "http://10.0.2.2:" + localPort;
    public static readonly string UrlProd = "https://promiseapi.azurewebsites.net";
}