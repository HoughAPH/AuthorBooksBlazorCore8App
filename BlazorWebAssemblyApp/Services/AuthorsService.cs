using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;

namespace BlazorWebAssemblyApp.Services
{
    public class AuthorsService(HttpClient httpClient) : IAuthors
    {
        private const string BaseUrl = "api/Author";
        private readonly HttpClient httpClient = httpClient;

        public async Task<List<AuthorDTO>> GetAuthors()
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthors");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. JsonHelper.DeserializeJsonString<AuthorDTO[]>(apiResponse)];
        }

        public async Task<AuthorDTO> GetAuthor(int id)
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthor/{id}");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<AuthorDTO>(apiResponse);
        }

        public async Task<int> AddAuthor(AuthorDTO author)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"{BaseUrl}/AddAuthor",
                JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateAuthor(int id, AuthorDTO author)
        {
            using var request = CreateRequest(
                HttpMethod.Put,
                $"{BaseUrl}/UpdateAuthor/{id}",
                JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<string> DeleteAuthor(int id)
        {
            using var request = CreateRequest(HttpMethod.Delete, $"{BaseUrl}/DeleteAuthor/{id}");
            using var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var error = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(error)
                ? response.ReasonPhrase ?? "Error deleting author."
                : error;
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
}