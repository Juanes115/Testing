using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.Connections;

public class ConnectionManager
{
    private readonly Dictionary<string, IConnection> _connections = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _connections.Count;

    public void Register(IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connections[connection.Id] = connection;
    }

    public bool Unregister(string connectionId) => _connections.Remove(connectionId);

    public IConnection GetConnection(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var conn))
            return conn;

        throw new KeyNotFoundException($"No se encontró la conexión con identificador '{connectionId}'.");
    }

    public async Task<T> ExecuteWithFailoverAsync<T>(
        IEnumerable<string> connectionIds,
        Func<IConnection, Task<T>> operation)
    {
        var exceptions = new List<Exception>();

        foreach (var id in connectionIds)
        {
            if (!_connections.TryGetValue(id, out var connection))
            {
                continue;
            }

            try
            {
                if (connection.State != ConnectionState.Connected)
                {
                    await connection.ConnectAsync();
                }

                return await operation(connection);
            }
            catch (Exception ex)
            {
                exceptions.Add(new ConnectionException($"Fallo al ejecutar operación sobre '{id}': {ex.Message}", ex, id));
            }
        }

        throw new AggregateException("Todas las conexiones de failover especificadas fallaron.", exceptions);
    }

    public async Task<int> BroadcastAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("El mensaje de difusión no puede estar vacío.", nameof(payload));

        int successCount = 0;
        foreach (var conn in _connections.Values)
        {
            if (conn.State != ConnectionState.Connected)
                continue;

            var result = await conn.SendAsync(payload, cancellationToken);
            if (result.Success)
            {
                successCount++;
            }
        }

        return successCount;
    }
}
