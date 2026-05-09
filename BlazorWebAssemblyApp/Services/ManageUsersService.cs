using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;

namespace BlazorWebAssemblyApp.Services;

public class ManageUsersService(HttpClient httpClient) : IManageUsers
{
    private const string BaseUrl = "api/ManageUsers";
    private readonly HttpClient httpClient = httpClient;

    public async Task<List<ManageUserDTO>> GetUsers()
    {
        using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/get-users");
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<List<ManageUserDTO>>(apiResponse);
    }

    public async Task UpdateUserRole(string userId, string role)
    {
        var updateUserRoleDTO = new UpdateUserRoleDTO
        {
            UserId = userId,
            Role = role
        };

        using var request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/update-user-role",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(updateUserRoleDTO)));

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUser(string userId)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"{BaseUrl}/delete-user/{userId}");
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
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
