using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class ApiResponse<T>
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public ApiResponse() { }

    public ApiResponse(int status, T? data, string message)
    {
        Status = status;
        Data = data;
        Message = message;
    }

    public static ApiResponse<T> Success(T data, string message = "操作成功")
    {
        return new ApiResponse<T>(200, data, message);
    }

    public static ApiResponse<T> Fail(string message, int status = 400)
    {
        return new ApiResponse<T>(status, default, message);
    }
}
