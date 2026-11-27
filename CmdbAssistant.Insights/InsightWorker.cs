using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CmdbAssistant.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CmdbAssistant.Insights;

public sealed class InsightWorker : BackgroundService
{
    private readonly IInsightAnalyzer analyzer;
    private readonly IInsightPublisher publisher;
    private readonly IConfiguration configuration;
    private readonly ILogger<InsightWorker> logger;

    public InsightWorker(
        IInsightAnalyzer analyzer,
        IInsightPublisher publisher,
        IConfiguration configuration,
        ILogger<InsightWorker> logger)
    {
        this.analyzer = analyzer;
        this.publisher = publisher;
        this.configuration = configuration;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Clamp(configuration.GetValue("InsightWorker:IntervalSeconds", 300), 30, 86400);
        string? lastSnapshot = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var insights = await analyzer.AnalyzeAsync(stoppingToken);
            var snapshot = JsonSerializer.Serialize(insights);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
            if (!string.Equals(hash, lastSnapshot, StringComparison.Ordinal))
            {
                await publisher.PublishAsync(insights, stoppingToken);
                logger.LogInformation("Published {InsightCount} CMDB infrastructure insights", insights.Count);
                lastSnapshot = hash;
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
}