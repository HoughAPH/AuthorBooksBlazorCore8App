using Microsoft.AspNetCore.Components.Authorization;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using System.Security.Claims;

namespace BlazorWebAssemblyApp.Authentication;

public class ApiAuthenticationStateProvider(IUserAccount userAccount) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var currentUser = await userAccount.GetCurrentUser();
        return new AuthenticationState(CreatePrincipal(currentUser));
    }

    public Task RefreshUserStateAsync()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal CreatePrincipal(CurrentUserDto? currentUser)
    {
        if (currentUser is null || !currentUser.IsAuthenticated)
        {
            return Anonymous;
        }

        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(currentUser.Id))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, currentUser.Id));
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, currentUser.Name));
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, currentUser.Email));
        }

        if (!string.IsNullOrWhiteSpace(currentUser.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, currentUser.Role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }
}