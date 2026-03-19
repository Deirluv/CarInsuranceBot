namespace CarInsuranceBot.Services;

public interface IAiService
{
    Task<string> GetAiReplyAsync(string startPrompt, string userPrompt);
}