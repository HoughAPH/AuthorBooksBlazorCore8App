using Microsoft.AspNetCore.Components.WebAssembly.Http;
using SharedClassLibrary.Contracts;
using SharedClassLibrary.DTOs;
using SharedClassLibrary.Helpers;
using System.Net;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace BlazorWebAssemblyApp.Services;

public class BookService(HttpClient httpClient) : IBooks
{
    private const string BaseUrl = "api/Book";
    private readonly HttpClient httpClient = httpClient;

    public async Task<IEnumerable<BookDTO>> GetBooks()
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetBooks");
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
    }

    public async Task<IEnumerable<BookDTO>> GetAuthorBooks(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetAuthorBooks/{id}");
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return [.. JsonHelper.DeserializeJsonString<BookDTO[]>(apiResponse)];
    }

    public async Task<BookDTO?> GetBook(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"{BaseUrl}/GetBook/{id}");
        using HttpResponseMessage response = await httpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        string apiResponse = await response.Content.ReadAsStringAsync();
        return JsonHelper.DeserializeJsonString<BookDTO>(apiResponse);
    }

    public async Task<GeneralResponse> AddBook(BookDTO book)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Post,
            $"{BaseUrl}/AddBook",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        return await ApiResponseReader.ToGeneralResponse(response, "Book added successfully.", "Book could not be added.");
    }

    public async Task<GeneralResponse> UpdateBook(int id, BookDTO book)
    {
        using HttpRequestMessage request = CreateRequest(
            HttpMethod.Put,
            $"{BaseUrl}/UpdateBook/{id}",
            JsonHelper.GenerateStringContent(JsonHelper.SerializeObj(book)));

        using HttpResponseMessage response = await httpClient.SendAsync(request);
        return await ApiResponseReader.ToGeneralResponse(response, "Book updated successfully.", "Book could not be updated.");
    }

    public async Task<GeneralResponse> DeleteBook(int id)
    {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, $"{BaseUrl}/DeleteBook/{id}");
        using HttpResponseMessage response = await httpClient.SendAsync(request);
        return await ApiResponseReader.ToGeneralResponse(response, "Book deleted successfully.", "Book could not be deleted.");
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
