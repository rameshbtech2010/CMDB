using System.Text.RegularExpressions;

namespace CmdbAssistant.Graph;

public static class CypherGuard
{
    private static readonly Regex ForbiddenClause = new(
        @"\b(CREATE|MERGE|DELETE|DETACH|SET|REMOVE|DROP|CALL|LOAD\s+CSV|FOREACH|GRANT|DENY|REVOKE)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ReadQueryStart = new(
        @"^\s*(MATCH|OPTIONAL\s+MATCH|WITH|UNWIND|RETURN)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsSafe(string cypher) =>
        !string.IsNullOrWhiteSpace(cypher) &&
        !cypher.Contains(';') &&
        ReadQueryStart.IsMatch(cypher) &&
        !ForbiddenClause.IsMatch(cypher);
}