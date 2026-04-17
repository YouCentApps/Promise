
using Microsoft.AspNetCore.Components;

namespace Promise.Web;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class WebNavigationManager : INavigationManager
{
    private readonly NavigationManager _navigationManager;

    public WebNavigationManager(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public Task NavigateToAsync(string route)
    {
        _navigationManager.NavigateTo(route);
        return Task.CompletedTask;
    }
}