namespace Promise.Native;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Must be public for XAML binding in MAUI projects")]
public partial class UpdateApp : ContentPage
{
	private readonly AppState _appState;

	public UpdateApp(AppState appState)
	{
		InitializeComponent();
		_appState = appState;
	}

	protected override bool OnBackButtonPressed()
	{
		// If an update is required, prevent going back
		if (_appState.IsUpdateRequired)
		{
			return true; // Consume the back button press
		}
		
		return base.OnBackButtonPressed();
	}

	private async void UpdateForAndroid(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://play.google.com/store/apps/details?id=app.YouCent")).ConfigureAwait(false);
	}

	private async void UpdateForiOS(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://apps.apple.com/us/app/youcent-pay-without-money/id1620023350")).ConfigureAwait(false);
	}

	private async void UpdateForWindows(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://apps.microsoft.com/detail/9p40rw7nmzd4")).ConfigureAwait(false);
	}

	//TODO: Add the link for MacOS later when its version up and running (for now just using iOS link)
	private async void UpdateForMacOS(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://apps.apple.com/us/app/youcent-pay-without-money/id1620023350")).ConfigureAwait(false);
	}

	private async void UpdateForWeb(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://promisesite.azurewebsites.net/")).ConfigureAwait(false);
	}

	// VisitWebsite
	private async void VisitWebsite(object sender, EventArgs e)
	{
		await Launcher.OpenAsync(new Uri("https://youcent.app")).ConfigureAwait(false);
	}
}
