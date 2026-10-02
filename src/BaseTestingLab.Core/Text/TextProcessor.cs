using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.Text;

/// <summary>
/// Procesador de operaciones sobre textos simples y complejos orientado a pruebas unitarias.
/// </summary>
public class TextProcessor
{
    public string SanitizeWhitespace(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Replace("\r\n", "\n").Replace("\r", "\n");
        return Regex.Replace(normalized.Trim(), @"[ \t]+", " ");
    }

    public string ToKebabCase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = SanitizeWhitespace(input);
        var step1 = Regex.Replace(sanitized, @"([A-Z]+)([A-Z][a-z])", "$1-$2");
        var step2 = Regex.Replace(step1, @"([a-z0-9])([A-Z])", "$1-$2");
        var step3 = Regex.Replace(step2, @"[\s_]+", "-");
        return step3.ToLowerInvariant().Trim('-');
    }

    public string ToSnakeCase(string? input)
    {
        var kebab = ToKebabCase(input);
        return kebab.Replace('-', '_');
    }

    public string ToTitleCase(string? input, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var textInfo = (culture ?? CultureInfo.InvariantCulture).TextInfo;
        return textInfo.ToTitleCase(input.ToLower(culture ?? CultureInfo.InvariantCulture));
    }

    public bool IsPalindrome(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var normalized = RemoveDiacritics(input).ToLowerInvariant();
        var alphanumericOnly = Regex.Replace(normalized, @"[^a-z0-9]", "");

        if (alphanumericOnly.Length <= 1)
            return alphanumericOnly.Length == 1;

        int left = 0;
        int right = alphanumericOnly.Length - 1;

        while (left < right)
        {
            if (alphanumericOnly[left] != alphanumericOnly[right])
                return false;

            left++;
            right--;
        }

        return true;
    }

    public string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("El correo electrónico no puede ser nulo ni vacío.", nameof(email));

        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            throw new ValidationException("Formato de correo electrónico inválido.", nameof(email));

        var user = parts[0];
        var domain = parts[1];

        if (user.Length <= 2)
        {
            return $"{user[0]}*@{domain}";
        }

        var firstChar = user[0];
        var lastChar = user[^1];
        var maskedSection = new string('*', Math.Min(3, user.Length - 2));

        return $"{firstChar}{maskedSection}{lastChar}@{domain}";
    }

    public string MaskCreditCard(string? cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ValidationException("El número de tarjeta no puede ser nulo o vacío.", nameof(cardNumber));

        var digitsOnly = Regex.Replace(cardNumber, @"\D", "");
        if (digitsOnly.Length is < 13 or > 19)
            throw new ValidationException("Longitud de tarjeta inválida (debe contener entre 13 y 19 dígitos).", nameof(cardNumber));

        var lastFour = digitsOnly[^4..];
        return $"****-****-****-{lastFour}";
    }

    public string Truncate(string? input, int maxLength, bool preserveWords = true, string ellipsis = "...")
    {
        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "La longitud máxima debe ser mayor a cero.");

        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (input.Length <= maxLength)
            return input;

        if (maxLength <= ellipsis.Length)
            return ellipsis[..maxLength];

        int targetLength = maxLength - ellipsis.Length;

        if (!preserveWords)
            return input[..targetLength] + ellipsis;

        var substring = input[..targetLength];
        var lastSpaceIndex = substring.LastIndexOf(' ');

        if (lastSpaceIndex > 0)
        {
            return substring[..lastSpaceIndex] + ellipsis;
        }

        return substring + ellipsis;
    }

    public string RenderTemplate(string template, IDictionary<string, object?> placeholders, bool strictMode = false)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        placeholders ??= new Dictionary<string, object?>();

        return Regex.Replace(template, @"\{(?<key>[a-zA-Z0-9_]+)\}", match =>
        {
            var key = match.Groups["key"].Value;
            if (placeholders.TryGetValue(key, out var val))
            {
                return val?.ToString() ?? string.Empty;
            }

            if (strictMode)
            {
                throw new KeyNotFoundException($"El marcador de posición '{{{key}}}' no fue proporcionado en los parámetros.");
            }

            return match.Value;
        });
    }

    public Dictionary<string, string> ParseKeyValuePairs(string? input, char delimiter = ';', char assignment = '=')
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(input))
            return result;

        var tokens = SplitRespectingQuotes(input, delimiter);
        foreach (var token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            var assignIndex = token.IndexOf(assignment);
            if (assignIndex <= 0) continue;

            var key = token[..assignIndex].Trim();
            var value = token[(assignIndex + 1)..].Trim();

            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
            {
                value = value[1..^1];
            }

            result[key] = value;
        }

        return result;
    }

    private static string[] SplitRespectingQuotes(string input, char delimiter)
    {
        var list = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                current.Append(ch);
            }
            else if (ch == delimiter && !inQuotes)
            {
                list.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            list.Add(current.ToString());
        }

        return list.ToArray();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
