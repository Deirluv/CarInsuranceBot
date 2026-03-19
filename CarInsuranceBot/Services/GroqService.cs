using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;

namespace CarInsuranceBot.Services;

public class GroqService : IAiService
{
    private readonly ChatClient _client;
    private readonly ILogger<GroqService> _logger;
    private const string ModelName = "llama-3.3-70b-versatile"; 

    public GroqService(string apiKey, ILogger<GroqService> logger)
    {
        _logger = logger;
        
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://api.groq.com/openai/v1")
        };

        _client = new ChatClient(ModelName, new ApiKeyCredential(apiKey), options);
    }

    public async Task<string> GetAiReplyAsync(string startPrompt, string userPrompt)
    {
        try
        {
            _logger.LogInformation("Sending request to Groq API ({Model})...", ModelName);
            
            List<ChatMessage> messages = [
                new SystemChatMessage(startPrompt),
                new UserChatMessage(userPrompt)
            ];

            ChatCompletion completion = await _client.CompleteChatAsync(messages);
            
            return completion.Content[0].Text ?? "Oh! Something went wrong. Please try again.";
        }
        catch (ClientResultException e) when (e.Status == 429)
        {
            _logger.LogWarning("Groq API rate limit reached (429).");
            return "Service is temporarily busy. Please try again in a few seconds.";
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error in GroqService: {Message}", e.Message);
            return "Sorry, something unexpected happened. Please try again later.";
        }
    }
}