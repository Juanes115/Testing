namespace BaseTestingLab.Core.Connections;

public enum ConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Disconnecting = 3,
    Faulted = 4,
    Closed = 5
}
