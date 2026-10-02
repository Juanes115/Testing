using System.Text.RegularExpressions;

namespace BaseTestingLab.Core.Text;

public class RegexValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UrlRegex = new(
        @"^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneE164Regex = new(
        @"^\+[1-9]\d{1,14}$",
        RegexOptions.Compiled);

    private static readonly Regex GuidRegex = new(
        @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.Compiled);

    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled);

    public bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

    public bool IsValidUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && UrlRegex.IsMatch(url);

    public bool IsValidE164Phone(string? phone) =>
        !string.IsNullOrWhiteSpace(phone) && PhoneE164Regex.IsMatch(phone);

    public bool IsValidGuid(string? guidString) =>
        !string.IsNullOrWhiteSpace(guidString) && GuidRegex.IsMatch(guidString);

    public bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && SlugRegex.IsMatch(slug);

    public (bool IsValid, List<string> Errors) ValidatePasswordStrength(string? password, int minLength = 8)
    {
        var errors = new List<string>();
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("La contraseña no puede estar vacía.");
            return (false, errors);
        }

        if (password.Length < minLength)
            errors.Add($"Debe tener al menos {minLength} caracteres.");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("Debe contener al menos una letra mayúscula.");

        if (!Regex.IsMatch(password, @"[a-z]"))
            errors.Add("Debe contener al menos una letra minúscula.");

        if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("Debe contener al menos un número.");

        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            errors.Add("Debe contener al menos un carácter especial.");

        return (errors.Count == 0, errors);
    }

    public Dictionary<string, string> ExtractNamedGroups(string input, string pattern)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
            return result;

        var regex = new Regex(pattern);
        var match = regex.Match(input);

        if (match.Success)
        {
            foreach (var groupName in regex.GetGroupNames())
            {
                if (groupName != "0" && match.Groups[groupName].Success)
                {
                    result[groupName] = match.Groups[groupName].Value;
                }
            }
        }

        return result;
    }
}
