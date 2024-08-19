using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;

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

            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync($"{BaseUrl}/GetBooks");
            if (!response.IsSuccessStatusCode)
                return null;
            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonString<BookDTO[]>(apiResponse)];

        }

        public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthorBooks/{id}");
            if (!response.IsSuccessStatusCode)
                return null;
            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonString<BookDTO[]>(apiResponse)];
        }

        public async Task<BookDTO> GetBook(int id)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync($"{BaseUrl}/GetBook/{id}");
            if (!response.IsSuccessStatusCode)
                return null;
            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<BookDTO>(apiResponse);
        }

        public async Task<int> AddBook(BookDTO book)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<BookDTO>(book));

            var response = await httpClient.PostAsync($"{BaseUrl}/AddBook", content);
            if (!response.IsSuccessStatusCode)
                return 0;

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateBook(int id, BookDTO book)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<BookDTO>(book));
            var response = await httpClient.PutAsync($"{BaseUrl}/UpdateBook/{id}", content);
            if (!response.IsSuccessStatusCode)
                return 0;
            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<bool> DeleteBook(int id)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.DeleteAsync($"{BaseUrl}/DeleteBook/{id}");
            if (!response.IsSuccessStatusCode)
                return false;
            return true;
        }
    }
}
