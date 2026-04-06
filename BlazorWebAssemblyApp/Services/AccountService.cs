using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
using System.Net.Http.Headers;
using static SharedClassLibrary.DTOs.ServiceResponses;

namespace BlazorWebAssemblyApp.Services
{
    public class AccountService(HttpClient httpClient, ILocalStorageService localStorageService) : IUserAccount, IWeather
    {
        private const string BaseUrl = "api/Account";
        private readonly HttpClient httpClient = httpClient;
        private readonly ILocalStorageService localStorageService = localStorageService;

        public async Task<GeneralResponse> CreateAccount(UserDTO userDTO)
        {
            var response = await httpClient.PostAsync(
                $"{BaseUrl}/register",
                Generics.GenerateStringContent(Generics.SerializeObj(userDTO)));

            var apiResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(apiResponse))
            {
                return new GeneralResponse(false, "Error occurred. Try again later...");
            }

            return Generics.DeserializeJsonString<GeneralResponse>(apiResponse);
        }

        public async Task<LoginResponse> LoginAccount(LoginDTO loginDTO)
        {
            var response = await httpClient.PostAsync(
                $"{BaseUrl}/login",
                Generics.GenerateStringContent(Generics.SerializeObj(loginDTO)));

            var apiResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(apiResponse))
            {
                return new LoginResponse(false, null!, "Error occurred. Try again later...");
            }

            return Generics.DeserializeJsonString<LoginResponse>(apiResponse);
        }

        public async Task<(bool, string)> ForgotPassword(ForgotPasswordDTO user)
        {
            var response = await httpClient.PostAsync(
                $"{BaseUrl}/forgot-password",
                Generics.GenerateStringContent(Generics.SerializeObj(user)));

            var apiResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(apiResponse))
            {
                return (false, "Error occurred. Try again later...");
            }

            var result = Generics.DeserializeJsonString<ForgotPasswordResponse>(apiResponse);
            return (result.Success, result.Token);
        }

        public async Task<(bool, string)> ResetPassword(ResetPasswordDTO user)
        {
            var response = await httpClient.PostAsync(
                $"{BaseUrl}/reset-password",
                Generics.GenerateStringContent(Generics.SerializeObj(user)));

            var apiResponse = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(apiResponse))
            {
                return (false, "Error occurred. Try again later...");
            }

            var result = Generics.DeserializeJsonString<ApiResponse>(apiResponse);
            return (response.IsSuccessStatusCode, result.Message);
        }

        public async Task<WeatherForecast[]> GetWeatherForecast()
        {
            var token = await localStorageService.GetItemAsStringAsync("token");
            token = token?.Trim('"');

            if (string.IsNullOrWhiteSpace(token))
            {
                return [];
            }

            var claims = Generics.GetClaimsFromToken(token);
            await SetAuthorizationHeaderAsync();

            var response = claims.Role == "Admin"
                ? await httpClient.GetAsync("api/WeatherForecast/admin")
                : await httpClient.GetAsync("api/WeatherForecast/user");

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var result = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonStringList<WeatherForecast>(result)];
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await localStorageService.GetItemAsStringAsync("token");
            token = token?.Trim('"');

            httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
