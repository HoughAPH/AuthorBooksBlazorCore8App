using System.Net;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using SharedClassLibrary.Models;

namespace BlazorWebAssemblyApp.Services;

public class BookService(HttpClient httpClient) : IBooks
{
    private const string _baseUrl = "api/Book";
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IEnumerable<BookDTO>> GetBooks()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/GetBooks");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
    }

    public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/GetAuthorBooks/{id}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
    }

    public async Task<PagedResult<BookDTO>> GetBooksPage(int pageNumber, int pageSize, string? search = null, int? authorId = null)
    {
        string uri = $"{_baseUrl}/GetBooksPage?pageNumber={pageNumber}&pageSize={pageSize}&search={Uri.EscapeDataString(search ?? string.Empty)}";
        if (authorId is not null)
            uri += $"&authorId={authorId}";

        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, uri);
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<PagedResult<BookDTO>>(apiResponse);
    }

    public async Task<BookDTO?> GetBook(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/GetBook/{id}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<BookDTO>(apiResponse);
    }

    public async Task<Result> AddBook(BookDTO book)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{_baseUrl}/AddBook",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Book added successfully.", "Book could not be added.");
    }

    public async Task<Result> UpdateBook(int id, BookDTO book)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Put,
            $"{_baseUrl}/UpdateBook/{id}",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Book updated successfully.", "Book could not be updated.");
    }

    public async Task<Result> DeleteBook(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, $"{_baseUrl}/DeleteBook/{id}");
        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        return await ResultReader.ToResult(response, "Book deleted successfully.", "Book could not be deleted.");
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
