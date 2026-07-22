namespace Promise.Native;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Must be public for XAML binding in MAUI projects")]
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "App is the standard application class name in MAUI projects")]
public partial class App : Application
{
	public static string? PendingPaymentToken { get; set; }

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

	protected override void OnAppLinkRequestReceived(Uri uri)
	{
		base.OnAppLinkRequestReceived(uri);
		ArgumentNullException.ThrowIfNull(uri);
		if (uri.AbsolutePath.StartsWith("/pay/", StringComparison.OrdinalIgnoreCase))
		{
			var token = uri.AbsolutePath["/pay/".Length..];
			if (!string.IsNullOrEmpty(token))
			{
				PendingPaymentToken = token;
				// Navigate on the main thread
				MainThread.BeginInvokeOnMainThread(() =>
				{
					if (Windows.Count > 0)
					{
						Shell.Current?.GoToAsync($"//MainPage");
					}
				});
			}
		}
	}
}
