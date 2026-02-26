namespace SelfCerts.Api.Infrastructure;

public class ApiResult<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static ApiResult<T> Success(T data, string message = "Success")
    {
        return new ApiResult<T> { Code = 200, Message = message, Data = data };
    }

    public static ApiResult<T> Error(int code, string message)
    {
        return new ApiResult<T> { Code = code, Message = message, Data = default };
    }
}

public class ApiResult : ApiResult<object>
{
    public static ApiResult Success(string message = "Success")
    {
        return new ApiResult { Code = 200, Message = message, Data = null };
    }
}