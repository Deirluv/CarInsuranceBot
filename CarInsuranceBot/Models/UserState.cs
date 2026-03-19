namespace CarInsuranceBot.Models;

public enum UserState
{
    None, // just started
    WaitingForPassport, // app asked for passport, waiting
    WaitingForVehicleDoc, // app asked for vehicle passport, waiting
    ConfirmingData, // mindee read the data. asking the user to confirm
    WaitingForPriceAgreement, // price showed. waiting for confirmation
    Finished // done
}