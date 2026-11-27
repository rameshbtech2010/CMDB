using System.Globalization;
using CmdbAssistant.KnowledgeGraph;
using CmdbAssistant.Core.Services;
using CmdbAssistant.Models;

namespace CmdbAssistant.Insights;

public sealed class Neo4jInsightAnalyzer(IGraphQueryService graph) : IInsightAnalyzer
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>();

    public async Task<IReadOnlyList<InfrastructureInsight>> AnalyzeAsync(CancellationToken cancellationToken = default)
    {
        var insights = new List<InfrastructureInsight>();
        var singlePoints = await Query("insights-single-points", cancellationToken);
        foreach (var row in singlePoints)
        {
            var name = Value(row, "name");
            insights.Add(new InfrastructureInsight(
                "high",
                $"Potential single point of failure: {name}",
                $"{name} has no REDUNDANT_WITH relationship in the graph.",
                StringValues(row, "services"),
                "Validate redundancy and recovery options for this component."));
        }

        var unhealthy = await Query("insights-non-operational", cancellationToken);
        foreach (var row in unhealthy)
        {
            var ciName = Value(row, "ci");
            var serviceName = Value(row, "service");
            var status = Value(row, "status");
            insights.Add(new InfrastructureInsight(
                string.Equals(status, "Down", StringComparison.OrdinalIgnoreCase) ? "critical" : "medium",
                $"{status} dependency: {ciName}",
                $"Live service {serviceName} depends on {ciName}, which is {status}.",
                [serviceName],
                "Review the dependency health and notify the service owner."));
        }

        var orphaned = await Query("insights-orphaned-applications", cancellationToken);
        insights.AddRange(orphaned.Select(row => new InfrastructureInsight(
            "low",
            $"Application has no service: {Value(row, "application")}",
            "This business application has no HAS_SERVICE relationship to an application service.",
            [],
            "Confirm whether the application is retired or add its service mapping.")));

        var unsupported = await Query("insights-unsupported-servers", cancellationToken);
        insights.AddRange(unsupported.Select(row => new InfrastructureInsight(
            "high",
            $"Server OS support ended: {Value(row, "server")}",
            $"{Value(row, "os")} {Value(row, "version")} reached end of support on {Value(row, "endOfSupport")}.",
            [],
            "Plan an operating-system upgrade or document an approved exception.")));

        var unassigned = await Query("insights-unassigned-cis", cancellationToken);
        insights.AddRange(unassigned.Select(row => new InfrastructureInsight(
            "medium",
            $"CI has no support group: {Value(row, "name")}",
            $"The {Value(row, "class")} configuration item has no SUPPORTED_BY relationship.",
            [],
            "Assign an accountable support group.")));

        return insights;
    }

    private Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> Query(
        string name,
        CancellationToken cancellationToken) =>
        graph.QueryAsync(new GraphQuery(name, NoParameters), cancellationToken);

    private static string Value(IReadOnlyDictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var value)
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "unknown"
            : "unknown";

    private static IReadOnlyList<string> StringValues(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is not IEnumerable<object> values)
        {
            return [];
        }

        return values.Select(item => Convert.ToString(item, CultureInfo.InvariantCulture) ?? "unknown").ToArray();
    }
}