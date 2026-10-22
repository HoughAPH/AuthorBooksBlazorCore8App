using System.Net;
using BlazorWebAssemblyApp.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;

namespace BlazorWebAssemblyApp.Services;

public class AccountService(HttpClient httpClient, TokenStorage tokenStorage) : IUserAccount
{
    private const string BaseUrl = "api/Account";
    private const string GenericFailure = "Error occurred. Try again later...";
    private readonly HttpClient httpClient = httpClient;

    public async Task<Result> CreateAccount(UserDTO userDTO)
    {
        using HttpResponseMessage response = await Post("register", userDTO);
        return await ResultReader.ToResult(response, "Account Created", GenericFailure);
    }

    public async Task<Result<string>> LoginAccount(LoginDTO loginDTO)
    {
        using HttpResponseMessage response = await Post("login", loginDTO);
        Result<string> result = await ResultReader.ToResult<string>(response, "Login completed", GenericFailure);

        if (result.Succeeded && !string.IsNullOrWhiteSpace(result.Value))
        {
            await tokenStorage.SetAsync(result.Value);
        }

        return result;
    }

    public async Task<Result> LogoutAccount()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"{BaseUrl}/logout");
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        await tokenStorage.ClearAsync();

        // The token is gone either way; a 401 just means the server already considered us signed out.
        return response.StatusCode == HttpStatusCode.Unauthorized
            ? Result.Ok("Already logged out")
            : await ResultReader.ToResult(response, "Logged out", "Logout failed");
    }

    public async Task<CurrentUserDto?> GetCurrentUser()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/me");
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return null;
        }

        return JsonHelper.DeserializeJsonString<CurrentUserDto>(apiResponse);
    }

    public async Task<Result> ForgotPassword(ForgotPasswordDTO user)
    {
        using HttpResponseMessage response = await Post("forgot-password", user);
        return await ResultReader.ToResult(
            response, "If the account exists, password reset instructions have been sent.", GenericFailure);
    }

    public async Task<Result> ResetPassword(ResetPasswordDTO user)
    {
        using HttpResponseMessage response = await Post("reset-password", user);
        return await ResultReader.ToResult(response, "Password reset successful", GenericFailure);
    }

    private async Task<HttpResponseMessage> Post<TBody>(string action, TBody body)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/{action}",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(body)));

        return await httpClient.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, HttpContent? content = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }
}
