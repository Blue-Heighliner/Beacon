using System.Text.RegularExpressions;

namespace Artemis.Services;

/// <summary>
/// Defense-in-depth screening of user-typed text for classic SQL-injection markers.
/// All queries are parameterized, so this is a second layer, not the primary defense —
/// patterns are kept narrow so legitimate serials/names (e.g. "SN-SELECT-01") pass.
/// </summary>
public static partial class InputValidator
{
    public const string RejectionMessage = "Input contains disallowed characters.";

    private static readonly Regex[] SuspiciousPatterns =
    [
        // Comment sequences used to truncate statements
        new(@"--", RegexOptions.Compiled),
        new(@"/\*", RegexOptions.Compiled),
        // Statement separator
        new(@";", RegexOptions.Compiled),
        // Quote adjacent to a SQL keyword (e.g. ' OR, ' UNION, ')DROP)
        new(@"['""]\s*(or|and|union|select|insert|update|delete|drop|exec|alter)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Multi-word attack phrases
        new(@"\bunion\s+(all\s+)?select\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bdrop\s+table\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\bdelete\s+from\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"\binsert\s+into\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Tautologies like OR 1=1, OR 'a'='a'
        new(@"\b(or|and)\s+['""]?\w+['""]?\s*=\s*['""]?\w+['""]?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // SQL Server extended procedures
        new(@"\bxp_\w+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    ];

    /// <summary>Returns true if any of the given inputs look like an injection attempt.</summary>
    public static bool IsSuspicious(params string?[] inputs)
    {
        foreach (var input in inputs)
        {
            if (string.IsNullOrEmpty(input))
                continue;
            foreach (var pattern in SuspiciousPatterns)
            {
                if (pattern.IsMatch(input))
                    return true;
            }
        }
        return false;
    }
}
