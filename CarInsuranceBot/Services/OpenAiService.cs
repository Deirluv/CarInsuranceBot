using OpenAI;
using OpenAI.Chat;

namespace CarInsuranceBot.Services;

public class OpenAiService
{
    private readonly ChatClient _chatClient;

    public OpenAiService(string apiKey)
    {
        var client = new OpenAIClient(apiKey);
        _chatClient = client.GetChatClient("gpt-4o");
    }

    public async Task<string> GetAiReplyAsync(string startPrompt, string userPrompt)
    {
        List<ChatMessage> messages = [
            ChatMessage.CreateSystemMessage(startPrompt),
            ChatMessage.CreateUserMessage(userPrompt)
        ];
        
        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

        return completion.Content.FirstOrDefault()?.Text 
               ?? "Oh! Something went wrong. Please try again.";
    }
}