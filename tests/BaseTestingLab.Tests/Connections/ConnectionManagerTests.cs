using BaseTestingLab.Core.Connections;
using FluentAssertions;
using Moq;
using Xunit;

namespace BaseTestingLab.Tests.Connections;

public class ConnectionManagerTests
{
    private readonly ConnectionManager _manager = new();

    [Fact]
    public async Task ExecuteWithFailoverAsync_WhenPrimaryFails_ShouldFallbackToSecondary()
    {
        var primaryMock = new Mock<IConnection>();
        primaryMock.Setup(c => c.Id).Returns("conn-primary");
        primaryMock.Setup(c => c.State).Returns(ConnectionState.Connected);

        var secondaryMock = new Mock<IConnection>();
        secondaryMock.Setup(c => c.Id).Returns("conn-secondary");
        secondaryMock.Setup(c => c.State).Returns(ConnectionState.Connected);

        _manager.Register(primaryMock.Object);
        _manager.Register(secondaryMock.Object);

        Func<IConnection, Task<string>> operation = conn =>
        {
            if (conn.Id == "conn-primary")
            {
                throw new InvalidOperationException("Servidor primario no disponible.");
            }
            return Task.FromResult("Resultado desde réplica secundaria");
        };

        var result = await _manager.ExecuteWithFailoverAsync(
            new[] { "conn-primary", "conn-secondary" },
            operation);

        result.Should().Be("Resultado desde réplica secundaria");
    }

    [Fact]
    public async Task ExecuteWithFailoverAsync_WhenAllConnectionsFail_ShouldThrowAggregateException()
    {
        var connMock1 = new Mock<IConnection>();
        connMock1.Setup(c => c.Id).Returns("c1");
        connMock1.Setup(c => c.State).Returns(ConnectionState.Connected);

        var connMock2 = new Mock<IConnection>();
        connMock2.Setup(c => c.Id).Returns("c2");
        connMock2.Setup(c => c.State).Returns(ConnectionState.Connected);

        _manager.Register(connMock1.Object);
        _manager.Register(connMock2.Object);

        Func<IConnection, Task<string>> alwaysFails = _ => throw new TimeoutException("Timeout general");

        Func<Task> act = async () => await _manager.ExecuteWithFailoverAsync(new[] { "c1", "c2" }, alwaysFails);

        var ex = await act.Should().ThrowAsync<AggregateException>();
        ex.Which.InnerExceptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldSendPayloadToAllConnectedInstances()
    {
        var active1 = new Mock<IConnection>();
        active1.Setup(c => c.Id).Returns("c-act-1");
        active1.Setup(c => c.State).Returns(ConnectionState.Connected);
        active1.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(SendResult.Ok("1"));

        var active2 = new Mock<IConnection>();
        active2.Setup(c => c.Id).Returns("c-act-2");
        active2.Setup(c => c.State).Returns(ConnectionState.Connected);
        active2.Setup(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(SendResult.Ok("2"));

        var disconnected = new Mock<IConnection>();
        disconnected.Setup(c => c.Id).Returns("c-disc");
        disconnected.Setup(c => c.State).Returns(ConnectionState.Disconnected);

        _manager.Register(active1.Object);
        _manager.Register(active2.Object);
        _manager.Register(disconnected.Object);

        var delivered = await _manager.BroadcastAsync("SYSTEM_SHUTDOWN_WARNING");

        delivered.Should().Be(2);
        active1.Verify(c => c.SendAsync("SYSTEM_SHUTDOWN_WARNING", It.IsAny<CancellationToken>()), Times.Once);
        active2.Verify(c => c.SendAsync("SYSTEM_SHUTDOWN_WARNING", It.IsAny<CancellationToken>()), Times.Once);
        disconnected.Verify(c => c.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ConnectionManager_RegistryOperations_ShouldManageConnectionsCorrectly()
    {
        var mock = new Mock<IConnection>();
        mock.Setup(c => c.Id).Returns("conn-test");

        _manager.Register(mock.Object);
        _manager.Count.Should().Be(1);
        _manager.GetConnection("conn-test").Should().BeSameAs(mock.Object);

        Action actMissing = () => _manager.GetConnection("non-existent");
        actMissing.Should().Throw<KeyNotFoundException>();

        var removed = _manager.Unregister("conn-test");
        removed.Should().BeTrue();
        _manager.Count.Should().Be(0);
    }
}
