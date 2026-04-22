namespace Promise.Native.Services;

public sealed class NativeNavigationManager : INavigationManager
{
    public Task NavigateToAsync(string route)
    {
        // Handle special case for update page with three slashes - clears navigation history
        if (route == "///UpdateApp")
        {
            return Shell.Current.GoToAsync("//UpdateApp");
        }
        
        return Shell.Current.GoToAsync(route);
    }
}