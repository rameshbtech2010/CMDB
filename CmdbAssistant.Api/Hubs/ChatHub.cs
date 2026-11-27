using CmdbAssistant.Core.Services;
using CmdbAssistant.Models;
using Microsoft.AspNetCore.SignalR;

namespace CmdbAssistant.Api.Hubs;

public sealed class ChatHub : Hub
{
    private readonly ICmdbChatAssistant assistant;

    public ChatHub(ICmdbChatAssistant assistant)
    {
        this.assistant = assistant;
    }

    public Task<ChatResponse> Ask(ChatRequest request) =>
        assistant.AskAsync(request.Question, Context.ConnectionAborted);
}

public sealed class InsightHub : Hub
{
}