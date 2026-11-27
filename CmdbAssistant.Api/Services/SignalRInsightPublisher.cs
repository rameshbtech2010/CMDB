using CmdbAssistant.Api.Hubs;
using CmdbAssistant.Core.Services;
using CmdbAssistant.Models;
using Microsoft.AspNetCore.SignalR;

namespace CmdbAssistant.Api.Services;

public sealed class SignalRInsightPublisher : IInsightPublisher
{
    private readonly IHubContext<InsightHub> hub;

    public SignalRInsightPublisher(IHubContext<InsightHub> hub)
    {
        this.hub = hub;
    }

    public Task PublishAsync(
        IReadOnlyList<InfrastructureInsight> insights,
        CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync("insightsUpdated", insights, cancellationToken);
}