using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using System.Net.Http.Headers;

namespace BlazorWebAssemblyApp.Services
{
    public class AuthorsService : IAuthors
    {
        public AuthorsService(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            this.httpClient = httpClient;
            this.localStorageService = localStorageService;
        }

        private const string BaseUrl = "api/Author";
        private readonly HttpClient httpClient;
        private readonly ILocalStorageService localStorageService;

        public async Task<List<AuthorDTO>> GetAuthors()
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthors");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. JsonHelper.DeserializeJsonString<AuthorDTO[]>(apiResponse)];
        }

        public async Task<AuthorDTO> GetAuthor(int id)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthor/{id}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<AuthorDTO>(apiResponse);
        }

        public async Task<int> AddAuthor(AuthorDTO author)
        {
            await SetAuthorizationHeaderAsync();

            StringContent content = JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author));
            var response = await httpClient.PostAsync($"{BaseUrl}/AddAuthor", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateAuthor(int id, AuthorDTO author)
        {
            await SetAuthorizationHeaderAsync();

            StringContent content = JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author));
            var response = await httpClient.PutAsync($"{BaseUrl}/UpdateAuthor/{id}", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<string> DeleteAuthor(int id)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.DeleteAsync($"{BaseUrl}/DeleteAuthor/{id}");
            if (response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var error = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(error)
                ? response.ReasonPhrase ?? "Error deleting author."
                : error;
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