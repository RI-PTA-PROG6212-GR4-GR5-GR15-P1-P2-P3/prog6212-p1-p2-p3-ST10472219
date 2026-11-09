using Microsoft.AspNetCore.Http;
using System.Net;

namespace RaceDay.Mvc.Services;

public interface IRaceDayApi
{
    Task<ApiResult<T>> Get<T>(string path, CancellationToken ct = default);
    Task<ApiResult<TOut>> Post<TIn, TOut>(string path, TIn body, CancellationToken ct = default);
    Task<ApiResult<TOut>> Put<TIn, TOut>(string path, TIn body, CancellationToken ct = default);
    Task<ApiResult<bool>> Delete(string path, CancellationToken ct = default);
    Task<ApiFileResult> GetFile(string path, CancellationToken ct = default);
    Task<ApiResult<TOut>> Upload<TOut>(string path, IFormFile file, CancellationToken ct = default);
}
public record ApiResult<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode);
public record ApiFileResult(bool Success, byte[]? Content, string? ContentType, string? Error,
                            HttpStatusCode StatusCode);
