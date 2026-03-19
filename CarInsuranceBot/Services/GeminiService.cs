using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace CarInsuranceBot.Services;

public class GeminiService : IAiService
{
    private readonly GenerativeModel _model;
    private readonly ILogger<GeminiService> _logger;
    public GeminiService(string apiKey, ILogger<GeminiService> logger)
    {
        var googleAi = new GoogleAI(apiKey);
        _model = googleAi.GenerativeModel(Model.Gemini25Flash);
        _logger = logger;
    }

    public async Task<string> GetAiReplyAsync(string startPrompt, string userPrompt)
    {
        try
        {
            _logger.LogInformation("Sending request to Gemini API...");
            var response = await _model.GenerateContent($"Instructions: {startPrompt}; User prompt: {userPrompt}");
            return response.Text ?? "Oh! Something went wrong. Please try again.";
        }
        catch (Exception e) when (e.Message.Contains("429") || e.Message.Contains("quota"))
        {
            _logger.LogWarning("Gemini API rate limit reached.");
            return "Sorry. Something went wrong. Service is unavailable. Please try again later.";
        }
        catch(Exception e)
        {
            _logger.LogWarning(e.Message);
            return "Sorry. Something unexpected happened. Please try again later.";
        }
    }
}