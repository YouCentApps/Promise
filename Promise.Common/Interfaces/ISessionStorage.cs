namespace Promise.Common;

public interface ISessionStorage
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task RemoveAsync(string key);
    Task ClearAsync();
}
