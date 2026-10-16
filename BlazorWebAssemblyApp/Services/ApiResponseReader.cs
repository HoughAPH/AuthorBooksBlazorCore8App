using System.Text.Json;
using GeneralResponse = SharedClassLibrary.Models.ServiceResponses.GeneralResponse;

namespace BlazorWebAssemblyApp.Services;

/// <summary>
/// Adapts the API's HTTP contract to the <see cref="GeneralResponse"/> the pages consume.
/// Success: 200 with a GeneralResponse body. Failure: 4xx/5xx with problem details
/// (<c>detail</c>, or a validation <c>errors</c> map for model-state failures).
/// </summary>
internal static class ApiResponseReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<GeneralResponse> ToGeneralResponse(
        HttpResponseMessage response, string defaultSuccess, string defaultFailure)
    {
        string body = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? ReadSuccess(body, defaultSuccess)
            : new GeneralResponse(false, ReadProblemMessage(response, body) ?? defaultFailure);
    }

    private static GeneralResponse ReadSuccess(string body, string defaultSuccess)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new(true, defaultSuccess);

        try
        {
            GeneralResponse? parsed = JsonSerializer.Deserialize<GeneralResponse>(body, JsonOptions);
            return parsed ?? new(true, defaultSuccess);
        }
        catch (JsonException)
        {
            return new(true, defaultSuccess);
        }
    }

    private static string? ReadProblemMessage(HttpResponseMessage response, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (root.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Object)
            {
                IEnumerable<string> messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray() : [])
                    .Select(m => m.GetString())
                    .OfType<string>();

                string joined = string.Join(" ", messages);
                if (joined.Length > 0)
                    return joined;
            }

            if (root.TryGetProperty("detail", out JsonElement detail) && detail.ValueKind == JsonValueKind.String)
                return detail.GetString();

            // Pre-contract-change bodies: { "flag": false, "message": "..." }.
            if (root.TryGetProperty("message", out JsonElement message) && message.ValueKind == JsonValueKind.String)
                return message.GetString();

            if (root.TryGetProperty("title", out JsonElement title) && title.ValueKind == JsonValueKind.String)
                return title.GetString();
        }
        catch (JsonException)
        {
            // Non-JSON error body (for example a gateway page): fall back to the default message.
        }

        return null;
    }
}
