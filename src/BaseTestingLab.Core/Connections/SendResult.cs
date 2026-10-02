namespace BaseTestingLab.Core.Connections;

public record SendResult
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }

    public static SendResult Ok(string messageId, int statusCode = 200) => new()
    {
        Success = true,
        MessageId = messageId,
        StatusCode = statusCode
    };

    public static SendResult Fail(string errorMessage, int statusCode = 500) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        StatusCode = statusCode
    };
}
