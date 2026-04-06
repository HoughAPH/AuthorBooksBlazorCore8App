using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
using System.Net.Http.Headers;

namespace BlazorWebAssemblyApp.Services
{
    public class ManageUsersService : IManageUsers
    {
        private const string BaseUrl = "api/ManageUsers";
        private readonly HttpClient httpClient;
        private readonly ILocalStorageService localStorageService;

        public ManageUsersService(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            this.httpClient = httpClient;
            this.localStorageService = localStorageService;
        }

        public async Task<List<ManageUserDTO>> GetUsers()
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/get-users");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<List<ManageUserDTO>>(apiResponse);
        }

        public async Task UpdateUserRole(string userId, string role)
        {
            await SetAuthorizationHeaderAsync();

            var updateUserRoleDTO = new UpdateUserRoleDTO
            {
                UserId = userId,
                Role = role
            };

            var response = await httpClient.PostAsync(
                $"{BaseUrl}/update-user-role",
                Generics.GenerateStringContent(Generics.SerializeObj(updateUserRoleDTO)));

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteUser(string userId)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.DeleteAsync($"{BaseUrl}/delete-user/{userId}");
            response.EnsureSuccessStatusCode();
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
