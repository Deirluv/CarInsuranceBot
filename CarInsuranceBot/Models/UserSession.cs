namespace CarInsuranceBot.Models;

public class UserSession
{
    public long ChatId { get; set; }
    public UserState State { get; set; } = UserState.None;
    
    public string? FullName { get; set; }
    public string? DocumentNumber { get; set; }
    public string? VinCode { get; set; }
    public string? CarModel { get; set; }
    
    public void Reset()
    {
        State = UserState.None;
        FullName = null;
        DocumentNumber = null;
        VinCode = null;
        CarModel = null;
    }
}