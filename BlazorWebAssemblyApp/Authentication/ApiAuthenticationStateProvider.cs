using Microsoft.AspNetCore.Components.Authorization;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Security.Claims;

namespace BlazorWebAssemblyApp.Authentication;

public class ApiAuthenticationStateProvider(IUserAccount userAccount) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private AuthenticationState? _cachedState;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!OperatingSystem.IsBrowser())
        {
            return new AuthenticationState(Anonymous);
        }

        if (_cachedState is not null)
        {
            return _cachedState;
        }

        CurrentUserDto? currentUser = await userAccount.GetCurrentUser();
        _cachedState = new AuthenticationState(CreatePrincipal(currentUser));
        return _cachedState;
    }

    // FIX: Await the new state generation properly before notifying Blazor
    public async Task RefreshUserStateAsync()
    {
        ClearCache();
        Task<AuthenticationState> newStateTask = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(newStateTask);
        await newStateTask; // Ensure Blazor registers the update
    }

    public void NotifyUserLogout()
    {
        _cachedState = new AuthenticationState(Anonymous);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
    }

    private static ClaimsPrincipal CreatePrincipal(CurrentUserDto? currentUser)
    {
        if (currentUser is null || !currentUser.IsAuthenticated)
        {
            return Anonymous;
        }

        List<Claim> claims =
        [
            new(ClaimTypes.Name, currentUser.Email!),
        new(ClaimTypes.Surname, currentUser.Name ?? string.Empty),
        new(ClaimTypes.NameIdentifier, currentUser.Id ?? string.Empty)
        ];

        foreach (string role in currentUser.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // FIX: Ensure the claims list AND the authentication type string 
        // are passed into the exact same constructor signature.
        ClaimsIdentity identity = new(claims, authenticationType: "JwtAuth");

        return new ClaimsPrincipal(identity);
    }

    public void ClearCache()
    {
        _cachedState = null;
    }
}
