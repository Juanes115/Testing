using System.Security.Cryptography;
using System.Text;

namespace BaseTestingLab.Core.Security;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IAuditLogger _auditLogger;
    private readonly ICacheService _cacheService;
    private const int MaxFailedAttempts = 3;

    public AuthenticationService(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IAuditLogger auditLogger,
        ICacheService cacheService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string clientIp)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Fail("Las credenciales son obligatorias.");
        }

        var isIpBlocked = await _cacheService.GetAsync<bool>($"blacklist_ip_{clientIp}");
        if (isIpBlocked)
        {
            await _auditLogger.LogAsync("LOGIN_BLOCKED_IP", email, false, $"Intento desde IP bloqueada: {clientIp}");
            return AuthResult.Fail("Dirección IP bloqueada por múltiples intentos sospechosos.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            await _auditLogger.LogAsync("LOGIN_FAILED", email, false, "Usuario inexistente.");
            return AuthResult.Fail("Credenciales inválidas.");
        }

        if (user.IsLocked)
        {
            await _auditLogger.LogAsync("LOGIN_LOCKED", user.Id, false, "Cuenta bloqueada.");
            return AuthResult.Fail("La cuenta se encuentra bloqueada por exceder los intentos.", accountLocked: true);
        }

        if (!user.IsActive)
        {
            await _auditLogger.LogAsync("LOGIN_INACTIVE", user.Id, false, "Cuenta desactivada.");
            return AuthResult.Fail("La cuenta de usuario está desactivada.");
        }

        var passwordMatches = VerifyHash(password, user.PasswordHash);
        if (!passwordMatches)
        {
            int newAttempts = user.FailedAttempts + 1;
            bool shouldLock = newAttempts >= MaxFailedAttempts;

            var updatedUser = user with
            {
                FailedAttempts = newAttempts,
                IsLocked = shouldLock
            };

            await _userRepository.UpdateUserAsync(updatedUser);

            if (shouldLock)
            {
                await _auditLogger.LogAsync("ACCOUNT_LOCKED", user.Id, false, $"Bloqueado por {newAttempts} intentos erróneos.");
                return AuthResult.Fail("Cuenta bloqueada por exceso de intentos fallidos.", accountLocked: true);
            }

            await _auditLogger.LogAsync("LOGIN_FAILED", user.Id, false, $"Contraseña incorrecta. Intento {newAttempts}/{MaxFailedAttempts}");
            return AuthResult.Fail("Credenciales inválidas.");
        }

        if (user.FailedAttempts > 0)
        {
            var resetUser = user with { FailedAttempts = 0 };
            await _userRepository.UpdateUserAsync(resetUser);
        }

        var token = _tokenGenerator.GenerateToken(user, TimeSpan.FromHours(2));
        await _auditLogger.LogAsync("LOGIN_SUCCESS", user.Id, true, $"Autenticación correcta desde IP {clientIp}");

        return AuthResult.Success(token);
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static bool VerifyHash(string password, string storedHash)
    {
        var computed = HashPassword(password);
        return string.Equals(computed, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
