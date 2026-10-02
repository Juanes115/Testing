using BaseTestingLab.Core.Security;
using FluentAssertions;
using Moq;
using Xunit;

namespace BaseTestingLab.Tests.Security;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ITokenGenerator> _tokenGenMock = new();
    private readonly Mock<IAuditLogger> _auditLogMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _sut = new AuthenticationService(
            _userRepoMock.Object,
            _tokenGenMock.Object,
            _auditLogMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsValid_ShouldReturnSuccessWithToken()
    {
        var rawPassword = "MySecurePassword123!";
        var user = new User
        {
            Id = "usr-01",
            Email = "sofia@acme.com",
            PasswordHash = AuthenticationService.HashPassword(rawPassword),
            IsActive = true,
            FailedAttempts = 1
        };

        _cacheMock
            .Setup(c => c.GetAsync<bool>("blacklist_ip_192.168.1.50"))
            .ReturnsAsync(false);

        _userRepoMock
            .Setup(r => r.GetByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _tokenGenMock
            .Setup(t => t.GenerateToken(It.Is<User>(u => u.Id == "usr-01"), It.IsAny<TimeSpan>()))
            .Returns("jwt.mocked.token.xyz");

        var result = await _sut.LoginAsync(user.Email, rawPassword, "192.168.1.50");

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("jwt.mocked.token.xyz");

        _userRepoMock.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.FailedAttempts == 0)), Times.Once);
        _auditLogMock.Verify(a => a.LogAsync("LOGIN_SUCCESS", "usr-01", true, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenIpIsBlacklistedInCache_ShouldBlockImmediatelyWithoutCheckingUser()
    {
        var clientIp = "10.0.0.99";
        _cacheMock
            .Setup(c => c.GetAsync<bool>($"blacklist_ip_{clientIp}"))
            .ReturnsAsync(true);

        var result = await _sut.LoginAsync("alguien@test.com", "cualquiera", clientIp);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("IP bloqueada");

        _userRepoMock.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        _tokenGenMock.Verify(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<TimeSpan>()), Times.Never);
        _auditLogMock.Verify(a => a.LogAsync("LOGIN_BLOCKED_IP", "alguien@test.com", false, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIncorrect_ShouldIncrementFailedAttempts()
    {
        var user = new User
        {
            Id = "usr-02",
            Email = "diego@acme.com",
            PasswordHash = AuthenticationService.HashPassword("RealPassword123"),
            FailedAttempts = 1
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(user.Email, "WrongPassword", "192.168.1.1");

        result.Succeeded.Should().BeFalse();
        result.AccountLocked.Should().BeFalse();
        result.ErrorMessage.Should().Be("Credenciales inválidas.");

        _userRepoMock.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.FailedAttempts == 2 && !u.IsLocked)), Times.Once);
        _tokenGenMock.Verify(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenReachingMaxFailedAttempts_ShouldLockAccount()
    {
        var user = new User
        {
            Id = "usr-03",
            Email = "lucas@acme.com",
            PasswordHash = AuthenticationService.HashPassword("RealPassword123"),
            FailedAttempts = 2
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(user.Email, "AnotherWrongPassword", "192.168.1.1");

        result.Succeeded.Should().BeFalse();
        result.AccountLocked.Should().BeTrue();
        result.ErrorMessage.Should().Contain("bloqueada por exceso");

        _userRepoMock.Verify(r => r.UpdateUserAsync(It.Is<User>(u => u.FailedAttempts == 3 && u.IsLocked)), Times.Once);
        _auditLogMock.Verify(a => a.LogAsync("ACCOUNT_LOCKED", "usr-03", false, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountAlreadyLocked_ShouldRefuseLoginWithoutVerifyingPassword()
    {
        var user = new User
        {
            Id = "usr-04",
            Email = "bloqueado@acme.com",
            PasswordHash = AuthenticationService.HashPassword("Password123"),
            IsLocked = true
        };

        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(user.Email, "Password123", "192.168.1.1");

        result.Succeeded.Should().BeFalse();
        result.AccountLocked.Should().BeTrue();
        result.ErrorMessage.Should().Contain("se encuentra bloqueada");

        _tokenGenMock.Verify(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<TimeSpan>()), Times.Never);
        _userRepoMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserInactive_ShouldReturnFail()
    {
        var user = new User
        {
            Id = "usr-inactive",
            Email = "inactivo@test.com",
            PasswordHash = AuthenticationService.HashPassword("Pass123"),
            IsActive = false
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(user.Email, "Pass123", "127.0.0.1");

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("desactivada");
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("email@test.com", "")]
    [InlineData(null, "password")]
    public async Task LoginAsync_WhenCredentialsEmpty_ShouldFailImmediately(string? email, string? pass)
    {
        var result = await _sut.LoginAsync(email!, pass!, "127.0.0.1");
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("obligatorias");
    }
}
