namespace BlueHeighliner.Beacon.Services;

/// <summary>
/// Defense-in-depth screening of user-typed text for classic SQL-injection markers. All queries are parameterized,
/// so this is a second layer, not the primary defense; patterns are kept narrow so legitimate serials and names
/// (e.g. "SN-SELECT-01") pass.
/// </summary>
internal interface IInputValidator
{
    /// <summary>Gets the message to show when input is rejected.</summary>
    string RejectionMessage { get; }

    /// <summary>Checks inputs for injection markers.</summary>
    /// <param name="inputs">The user-typed values; null and empty values are skipped.</param>
    /// <returns>True if any input looks like an injection attempt.</returns>
    bool IsSuspicious(params string?[] inputs);
}

internal sealed class InputValidator : IInputValidator
{
    private readonly Regex[] suspiciousPatterns =
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

    public string RejectionMessage { get; } = "Input contains disallowed characters.";

    public bool IsSuspicious(params string?[] inputs)
    {
        foreach (string? input in inputs)
        {
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            foreach (Regex pattern in suspiciousPatterns)
            {
                if (pattern.IsMatch(input))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
