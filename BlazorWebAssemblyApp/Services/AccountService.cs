using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;
using System.Net;
using static SharedClassLibrary.Models.ServiceResponses;

namespace BlazorWebAssemblyApp.Services;

public class AccountService(HttpClient httpClient) : IUserAccount, IWeather
{
    private const string BaseUrl = "api/Account";
    private readonly HttpClient httpClient = httpClient;

    public async Task<GeneralResponse> CreateAccount(UserDTO userDTO)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/register",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(userDTO)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string apiResponse = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return new GeneralResponse(false, "Error occurred. Try again later...");
        }

        return JsonHelper.DeserializeJsonString<GeneralResponse>(apiResponse);
    }

    public async Task<LoginResponse> LoginAccount(LoginDTO loginDTO)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/login",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(loginDTO)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string apiResponse = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return new LoginResponse(false, string.Empty, "Error occurred. Try again later...");
        }

        return JsonHelper.DeserializeJsonString<LoginResponse>(apiResponse);
    }

    public async Task<GeneralResponse> LogoutAccount()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"{BaseUrl}/logout");
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new GeneralResponse(true, "Already logged out");
        }

        string apiResponse = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return response.IsSuccessStatusCode
                ? new GeneralResponse(true, "Logged out")
                : new GeneralResponse(false, "Logout failed");
        }

        return JsonHelper.DeserializeJsonString<GeneralResponse>(apiResponse);
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

    public async Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/forgot-password",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(user)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string apiResponse = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return (false, "Error occurred. Try again later...");
        }

        ForgotPasswordResponse result = JsonHelper.DeserializeJsonString<ForgotPasswordResponse>(apiResponse);
        return (result.Success, result.Token);
    }

    public async Task<(bool, string)> ResetPassword(ResetPasswordDTO user)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/reset-password",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(user)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        string apiResponse = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return (false, "Error occurred. Try again later...");
        }

        ApiResponse result = JsonHelper.DeserializeJsonString<ApiResponse>(apiResponse);
        return (response.IsSuccessStatusCode, result.Message);
    }

    public async Task<WeatherForecast[]> GetWeatherForecast()
    {
        CurrentUserDto? currentUser = await GetCurrentUser();
        if (currentUser is null || !currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.Role))
        {
            return [];
        }

        string route = currentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ? "api/WeatherForecast/admin"
            : "api/WeatherForecast/user";

        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, route);
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        string result = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonStringList<WeatherForecast>(result)];
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }
}
