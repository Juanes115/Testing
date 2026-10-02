using BaseTestingLab.Core.Common.Exceptions;
using BaseTestingLab.Core.Connections;
using FluentAssertions;
using Moq;
using Xunit;

namespace BaseTestingLab.Tests.Connections;

public class ConnectionBridgeTests
{
    private readonly Mock<IConnection> _sourceMock;
    private readonly Mock<IConnection> _destMock;
    private readonly ConnectionBridge _sut;

    public ConnectionBridgeTests()
    {
        _sourceMock = new Mock<IConnection>();
        _destMock = new Mock<IConnection>();

        _sourceMock.Setup(s => s.Id).Returns("conn-source-01");
        _sourceMock.Setup(s => s.State).Returns(ConnectionState.Connected);

        _destMock.Setup(d => d.Id).Returns("conn-dest-02");
        _destMock.Setup(d => d.State).Returns(ConnectionState.Connected);

        _sut = new ConnectionBridge();
    }

    [Fact]
    public async Task TransferAsync_WhenBothConnected_ShouldTransferPayloadSuccessfully()
    {
        var payload = "{\"event\":\"order_created\",\"amount\":250.00}";
        _destMock
            .Setup(d => d.SendAsync(payload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok("msg-101"));

        var result = await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, payload);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SourceId.Should().Be("conn-source-01");
        result.DestinationId.Should().Be("conn-dest-02");
        result.TransferredMessageId.Should().Be("msg-101");
        result.RetriesUsed.Should().Be(0);

        _destMock.Verify(d => d.SendAsync(payload, It.IsAny<CancellationToken>()), Times.Once);
        _destMock.Verify(d => d.ConnectAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_WhenSourceDisconnected_WithAutoReconnect_ShouldConnectBeforeTransfer()
    {
        _sourceMock.Setup(s => s.State).Returns(ConnectionState.Disconnected);
        _sourceMock
            .Setup(s => s.ConnectAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _sourceMock.Setup(s => s.State).Returns(ConnectionState.Connected))
            .Returns(Task.CompletedTask);

        _destMock
            .Setup(d => d.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok("msg-auto-recon"));

        var options = new ConnectionTransferOptions { AutoReconnect = true };

        var result = await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "payload", options);

        result.Success.Should().BeTrue();
        _sourceMock.Verify(s => s.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenFirstAttemptFails_ShouldRetryAndSucceedOnSecondAttempt()
    {
        var payload = "data-to-retry";
        var options = new ConnectionTransferOptions
        {
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(5)
        };

        _destMock
            .SetupSequence(d => d.SendAsync(payload, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Fail("Error de socket transitorio"))
            .ReturnsAsync(SendResult.Ok("msg-recovered-02"));

        var result = await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, payload, options);

        result.Success.Should().BeTrue();
        result.RetriesUsed.Should().Be(1);
        result.TransferredMessageId.Should().Be("msg-recovered-02");

        _destMock.Verify(d => d.SendAsync(payload, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TransferAsync_WhenAllRetriesExhausted_ShouldThrowConnectionException()
    {
        var options = new ConnectionTransferOptions
        {
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(5)
        };

        _destMock
            .Setup(d => d.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Fail("Timeout de red permanente"));

        Func<Task> act = async () => await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "data", options);

        await act.Should().ThrowAsync<ConnectionException>()
                 .WithMessage("*Fallo en la transferencia tras 3 intento(s)*")
                 .Where(ex => ex.ConnectionId == "conn-dest-02");

        _destMock.Verify(d => d.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task TransferAsync_WhenSourceAndDestinationAreSame_ShouldThrowInvalidOperationException()
    {
        _destMock.Setup(d => d.Id).Returns("conn-source-01");

        Func<Task> act = async () => await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "data");

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*no pueden ser la misma instancia*");
    }

    [Fact]
    public async Task TransferAsync_WithPayloadTransformer_ShouldSendTransformedPayload()
    {
        var options = new ConnectionTransferOptions
        {
            PayloadTransformer = raw => $"[ENCRYPTED]:{raw.ToUpperInvariant()}"
        };

        _destMock
            .Setup(d => d.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok("msg-transformed"));

        await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "hello world", options);

        _destMock.Verify(d => d.SendAsync("[ENCRYPTED]:HELLO WORLD", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncQueueAsync_ShouldDrainMessagesUntilNullAndForwardEachToDestination()
    {
        _sourceMock
            .SetupSequence(s => s.ReceiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("item-1")
            .ReturnsAsync("item-2")
            .ReturnsAsync("item-3")
            .ReturnsAsync((string?)null);

        _destMock
            .Setup(d => d.SendAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok("ok"));

        var count = await _sut.SyncQueueAsync(_sourceMock.Object, _destMock.Object, maxItems: 10);

        count.Should().Be(3);
        _destMock.Verify(d => d.SendAsync("item-1", It.IsAny<CancellationToken>()), Times.Once);
        _destMock.Verify(d => d.SendAsync("item-2", It.IsAny<CancellationToken>()), Times.Once);
        _destMock.Verify(d => d.SendAsync("item-3", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WhenValidatePingBeforeSendAndPingFails_ShouldThrowConnectionException()
    {
        var options = new ConnectionTransferOptions { ValidatePingBeforeSend = true };
        _destMock.Setup(d => d.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Func<Task> act = async () => await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "data", options);

        await act.Should().ThrowAsync<ConnectionException>()
                 .WithMessage("*no respondió al ping*");
    }

    [Fact]
    public async Task TransferAsync_WhenDisconnectedAndAutoReconnectFalse_ShouldThrowConnectionException()
    {
        _sourceMock.Setup(s => s.State).Returns(ConnectionState.Disconnected);
        var options = new ConnectionTransferOptions { AutoReconnect = false };

        Func<Task> act = async () => await _sut.TransferAsync(_sourceMock.Object, _destMock.Object, "data", options);

        await act.Should().ThrowAsync<ConnectionException>()
                 .WithMessage("*no está activa*");
    }
}
