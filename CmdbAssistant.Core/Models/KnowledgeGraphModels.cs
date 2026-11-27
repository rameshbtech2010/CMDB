namespace CmdbAssistant.Models;

public sealed record CiNode(
    string Id,
    string Name,
    string CiClass,
    string Status,
    bool IsLive = false,
    bool IsBusinessCritical = false,
    bool IsSinglePointOfFailure = false,
    string? SupportGroupId = null,
    string? OperatingSystem = null,
    string? OsVersion = null);

public sealed record CiRelationship(string SourceId, string TargetId, string Type);

public sealed record InfrastructureInsight(
    string Severity,
    string Title,
    string Description,
    IReadOnlyList<string> AffectedServices,
    string Recommendation);