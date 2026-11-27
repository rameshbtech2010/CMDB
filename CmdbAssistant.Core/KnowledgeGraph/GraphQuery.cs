namespace CmdbAssistant.KnowledgeGraph;

public sealed record GraphQuery(
    string Name,
    IReadOnlyDictionary<string, object?> Parameters);

public interface IGraphQueryService
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        GraphQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> RunReadOnlyCypherAsync(
        string cypher,
        CancellationToken cancellationToken = default);
}