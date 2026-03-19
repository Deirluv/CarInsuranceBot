using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Input;

namespace CarInsuranceBot.Services;

public class MindeeService : IDocumentService
{
    private readonly MindeeClientV2 _mindeeClient;
    private readonly ILogger<MindeeService> _logger;
    private readonly string _passportModelId;
    private readonly string _vehicleModelId;
    public MindeeService(string apiKey, string passportModelId, string vehicleModelId, ILogger<MindeeService> logger)
    {
        _passportModelId = passportModelId;
        _vehicleModelId = vehicleModelId;
        _mindeeClient = new MindeeClientV2(apiKey);
        _logger = logger;
    }

    public async Task<(string? Name, string? DocNumber)> ParsePassportAsync(Stream fileStream)
    {
        try 
        {
            if (fileStream.CanSeek) fileStream.Position = 0;
            
            var localInput = new LocalInputSource(fileStream, "passport.jpg");
            
            var inferenceParams = new InferenceParameters(_passportModelId);
            
            _logger.LogInformation("Sending request to Mindee API...");
            var response = await _mindeeClient.EnqueueAndGetInferenceAsync(localInput, inferenceParams);
            var fields = response.Inference.Result.Fields;
            
            string? names = fields.TryGetValue("given_names", out var namesField) ? namesField.ToString() : null;
            string? surnames = fields.TryGetValue("surnames", out var surnamesField) ? surnamesField.ToString() : null;
            string? docNumber = fields.TryGetValue("passport_number", out var docNumberField) ? docNumberField.ToString() : null;
            
            string fullName = $"{names} {surnames}".Trim();
            
            return (string.IsNullOrWhiteSpace(fullName) ? null : fullName, docNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Custom Passport Error: {ex.Message}");
            return (null, null);
        }
    }

    public async Task<(string? Vin, string? Model)> ParseVehicleDocAsync(Stream fileStream)
    {
        try
        {
            if (fileStream.CanSeek) fileStream.Position = 0;

            var inputSource = new LocalInputSource(fileStream, "vehicle_doc.jpg");

            var inferenceParams = new InferenceParameters(_vehicleModelId);
            var response = await _mindeeClient.EnqueueAndGetInferenceAsync(inputSource, inferenceParams);
            var fields = response.Inference.Result.Fields;

            string? vin = fields.TryGetValue("vin", out var vinField) ? vinField.ToString() : null;
            string? model = fields.TryGetValue("model", out var modelField) ? modelField.ToString() : null;

            return (vin, model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Mindee Vehicle Error: {ex.Message}");
            return (null, null);
        }
    }
}