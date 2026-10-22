using System.Net;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;

namespace BlazorWebAssemblyApp.Services;

public class AuthorsService(HttpClient httpClient) : IAuthors
{
    private const string _baseUrl = "api/Author";
    private readonly HttpClient _httpClient = httpClient;

    public async Task<List<AuthorDTO>> GetAuthors()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/GetAuthors");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<AuthorDTO[]>(apiResponse)];
    }

    public async Task<PagedResult<AuthorDTO>> GetAuthorsPage(int pageNumber, int pageSize, string? search = null)
    {
        string uri = $"{_baseUrl}/GetAuthorsPage?pageNumber={pageNumber}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}";
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<PagedResult<AuthorDTO>>(apiResponse);
    }

    public async Task<AuthorDTO?> GetAuthor(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/GetAuthor/{id}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<AuthorDTO>(apiResponse);
    }

    public async Task<Result> AddAuthor(AuthorDTO author)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{_baseUrl}/AddAuthor",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Author added successfully.", "Author could not be added.");
    }

    public async Task<Result> UpdateAuthor(int id, AuthorDTO author)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Put,
            $"{_baseUrl}/UpdateAuthor/{id}",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(author)));

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Author updated successfully.", "Author could not be updated.");
    }

    public async Task<Result> DeleteAuthor(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, $"{_baseUrl}/DeleteAuthor/{id}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Author deleted successfully.", "Author could not be deleted.");
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
