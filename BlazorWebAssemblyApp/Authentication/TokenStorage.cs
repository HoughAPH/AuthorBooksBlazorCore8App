using Microsoft.JSInterop;

namespace BlazorWebAssemblyApp.Authentication;

public class TokenStorage(IJSRuntime js)
{
    private const string Key = "authToken";

    public async ValueTask<string?> GetAsync() => await js.InvokeAsync<string?>("localStorage.getItem", Key);

    public async ValueTask SetAsync(string token) => await js.InvokeVoidAsync("localStorage.setItem", Key, token);

    public async ValueTask ClearAsync() => await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
