namespace Promise.Native.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class NativeSettings(IMyEnvironment myEnvironment) : ISettings
{
    private readonly IMyEnvironment myEnv = myEnvironment;
    private static bool IsAndroid() => DeviceInfo.Current.Platform == DevicePlatform.Android;
    private static bool IsiOS() => DeviceInfo.Current.Platform == DevicePlatform.iOS;
    private static bool IsmacOS() => DeviceInfo.Current.Platform == DevicePlatform.macOS;
    private static bool IsMacCatalyst() => DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst;
    private static bool IsWinUI() => DeviceInfo.Current.Platform == DevicePlatform.WinUI;

    public string ApiEndpoint
    {
        get
        {
            if (myEnv.IsProduction())
            {
                return Api.UrlProd;
            }
            if (myEnv.IsDevelopment())
            {
                if (IsAndroid())
                {
                    return Api.UrlDevAndroid;
                }
                return Api.UrlDev;
            }
            return string.Empty;
        }
    }
}
