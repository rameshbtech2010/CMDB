using System.ComponentModel.DataAnnotations;

namespace CmdbAssistant.Models;

/// <summary>Natural-language question submitted to the CMDB assistant.</summary>
public sealed record ChatRequest
{
    [Required, StringLength(1000, MinimumLength = 3)]
    public required string Question { get; init; }
}

/// <summary>Evidence item returned from the knowledge graph.</summary>
public sealed record EvidenceResponse(string Id, string Name, string CiClass, string Status);

/// <summary>Answer and supporting evidence for a CMDB question.</summary>
public sealed record ChatResponse(
    string Answer,
    string Intent,
    double Confidence,
    IReadOnlyList<EvidenceResponse> Evidence,
    IReadOnlyList<string> Recommendations,
    DateTimeOffset CreatedAt);

/// <summary>Counts of knowledge-graph nodes and relationships.</summary>
public sealed record GraphSummaryResponse(
    int NodeCount,
    int RelationshipCount,
    IReadOnlyDictionary<string, int> NodesByClass);