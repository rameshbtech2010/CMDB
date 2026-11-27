using CmdbAssistant.AI.Services;
using CmdbAssistant.Core.Services;
using CmdbAssistant.Models;

namespace CmdbAssistant.Insights;

public sealed class DemoInsightAnalyzer(GraphAssistant assistant) : IInsightAnalyzer
{
    public Task<IReadOnlyList<InfrastructureInsight>> AnalyzeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(assistant.GetInsights());
}