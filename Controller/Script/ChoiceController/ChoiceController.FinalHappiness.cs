using UnityEngine;

public partial class ChoiceController
{
    private const int MaxFinalHappinessInput = 100;

    private int finalHappinessPlayer = 1;
    private int finalHappinessInput;

    public void StartFinalHappinessInput()
    {
        finalHappinessPlayer = 1;
        finalHappinessInput = GameState.Instance.GetHappiness(finalHappinessPlayer);
        view.UpdateFinalHappinessPlayer(finalHappinessPlayer);
        view.UpdateFinalHappinessInput(finalHappinessInput);
        view.ShowChoice("FinalHappiness");
    }

    private void HandleChoiceFinalHappiness(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "MinButtonFinalHappiness":
                finalHappinessInput = 0;
                break;
            case "MaxButtonFinalHappiness":
                finalHappinessInput = MaxFinalHappinessInput;
                break;
            case "DecreaseButtonFinalHappiness":
                finalHappinessInput = Mathf.Max(0, finalHappinessInput - 1);
                break;
            case "IncreaseButtonFinalHappiness":
                finalHappinessInput = Mathf.Min(MaxFinalHappinessInput, finalHappinessInput + 1);
                break;
            case "ConfirmButtonFinalHappiness":
                SaveFinalHappinessAndAdvance();
                return;
            default:
                Debug.Log("Pilihan final happiness tidak valid: " + selectedChoice);
                return;
        }

        view.UpdateFinalHappinessInput(finalHappinessInput);
    }

    private void SaveFinalHappinessAndAdvance()
    {
        GameState.Instance.SetHappiness(finalHappinessPlayer, finalHappinessInput);

        if (finalHappinessPlayer >= GameState.Instance.playerCount)
        {
            view.ShowGameOverSummary();
            return;
        }

        finalHappinessPlayer++;
        finalHappinessInput = GameState.Instance.GetHappiness(finalHappinessPlayer);
        view.UpdateFinalHappinessPlayer(finalHappinessPlayer);
        view.UpdateFinalHappinessInput(finalHappinessInput);
        view.ShowChoice("FinalHappiness");
    }
}
