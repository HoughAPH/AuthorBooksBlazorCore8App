using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
using System.Net.Http.Headers;

namespace BlazorWebAssemblyApp.Services
{
    public class BookService : IBooks
    {
        public BookService(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            this.httpClient = httpClient;
            this.localStorageService = localStorageService;

        }

        private const string BaseUrl = "api/Book";
        private readonly HttpClient httpClient;
        private readonly ILocalStorageService localStorageService;

        public async Task<IEnumerable<BookDTO>> GetBooks()
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/GetBooks");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonString<BookDTO[]>(apiResponse)];
        }

        public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthorBooks/{id}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonString<BookDTO[]>(apiResponse)];
        }

        public async Task<BookDTO> GetBook(int id)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.GetAsync($"{BaseUrl}/GetBook/{id}");
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<BookDTO>(apiResponse);
        }

        public async Task<int> AddBook(BookDTO book)
        {
            await SetAuthorizationHeaderAsync();

            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<BookDTO>(book));
            var response = await httpClient.PostAsync($"{BaseUrl}/AddBook", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateBook(int id, BookDTO book)
        {
            await SetAuthorizationHeaderAsync();

            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<BookDTO>(book));
            var response = await httpClient.PutAsync($"{BaseUrl}/UpdateBook/{id}", content);
            response.EnsureSuccessStatusCode();

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<bool> DeleteBook(int id)
        {
            await SetAuthorizationHeaderAsync();

            var response = await httpClient.DeleteAsync($"{BaseUrl}/DeleteBook/{id}");
            return response.IsSuccessStatusCode;
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
