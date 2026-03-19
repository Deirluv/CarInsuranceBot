using System.Collections.Concurrent;
using CarInsuranceBot.Models;
using CarInsuranceBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CarInsuranceBot.Handlers;

public class UpdateHandler(
    ITelegramBotClient botClient, 
    IDocumentService mindeeService, 
    IAiService aiService,
    Dictionary<string, string> prompts)
{
    private static readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    private readonly string _aiSystemRole = prompts[PromptKeys.SystemRole];

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        // Buttons click processing
        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            await HandleCallbackAsync(update.CallbackQuery, ct);
            return;
        }

        if (update.Message is not { } message) return;
        long chatId = message.Chat.Id;
        var session = _sessions.GetOrAdd(chatId, id => new UserSession { ChatId = id });
        
        if (message.Text == "/start")
        {
            session.Reset();

            var aiWelcome = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Welcome]);
            
            session.State = UserState.WaitingForPassport;
            await botClient.SendMessage(chatId, aiWelcome, cancellationToken:ct);
            return;
        }

        // Main states logic
        switch (session.State)
        {
            case UserState.None:
                session.State = UserState.WaitingForPassport;
                var welcomeResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.PassportRequest]);
                await botClient.SendMessage(chatId, welcomeResponse, cancellationToken: ct);
                break;
            case UserState.WaitingForPassport:
                if (message.Type == MessageType.Photo)
                {
                    await ProcessPassport(message, session, ct);
                }
                else
                {
                    var aiResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.PassportRequest]);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken:ct);
                }
                break;

            case UserState.WaitingForVehicleDoc:
                if (message.Type == MessageType.Photo) await ProcessVehicleDoc(message, session, ct);
                else
                {
                    var aiResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.VehicleDocRequest]);
                    await botClient.SendMessage(chatId, aiResponse, cancellationToken:ct);
                }
                break;

            case UserState.ConfirmingData:
                var confirmationResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.ConfirmationRequest]);
                await botClient.SendMessage(chatId, confirmationResponse, cancellationToken:ct);
                break;

            case UserState.WaitingForPriceAgreement:
                await HandlePriceResponse(message, session, ct);
                break;
            
            case UserState.Finished:
                break;

            default:
                session.Reset();
                var errorResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Error]);
                await botClient.SendMessage(chatId, errorResponse, cancellationToken:ct);
                break;
        }
    }

    private async Task ProcessPassport(Message message, UserSession session, CancellationToken ct)
    {
        var readingResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.ReadingData]);
        await botClient.SendMessage(session.ChatId, readingResponse, cancellationToken:ct);
        
        using var stream = await DownloadFile(message.Photo!.Last().FileId, ct);
        var (name, docNum) = await mindeeService.ParsePassportAsync(stream);

        if (!string.IsNullOrEmpty(name))
        {
            session.FullName = name;
            session.DocumentNumber = docNum;
            session.State = UserState.WaitingForVehicleDoc;
            
            string dynamicPrompt = string.Format(prompts[PromptKeys.VehicleDocPersonalized], name);
            var vehicleDocResponse = await aiService.GetAiReplyAsync(_aiSystemRole, dynamicPrompt);
            await botClient.SendMessage(session.ChatId, vehicleDocResponse, cancellationToken:ct);
        }
        else
        {
            var cancelResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Cancel]);
            await botClient.SendMessage(session.ChatId, cancelResponse, cancellationToken:ct);
        }
    }

    private async Task ProcessVehicleDoc(Message message, UserSession session, CancellationToken ct)
    {
        var analyzeResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.AnalyzeData]);
        await botClient.SendMessage(session.ChatId, analyzeResponse, cancellationToken:ct);
        using var stream = await DownloadFile(message.Photo!.Last().FileId, ct);
        var (vin, model) = await mindeeService.ParseVehicleDocAsync(stream);

        session.VinCode = vin;
        session.CarModel = model;
        session.State = UserState.ConfirmingData;
        
        string dynamicPrompt = string.Format(prompts[PromptKeys.ResultSummary], session.FullName, session.DocumentNumber, vin, model);
        var resultResponse = await aiService.GetAiReplyAsync(_aiSystemRole, dynamicPrompt);
        
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Yes, everything is correct ✅", "data_ok"), 
                    InlineKeyboardButton.WithCallbackData("No, there are mistakes ❌", "data_error") }
        });
        
        await botClient.SendMessage(session.ChatId, resultResponse, replyMarkup: keyboard, cancellationToken:ct);
    }

    private async Task HandleCallbackAsync(CallbackQuery callback, CancellationToken ct)
    {
        var session = _sessions.GetOrAdd(callback.Message!.Chat.Id, id => new UserSession { ChatId = id });

        if (callback.Data == "data_ok")
        {
            session.State = UserState.WaitingForPriceAgreement;
            
            var priceResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.PriceOffer]);
            await botClient.EditMessageText(session.ChatId, callback.Message.MessageId, priceResponse, cancellationToken: ct);
        }
        else if (callback.Data == "data_error")
        {
            session.State = UserState.WaitingForPassport;
            
            var errorResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.DataErrorRetry]);
            await botClient.SendMessage(session.ChatId, errorResponse, cancellationToken:ct);
        }
        else if (callback.Data == "restart")
        {
            session.Reset();
            session.State = UserState.WaitingForPassport;
    
            var welcomeResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Welcome]);
            await botClient.SendMessage(session.ChatId, welcomeResponse, cancellationToken: ct);
        }
    }

    private async Task HandlePriceResponse(Message message, UserSession session, CancellationToken ct)
    {
        string input = message.Text?.ToLower() ?? "";
        
        string dynamicAgreePrompt = string.Format(prompts[PromptKeys.AgreeCheck], input);
        var agreeResponse = await aiService.GetAiReplyAsync(_aiSystemRole, dynamicAgreePrompt);

        if (agreeResponse == "Yes")
        {
            session.State = UserState.Finished;
            
            var agreePolicyResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.AgreePolicy]);
            await botClient.SendMessage(session.ChatId, agreePolicyResponse, cancellationToken:ct);

            string dynamicPolicyPrompt = string.Format(prompts[PromptKeys.PolicyGeneration], session.FullName, session.CarModel, session.VinCode);
            
            string policyResponce = await aiService.GetAiReplyAsync(_aiSystemRole, dynamicPolicyPrompt);
            await botClient.SendMessage(session.ChatId, policyResponce, cancellationToken:ct);
            
            var congratulateResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Congratulate]);
            await botClient.SendMessage(session.ChatId, congratulateResponse, cancellationToken:ct);
        }
        else
        {
            var apologyResponse = await aiService.GetAiReplyAsync(_aiSystemRole, prompts[PromptKeys.Apology]);
            
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("I agree now ($100) ✅", "data_ok") },
                new[] { InlineKeyboardButton.WithCallbackData("Start over 🔄", "restart") }
            });

            await botClient.SendMessage(session.ChatId, apologyResponse, replyMarkup: keyboard, cancellationToken:ct);
        }
    }

    private async Task<MemoryStream> DownloadFile(string fileId, CancellationToken ct)
    {
        var stream = new MemoryStream();
        await botClient.GetInfoAndDownloadFile(fileId, stream, ct);
        stream.Position = 0;
        return stream;
    }
}