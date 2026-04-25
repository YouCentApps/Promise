using Microsoft.JSInterop;

namespace Promise.Web;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by dependency injection")]
internal sealed class SessionStorage : ISessionStorage
{
    private readonly IJSRuntime _jsRuntime;

    public SessionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        return await _jsRuntime.InvokeAsync<T>("localStorage.getItem", key).ConfigureAwait(true);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value).ConfigureAwait(true);
    }

    public async Task RemoveAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key).ConfigureAwait(true);
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.clear").ConfigureAwait(true);
    }
}
