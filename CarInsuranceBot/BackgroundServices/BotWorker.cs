using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using CarInsuranceBot.Handlers;

namespace CarInsuranceBot.BackgroundServices;

public class BotWorker(
    ITelegramBotClient botClient,
    UpdateHandler updateHandler,
    ILogger<BotWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bot launching");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>(),
            DropPendingUpdates = true,
        };
        
        botClient.StartReceiving(
            updateHandler: async (bot, update, ct) => 
            {
                try 
                {
                    await updateHandler.HandleUpdateAsync(update, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Update error");
                }
            },
            errorHandler: (bot, ex, ct) => 
            {
                logger.LogCritical(ex, "API Telegram error");
                return Task.CompletedTask;
            },
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        logger.LogInformation("Bot successfully launched");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}