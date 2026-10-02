namespace BaseTestingLab.Core.Common.Exceptions;

public class ConnectionException : Exception
{
    public string? ConnectionId { get; }

    public ConnectionException(string message) : base(message)
    {
    }

    public ConnectionException(string message, string? connectionId) : base(message)
    {
        ConnectionId = connectionId;
    }

    public ConnectionException(string message, Exception innerException, string? connectionId = null) 
        : base(message, innerException)
    {
        ConnectionId = connectionId;
    }
}
