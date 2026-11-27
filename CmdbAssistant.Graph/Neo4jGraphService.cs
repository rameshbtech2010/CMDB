using System.Text.RegularExpressions;
using CmdbAssistant.Graph.Queries;
using CmdbAssistant.KnowledgeGraph;
using Neo4j.Driver;

namespace CmdbAssistant.Graph;

public sealed class Neo4jGraphService : IGraphQueryService, IAsyncDisposable
{
    private const int DefaultResultLimit = 200;
    private static readonly Regex HasLimit = new(@"\bLIMIT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly IDriver driver;

    public Neo4jGraphService(string uri, string username, string password)
    {
        driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(
        GraphQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!CypherCatalog.TryGet(query.Name, out var cypher))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "The requested graph query is not in the approved catalog.");
        }

        return ExecuteReadAsync(cypher, query.Parameters, cancellationToken);
    }

    public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> RunReadOnlyCypherAsync(
        string cypher,
        CancellationToken cancellationToken = default)
    {
        if (!CypherGuard.IsSafe(cypher))
        {
            throw new ArgumentException("Only a single read-only Cypher query is allowed.", nameof(cypher));
        }

        var boundedCypher = HasLimit.IsMatch(cypher) ? cypher : $"{cypher} LIMIT {DefaultResultLimit}";
        return ExecuteReadAsync(boundedCypher, new Dictionary<string, object?>(), cancellationToken);
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteReadAsync(
        string cypher,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var session = driver.AsyncSession(options => options.WithDefaultAccessMode(AccessMode.Read));
        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(cypher, parameters);
            var records = await cursor.ToListAsync(cancellationToken);
            return (IReadOnlyList<IReadOnlyDictionary<string, object?>>)records
                .Select(record => (IReadOnlyDictionary<string, object?>)record.Keys
                    .ToDictionary(key => key, key => (object?)record[key]))
                .ToList();
        });
    }

    public ValueTask DisposeAsync() => driver.DisposeAsync();
}