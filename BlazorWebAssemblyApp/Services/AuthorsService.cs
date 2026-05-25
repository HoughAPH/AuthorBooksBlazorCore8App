using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace BlazorWebAssemblyApp.Services;

public class AuthorsService(HttpClient httpClient) : IAuthors
{
    private const string BaseUrl = "api/Author";
    private readonly HttpClient httpClient = httpClient;

    public async Task<List<AuthorDTO>> GetAuthors()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthors");
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<AuthorDTO[]>(apiResponse)];
    }

    public async Task<AuthorDTO?> GetAuthor(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthor/{id}");
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<AuthorDTO>(apiResponse);
    }

    public async Task<int> AddAuthor(AuthorDTO author)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/AddAuthor",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<int>(apiResponse);
    }

    public async Task<int> UpdateAuthor(int id, AuthorDTO author)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Put,
            $"{BaseUrl}/UpdateAuthor/{id}",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return 0;

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<int>(apiResponse);
    }

    public async Task<GeneralResponse> DeleteAuthor(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, $"{BaseUrl}/DeleteAuthor/{id}");
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        string apiResponse = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(apiResponse))
        {
            return JsonHelper.DeserializeJsonString<GeneralResponse>(apiResponse);
        }

        return response.IsSuccessStatusCode
            ? new(true, "Author deleted successfully.")
            : new(false, response.ReasonPhrase ?? "Error deleting author.");
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string uri, HttpContent? content = null)
    {
        HttpRequestMessage request = new(method, uri);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        if (content is not null)
            request.Content = content;

        return request;
    }
}