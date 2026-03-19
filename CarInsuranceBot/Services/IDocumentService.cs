namespace CarInsuranceBot.Services;

public interface IDocumentService
{
    Task<(string? Name, string? DocNumber)> ParsePassportAsync(Stream fileStream);
    Task<(string? Vin, string? Model)> ParseVehicleDocAsync(Stream fileStream);
}