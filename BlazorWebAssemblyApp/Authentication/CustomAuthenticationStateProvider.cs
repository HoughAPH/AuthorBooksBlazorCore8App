using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SharedClassLibrary.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BlazorWebAssemblyApp.Authentication;

public class CustomAuthenticationStateProvider(ILocalStorageService localStorageService) : AuthenticationStateProvider
{
    private ClaimsPrincipal anonymous = new(new ClaimsIdentity());
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await localStorageService.GetItemAsStringAsync("token");
            token = token?.Trim('"');

            if (string.IsNullOrWhiteSpace(token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;

            if (expClaim is null || 
                DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)) <= DateTimeOffset.UtcNow)
            {
                await localStorageService.RemoveItemAsync("token");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = JwtHelper.GetClaimsFromToken(token);
            var principal = JwtHelper.SetClaimPrincipal(claims);
            return new AuthenticationState(principal);
        }
        catch
        {
            await localStorageService.RemoveItemAsync("token");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task UpdateAuthenticationState(string? token)
    {
        ClaimsPrincipal claimsPrincipal = new();
        if (!string.IsNullOrWhiteSpace(token))
        {
            var userSession = JwtHelper.GetClaimsFromToken(token);
            claimsPrincipal = JwtHelper.SetClaimPrincipal(userSession);
            await localStorageService.SetItemAsStringAsync("Role", userSession.Role);
            await localStorageService.SetItemAsStringAsync("token", token);
        }
        else
        {
            claimsPrincipal = anonymous;
            await localStorageService.RemoveItemAsync("token");
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }
}
