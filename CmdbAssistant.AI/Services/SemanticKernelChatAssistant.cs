using CmdbAssistant.AI.Prompts;
using CmdbAssistant.Core.Services;
using CmdbAssistant.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CmdbAssistant.AI.Services;

public sealed class SemanticKernelChatAssistant(Kernel kernel) : ICmdbChatAssistant
{
    public async Task<ChatResponse> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory(SystemPrompt.Text);
        history.AddUserMessage(question);
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        var completion = kernel.GetRequiredService<IChatCompletionService>();
        var response = await completion.GetChatMessageContentAsync(
            history,
            settings,
            kernel,
            cancellationToken: cancellationToken);

        return new ChatResponse(
            response.Content ?? "The assistant returned an empty response.",
            "semantic-kernel",
            0.9,
            [],
            [],
            DateTimeOffset.UtcNow);
    }
}