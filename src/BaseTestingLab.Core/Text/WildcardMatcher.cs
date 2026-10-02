using System.Text.RegularExpressions;

namespace BaseTestingLab.Core.Text;

/// <summary>
/// Evaluador de patrones con comodines (wildcards glob: '*', '?', '[a-z]').
/// </summary>
public class WildcardMatcher
{
    private readonly bool _ignoreCase;

    public WildcardMatcher(bool ignoreCase = true)
    {
        _ignoreCase = ignoreCase;
    }

    public bool IsMatch(string? input, string? pattern)
    {
        if (input == null || pattern == null)
            return false;

        var regex = BuildRegex(pattern, _ignoreCase);
        return regex.IsMatch(input);
    }

    public IEnumerable<string> Filter(IEnumerable<string> items, string pattern)
    {
        if (items == null || pattern == null)
            return Enumerable.Empty<string>();

        var regex = BuildRegex(pattern, _ignoreCase);
        return items.Where(item => item != null && regex.IsMatch(item));
    }

    public static string WildcardToRegex(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "^$";

        var escaped = Regex.Escape(pattern);
        var regexPattern = escaped.Replace(@"\*", ".*");
        regexPattern = regexPattern.Replace(@"\?", ".");
        regexPattern = Regex.Replace(regexPattern, @"\\\[(.*?)(?:\\\]|\])", "[$1]");

        return $"^{regexPattern}$";
    }

    private static Regex BuildRegex(string pattern, bool ignoreCase)
    {
        var regexString = WildcardToRegex(pattern);
        var options = RegexOptions.Singleline;
        if (ignoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return new Regex(regexString, options);
    }
}
