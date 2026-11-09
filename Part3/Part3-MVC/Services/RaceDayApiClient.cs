using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Mvc.Services;

public sealed class RaceDayApiClient : IRaceDayApi
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken ct)
    {
        var session = _accessor.HttpContext?.Session;
        var cookie = session?.GetString("RaceDayApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        var response = await _http.SendAsync(request, ct);
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var first = values.Select(v => v.Split(';')[0]).FirstOrDefault(v => v.StartsWith(".RaceDay.Session="));
            if (first != null)
                session?.SetString("RaceDayApiCookie", first);
        }
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            session?.Remove("RaceDayApiCookie");
            session?.Remove("UserId");
            session?.Remove("Role");
            session?.Remove("FullName");
        }
        return response;
    }

    private async Task<ApiResult<T>> Read<T>(HttpResponseMessage r, CancellationToken ct)
    {
        if (r.StatusCode == HttpStatusCode.NoContent)
            return new(true, default, null, r.StatusCode);
        var raw = await r.Content.ReadAsStringAsync(ct);
        if (r.IsSuccessStatusCode)
        {
            try
            {
                return new(true, JsonSerializer.Deserialize<T>(raw, Json), null, r.StatusCode);
            }
            catch
            {
                return new(true, default, raw, r.StatusCode);
            }
        }
        string? msg = null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("message", out var m))
                msg = m.GetString();
            else if (doc.RootElement.TryGetProperty("title", out var t))
                msg = t.GetString();
            else if (doc.RootElement.TryGetProperty("errors", out var e))
                msg = string.Join(
                    " ", e.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()));
        }
        catch
        {
        }
        return new(false, default,
                   msg ?? (r.StatusCode == HttpStatusCode.ServiceUnavailable
                               ? "RaceDay services are temporarily unavailable."
                               : "The request could not be completed."),
                   r.StatusCode);
    }

    public async Task<ApiResult<T>> Get<T>(string path, CancellationToken ct = default)
    {
        try
        {
            return await Read<T>(await Send(new(HttpMethod.Get, path), ct), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, default, "RaceDay API is unavailable. Please try again shortly.",
                       HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<ApiResult<TOut>> Post<TIn, TOut>(string path, TIn body, CancellationToken ct = default) =>
        await JsonSend<TIn, TOut>(HttpMethod.Post, path, body, ct);
    public async Task<ApiResult<TOut>> Put<TIn, TOut>(string path, TIn body, CancellationToken ct = default) =>
        await JsonSend<TIn, TOut>(HttpMethod.Put, path, body, ct);
    private async Task<ApiResult<TOut>> JsonSend<TIn, TOut>(HttpMethod method, string path, TIn body,
                                                            CancellationToken ct)
    {
        try
        {
            var req =
                new HttpRequestMessage(method, path) { Content = new StringContent(JsonSerializer.Serialize(body, Json),
                                                                                   Encoding.UTF8, "application/json") };
            return await Read<TOut>(await Send(req, ct), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, default, "RaceDay API is unavailable. Please try again shortly.",
                       HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<ApiResult<bool>> Delete(string path, CancellationToken ct = default)
    {
        try
        {
            var r = await Send(new(HttpMethod.Delete, path), ct);
            return new(r.IsSuccessStatusCode, true, r.IsSuccessStatusCode ? null : "Unable to delete this item.",
                       r.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, false, "RaceDay API is unavailable.", HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<ApiFileResult> GetFile(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await Send(new HttpRequestMessage(HttpMethod.Get, path), ct);
            if (!response.IsSuccessStatusCode)
                return new(false, null, null, "Image is not available.", response.StatusCode);
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return new(true, bytes, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream", null,
                       response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, null, null, "RaceDay API is unavailable.", HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<ApiResult<TOut>> Upload<TOut>(string path, IFormFile file, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var part = new StreamContent(stream);
            if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
                part.Headers.ContentType = contentType;
            content.Add(part, "file", Path.GetFileName(file.FileName));
            var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
            return await Read<TOut>(await Send(req, ct), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(false, default, "RaceDay API is unavailable.", HttpStatusCode.ServiceUnavailable);
        }
    }
}
