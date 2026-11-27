using CmdbAssistant.Models;

namespace CmdbAssistant.KnowledgeGraph;

public interface IKnowledgeGraph
{
    IReadOnlyList<CiNode> Nodes { get; }
    IReadOnlyList<CiRelationship> Relationships { get; }
}