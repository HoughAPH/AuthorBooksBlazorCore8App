using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.GenericModels;
using SharedClassLibrary.DTOs;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

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
            var response = await httpClient.GetAsync($"{BaseUrl}/get-users");

            if (!response.IsSuccessStatusCode)
                return new List<ManageUserDTO>();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<List<ManageUserDTO>>(apiResponse);
        }

        public async Task UpdateUserRole(string userId, string role)
        {
            var updateUserRoleDTO = new UpdateUserRoleDTO { UserId = userId, Role = role };
            var response = await httpClient.PostAsync($"{BaseUrl}/update-user-role",
                Generics.GenerateStringContent(Generics.SerializeObj(updateUserRoleDTO)));

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Error updating user role.");
        }

        public async Task DeleteUser(string userId)
        {
            var response = await httpClient.DeleteAsync($"{BaseUrl}/delete-user/{userId}");

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException("Error deleting user.");
        }
    }
}
