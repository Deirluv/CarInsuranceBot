using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using CarInsuranceBot.Services;
using CarInsuranceBot.Handlers;
using CarInsuranceBot.Models;
using System.Collections.Concurrent;
using CarInsuranceBot.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarInsuranceBot
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Loading configuration
            var config = builder.Configuration;

            // Token registration
            string tgToken = config["ApiConfiguration:TelegramToken"] ?? throw new Exception("TG Token missing");
            string mindeeKey = config["ApiConfiguration:MindeeToken"] ?? throw new Exception("Mindee Key missing");
            string geminiKey = config["ApiConfiguration:GeminiToken"] ?? throw new Exception("Gemini Key missing");
            string groqKey = config["ApiConfiguration:GroqToken"] ?? throw new Exception("Groq Key missing");


            // Services registration
            builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(tgToken));
            builder.Services.AddSingleton<IDocumentService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<MindeeService>>();
                var passportId = config["MindeeConfig:PassportModelId"] ?? "";
                var vehicleId = config["MindeeConfig:VehicleModelId"] ?? "";
                return new MindeeService(mindeeKey, passportId, vehicleId, logger);
            });
            // builder.Services.AddSingleton<IAiService>(sp => 
            // {
            //     var logger = sp.GetRequiredService<ILogger<GeminiService>>();
            //     return new GeminiService(geminiKey, logger);
            // });
            builder.Services.AddSingleton<IAiService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<GroqService>>();
                return new GroqService(groqKey, logger);
            });
            builder.Services.AddSingleton<ConcurrentDictionary<long, UserSession>>();
            builder.Services.AddSingleton<UpdateHandler>();

            // Prompts list
            var prompts = builder.Configuration.GetSection("AIPrompts").Get<Dictionary<string, string>>();
            builder.Services.AddSingleton(prompts);

            // Background bot process
            builder.Services.AddHostedService<BotWorker>();

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}