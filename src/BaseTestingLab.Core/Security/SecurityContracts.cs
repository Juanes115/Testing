namespace BaseTestingLab.Core.Security;

public record User
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Email { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public int FailedAttempts { get; init; } = 0;
    public bool IsLocked { get; init; } = false;
    public string Role { get; init; } = "User";
}

public record AuthResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AccountLocked { get; init; }

    public static AuthResult Success(string token) => new()
    {
        Succeeded = true,
        Token = token
    };

    public static AuthResult Fail(string error, bool accountLocked = false) => new()
    {
        Succeeded = false,
        ErrorMessage = error,
        AccountLocked = accountLocked
    };
}

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task UpdateUserAsync(User user);
}

public interface ITokenGenerator
{
    string GenerateToken(User user, TimeSpan lifetime);
}

public interface IAuditLogger
{
    Task LogAsync(string action, string targetId, bool success, string details);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
}
