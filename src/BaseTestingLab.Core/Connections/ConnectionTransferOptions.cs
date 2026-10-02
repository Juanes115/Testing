namespace BaseTestingLab.Core.Connections;

public class ConnectionTransferOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);
    public Func<string, string>? PayloadTransformer { get; set; }
    public bool AutoReconnect { get; set; } = true;
    public bool ValidatePingBeforeSend { get; set; } = false;
}

public record TransferResult
{
    public bool Success { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public string DestinationId { get; init; } = string.Empty;
    public string? TransferredMessageId { get; init; }
    public int RetriesUsed { get; init; }
    public TimeSpan Elapsed { get; init; }
    public string? ErrorMessage { get; init; }
}
