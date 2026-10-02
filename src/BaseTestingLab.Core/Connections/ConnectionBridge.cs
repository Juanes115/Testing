using System.Diagnostics;
using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.Connections;

public class ConnectionBridge
{
    public async Task<TransferResult> TransferAsync(
        IConnection source,
        IConnection destination,
        string payload,
        ConnectionTransferOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (string.Equals(source.Id, destination.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La conexión de origen y destino no pueden ser la misma instancia.");
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("El contenido de la carga útil (payload) no puede ser nulo o vacío.", nameof(payload));
        }

        options ??= new ConnectionTransferOptions();
        var stopwatch = Stopwatch.StartNew();

        await EnsureConnectedAsync(source, options.AutoReconnect, cancellationToken);
        await EnsureConnectedAsync(destination, options.AutoReconnect, cancellationToken);

        if (options.ValidatePingBeforeSend)
        {
            var pingDest = await destination.PingAsync(cancellationToken);
            if (!pingDest)
            {
                throw new ConnectionException($"El destino '{destination.Id}' no respondió al ping de validación pre-envío.", destination.Id);
            }
        }

        var finalPayload = options.PayloadTransformer != null
            ? options.PayloadTransformer(payload)
            : payload;

        int retriesUsed = 0;
        string? lastError = null;

        while (retriesUsed <= options.MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sendResult = await destination.SendAsync(finalPayload, cancellationToken);
            if (sendResult.Success)
            {
                stopwatch.Stop();
                return new TransferResult
                {
                    Success = true,
                    SourceId = source.Id,
                    DestinationId = destination.Id,
                    TransferredMessageId = sendResult.MessageId,
                    RetriesUsed = retriesUsed,
                    Elapsed = stopwatch.Elapsed
                };
            }

            lastError = sendResult.ErrorMessage ?? "Error no especificado al enviar al destino.";
            retriesUsed++;

            if (retriesUsed <= options.MaxRetries)
            {
                await Task.Delay(options.RetryDelay, cancellationToken);
            }
        }

        stopwatch.Stop();
        throw new ConnectionException(
            $"Fallo en la transferencia tras {retriesUsed} intento(s). Último error: {lastError}",
            destination.Id);
    }

    public async Task<int> SyncQueueAsync(
        IConnection source,
        IConnection destination,
        int maxItems = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        await EnsureConnectedAsync(source, autoReconnect: true, cancellationToken);
        await EnsureConnectedAsync(destination, autoReconnect: true, cancellationToken);

        int processed = 0;
        while (processed < maxItems)
        {
            var message = await source.ReceiveAsync(cancellationToken);
            if (message == null)
            {
                break;
            }

            var result = await destination.SendAsync(message, cancellationToken);
            if (!result.Success)
            {
                throw new ConnectionException($"Error al sincronizar mensaje al destino: {result.ErrorMessage}", destination.Id);
            }

            processed++;
        }

        return processed;
    }

    private static async Task EnsureConnectedAsync(IConnection conn, bool autoReconnect, CancellationToken cancellationToken)
    {
        if (conn.State == ConnectionState.Connected)
            return;

        if (autoReconnect)
        {
            await conn.ConnectAsync(cancellationToken);
            if (conn.State != ConnectionState.Connected)
            {
                throw new ConnectionException($"No se pudo establecer la conexión '{conn.Id}'. Estado actual: {conn.State}", conn.Id);
            }
        }
        else
        {
            throw new ConnectionException($"La conexión '{conn.Id}' no está activa (Estado: {conn.State}).", conn.Id);
        }
    }
}
