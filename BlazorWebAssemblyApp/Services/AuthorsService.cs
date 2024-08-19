using Blazored.LocalStorage;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.GenericModels;
//using LanguageExt.Common;


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
            string token = await localStorageService.GetItemAsStringAsync("token");
            var claims = Generics.GetClaimsFromToken(token);
            string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthors");
            if (!response.IsSuccessStatusCode)
            {
                var test = await response.Content.ReadAsStringAsync();
                return null;// Task.FromResult<>(test).Result;
            }


            var apiResponse = await response.Content.ReadAsStringAsync();
            return [.. Generics.DeserializeJsonString<AuthorDTO[]>(apiResponse)];
        }
        public async Task<AuthorDTO> GetAuthor(int id)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            //var claims = Generics.GetClaimsFromToken(token);
            //string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync($"{BaseUrl}/GetAuthor/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return await Task.FromResult<AuthorDTO>(null);
            }
            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<AuthorDTO>(apiResponse);
        }

        public async Task<int> AddAuthor(AuthorDTO author)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            //var claims = Generics.GetClaimsFromToken(token);
            //string role = claims.Role;
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<AuthorDTO>(author));
            var response = await httpClient.PostAsync($"{BaseUrl}/AddAuthor", content);
            if (!response.IsSuccessStatusCode)
            {
                //return Error.Unauthorized(response.StatusCode.ToString(), response.ReasonPhrase);

                //response.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                //response.Content = new StringContent("Unauthorized");

                return await Task.FromResult<int>(0);
            }
            var apiResponse = await response.Content.ReadAsStringAsync();

            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<int> UpdateAuthor(int id, AuthorDTO author)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            StringContent content = Generics.GenerateStringContent(Generics.SerializeObj<AuthorDTO>(author));
            var response = await httpClient.PutAsync($"{BaseUrl}/UpdateAuthor/{id}", content);

            if (!response.IsSuccessStatusCode)
                //return Error.Custom(0, response.StatusCode.ToString(), response.ReasonPhrase);
                return 0;

            var apiResponse = await response.Content.ReadAsStringAsync();
            return Generics.DeserializeJsonString<int>(apiResponse);
        }

        public async Task<string> DeleteAuthor(int id)
        {
            string token = await localStorageService.GetItemAsStringAsync("token");

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            try
            {
                var response = await httpClient.DeleteAsync($"{BaseUrl}/DeleteAuthor/{id}");

                if (!response.IsSuccessStatusCode)
                    return response.ReasonPhrase; //Error.Custom(0, response.StatusCode.ToString(),  response.ReasonPhrase);  //"There was an error";//response.ReasonPhrase;
                else
                {
                    var apiResponse = await response.Content.ReadAsStringAsync();
                    //string result = Generics.DeserializeJsonString<string>(apiResponse);
                    //return Generics.DeserializeJsonString<string>(apiResponse);
                    return apiResponse;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
    }
}