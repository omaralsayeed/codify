namespace Codify.API.Common;

public class ApiResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public object? Details { get; set; }

    public static ApiResponse Ok(object? data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse Fail(string errorCode, string message, object? details = null) =>
        new() { Success = false, ErrorCode = errorCode, Message = message, Details = details };
}
