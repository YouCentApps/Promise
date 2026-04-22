using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Promise.Native;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = "https",
    DataHost = "promiseapi.azurewebsites.net",
    DataPathPrefix = "/pay/",
    AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent?.Data is not null)
        {
            Platform.CurrentActivity?.Intent?.SetData(intent.Data);
        }
    }
}
