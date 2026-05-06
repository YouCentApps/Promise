
using Microsoft.AspNetCore.Components;

namespace Promise.Web.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class WebNavigationManager(NavigationManager navigationManager) : INavigationManager
{
    private readonly NavigationManager _navigationManager = navigationManager;

    public Task NavigateToAsync(string route)
    {
        _navigationManager.NavigateTo(route);
        return Task.CompletedTask;
    }
}