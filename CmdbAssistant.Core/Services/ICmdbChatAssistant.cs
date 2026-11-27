using CmdbAssistant.Models;

namespace CmdbAssistant.Core.Services;

public interface ICmdbChatAssistant
{
    Task<ChatResponse> AskAsync(string question, CancellationToken cancellationToken = default);
}

public interface IInsightPublisher
{
    Task PublishAsync(
        IReadOnlyList<InfrastructureInsight> insights,
        CancellationToken cancellationToken = default);
}

    public interface IInsightAnalyzer
    {
        Task<IReadOnlyList<InfrastructureInsight>> AnalyzeAsync(
        CancellationToken cancellationToken = default);
    }