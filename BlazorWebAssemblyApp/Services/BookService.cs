using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;

namespace BlazorWebAssemblyApp.Services
{
    public class BookService(HttpClient httpClient) : IBooks
    {
        private const string BaseUrl = "api/Book";
        private readonly HttpClient httpClient = httpClient;

        public async Task<IEnumerable<BookDTO>> GetBooks()
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetBooks");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
        }

        public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthorBooks/{id}");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
        }

        public async Task<BookDTO> GetBook(int id)
        {
            using var request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetBook/{id}");
            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<BookDTO>(apiResponse);
        }

        public async Task<int> AddBook(BookDTO book)
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"{BaseUrl}/AddBook",
                JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateBook(int id, BookDTO book)
        {
            using var request = CreateRequest(
                HttpMethod.Put,
                $"{BaseUrl}/UpdateBook/{id}",
                JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return JsonHelper.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<bool> DeleteBook(int id)
        {
            using var request = CreateRequest(HttpMethod.Delete, $"{BaseUrl}/DeleteBook/{id}");
            using var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
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
