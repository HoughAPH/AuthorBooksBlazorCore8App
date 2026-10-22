using System.Net;
using System.Text.Json;
using SharedClassLibrary.Models;

namespace BlazorWebAssemblyApp.Services;

/// <summary>
/// Turns the API's HTTP contract into the shared <see cref="Result"/> types the pages consume.
/// Success: 200 with a <see cref="Result"/> / <see cref="Result{T}"/> body. Failure: 4xx/5xx with problem details
/// (<c>detail</c>, or a validation <c>errors</c> map for model-state failures); the HTTP status decides the
/// <see cref="ResultStatus"/>.
/// </summary>
internal static class ResultReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<Result> ToResult(HttpResponseMessage response, string defaultSuccess, string defaultFailure)
    {
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Failure(response, body, defaultFailure);

        Result? parsed = TryDeserialize<Result>(body);
        return Result.Ok(string.IsNullOrWhiteSpace(parsed?.Message) ? defaultSuccess : parsed.Message);
    }

    public static async Task<Result<T>> ToResult<T>(HttpResponseMessage response, string defaultSuccess, string defaultFailure)
    {
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Result failure = Failure(response, body, defaultFailure);
            return new Result<T>(failure.Status, failure.Message);
        }

        Result<T>? parsed = TryDeserialize<Result<T>>(body);

        if (parsed is null || parsed.Value is null)
            return Result<T>.Invalid(defaultFailure);

        return Result<T>.Ok(parsed.Value, string.IsNullOrWhiteSpace(parsed.Message) ? defaultSuccess : parsed.Message);
    }

    private static Result Failure(HttpResponseMessage response, string body, string defaultFailure)
    {
        ResultStatus status = response.StatusCode switch
        {
            HttpStatusCode.NotFound => ResultStatus.NotFound,
            HttpStatusCode.Conflict => ResultStatus.Conflict,
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ResultStatus.Unauthorized,
            _ => ResultStatus.Invalid
        };

        return new Result(status, ReadProblemMessage(body) ?? defaultFailure);
    }

    private static T? TryDeserialize<T>(string body) where T : class
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadProblemMessage(string body)
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
