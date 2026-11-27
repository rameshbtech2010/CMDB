using CmdbAssistant.AI.Plugins;
using CmdbAssistant.AI.Services;
using CmdbAssistant.Api.Hubs;
using CmdbAssistant.Api.Services;
using CmdbAssistant.Core.Services;
using CmdbAssistant.Graph;
using CmdbAssistant.Insights;
using CmdbAssistant.KnowledgeGraph;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Description = "Cloud CMDB Infrastructure Assistant API. Developed by Ramesh Thirupathi.";
        return Task.CompletedTask;
    }));
builder.Services.AddValidation();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IKnowledgeGraph, InMemoryKnowledgeGraph>();
builder.Services.AddSingleton<GraphAssistant>();
builder.Services.AddSingleton<IInsightPublisher, SignalRInsightPublisher>();

var neo4jUri = builder.Configuration["Neo4j:Uri"];
var neo4jUsername = builder.Configuration["Neo4j:Username"];
var neo4jPassword = builder.Configuration["Neo4j:Password"];
var hasNeo4j = !string.IsNullOrWhiteSpace(neo4jUri) &&
    !string.IsNullOrWhiteSpace(neo4jUsername) &&
    !string.IsNullOrWhiteSpace(neo4jPassword);

if (hasNeo4j)
{
    builder.Services.AddSingleton<IGraphQueryService>(
        new Neo4jGraphService(neo4jUri!, neo4jUsername!, neo4jPassword!));
    builder.Services.AddSingleton<IInsightAnalyzer, Neo4jInsightAnalyzer>();
}
else
{
    builder.Services.AddSingleton<IInsightAnalyzer, DemoInsightAnalyzer>();
}

var aiProvider = builder.Configuration["AI:Provider"];
var aiModel = builder.Configuration["AI:Model"];
var aiApiKey = builder.Configuration["AI:ApiKey"];
var aiEndpoint = builder.Configuration["AI:Endpoint"];
var aiDeployment = builder.Configuration["AI:DeploymentName"];
var hasAi = !string.IsNullOrWhiteSpace(aiProvider) && !string.IsNullOrWhiteSpace(aiApiKey) &&
    (string.Equals(aiProvider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase)
        ? !string.IsNullOrWhiteSpace(aiEndpoint) && !string.IsNullOrWhiteSpace(aiDeployment)
        : !string.IsNullOrWhiteSpace(aiModel));

if (hasNeo4j && hasAi)
{
    builder.Services.AddSingleton<Kernel>(services =>
    {
        var kernelBuilder = Kernel.CreateBuilder();
        if (string.Equals(aiProvider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(aiDeployment!, aiEndpoint!, aiApiKey!);
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(aiModel!, aiApiKey!);
        }

        var graph = services.GetRequiredService<IGraphQueryService>();
        kernelBuilder.Plugins.AddFromObject(new CmdbPlugin(graph), "Cmdb");
        return kernelBuilder.Build();
    });
    builder.Services.AddSingleton<ICmdbChatAssistant, SemanticKernelChatAssistant>();
}
else
{
    builder.Services.AddSingleton<ICmdbChatAssistant>(services =>
        services.GetRequiredService<GraphAssistant>());
}

builder.Services.AddHostedService<InsightWorker>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<InsightHub>("/hubs/insights");
app.MapGet("/healthz", () => TypedResults.Ok(new { status = "healthy" }))
    .WithName("HealthCheck");

app.Run();
