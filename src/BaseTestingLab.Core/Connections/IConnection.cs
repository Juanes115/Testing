namespace BaseTestingLab.Core.Connections;

public interface IConnection
{
    string Id { get; }
    string Endpoint { get; }
    ConnectionState State { get; }
    int LatencyMs { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<SendResult> SendAsync(string payload, CancellationToken cancellationToken = default);
    Task<string?> ReceiveAsync(CancellationToken cancellationToken = default);
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
}
