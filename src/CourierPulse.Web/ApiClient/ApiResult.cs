namespace CourierPulse.Web.ApiClient;

public record ApiResult<T>(bool IsSuccess, int StatusCode, T? Data, string? Error)
{
    public static ApiResult<T> Ok(int statusCode, T data) => new(true, statusCode, data, null);

    public static ApiResult<T> Fail(int statusCode, string? error) => new(false, statusCode, default, error);
}
