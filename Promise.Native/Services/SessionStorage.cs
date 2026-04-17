using Microsoft.JSInterop;

namespace Promise.Native;

#pragma warning disable CA2007 // IJSRuntime requires synchronization context; ConfigureAwait(false) would break it
internal sealed class SessionStorage : ISessionStorage
{
    private readonly IJSRuntime _jsRuntime;

    public SessionStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        return await _jsRuntime.InvokeAsync<T>("localStorage.getItem", key);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public async Task RemoveAsync(string key)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}