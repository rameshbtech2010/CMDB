using CmdbAssistant.Core.Services;
using CmdbAssistant.KnowledgeGraph;
using CmdbAssistant.Models;
using Microsoft.AspNetCore.Mvc;

namespace CmdbAssistant.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ChatController : ControllerBase
{
    private readonly ICmdbChatAssistant assistant;
    private readonly IInsightAnalyzer insightAnalyzer;
    private readonly IKnowledgeGraph demoGraph;

    public ChatController(
        ICmdbChatAssistant assistant,
        IInsightAnalyzer insightAnalyzer,
        IKnowledgeGraph demoGraph)
    {
        this.assistant = assistant;
        this.insightAnalyzer = insightAnalyzer;
        this.demoGraph = demoGraph;
    }

    [HttpPost("chat")]
    [ProducesResponseType<ChatResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Ask(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        var response = await assistant.AskAsync(request.Question, cancellationToken);
        return Ok(response);
    }

    [HttpGet("insights")]
    [ProducesResponseType<IReadOnlyList<InfrastructureInsight>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InfrastructureInsight>>> GetInsights(
        CancellationToken cancellationToken) =>
        Ok(await insightAnalyzer.AnalyzeAsync(cancellationToken));

    [HttpGet("graph/summary")]
    [ProducesResponseType<GraphSummaryResponse>(StatusCodes.Status200OK)]
    public ActionResult<GraphSummaryResponse> GetGraphSummary() =>
        Ok(new GraphSummaryResponse(
            demoGraph.Nodes.Count,
            demoGraph.Relationships.Count,
            demoGraph.Nodes.GroupBy(node => node.CiClass)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.Count())));
}