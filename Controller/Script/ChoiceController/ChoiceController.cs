using UnityEngine;

public partial class ChoiceController : MonoBehaviour
{
    public UIManagerPlay view;

    public void HandleChoice(string currentContainer, string selectedChoice)
    {
        switch (currentContainer)
        {
            case "Choice1":
                HandleChoice1(selectedChoice);
                break;
            case "ChoiceBM":
                HandleChoiceBahan(selectedChoice);
                break;
            case "ChoiceJM":
                HandleChoiceJM(selectedChoice);
                break;
            case "ChoiceK":
                HandleChoiceK(selectedChoice);
                break;
            case "ChoiceKL":
                HandleChoiceKL();
                break;
            case "ChoiceTF":
                HandleChoiceTF(selectedChoice);
                break;
            case "ChoiceMenabung":
                HandleChoiceMenabung(selectedChoice);
                break;
            case "ChoiceTFConfirm":
                HandleChoiceTFConfirm(selectedChoice);
                break;
            case "ChoiceHargaEmas":
                HandleChoiceHargaEmas(selectedChoice);
                break;
            case "ChoiceEmasAction":
                HandleChoiceEmasAction(selectedChoice);
                break;
            case "ChoiceJumlahEmas":
                HandleChoiceJumlahEmas(selectedChoice);
                break;
            case "ChoiceRisikoKehidupan":
                HandleChoiceRisikoKehidupan(selectedChoice);
                break;
            case "ChoicePS":
                HandleChoicePinjamanSyariah(selectedChoice);
                break;
            case "JumatBerkah":
                JumatBerkah(selectedChoice);
                break;
            case "ChoiceKJumlah":
                HandleChoiceKJumlah(selectedChoice);
                break;
            case "ChoiceFinalHappiness":
                HandleChoiceFinalHappiness(selectedChoice);
                break;
            case "ChoiceTargetKebutuhan":
                HandleChoiceTargetKebutuhan(selectedChoice);
                break;
            case "ChoiceInitialBahan":
                HandleChoiceInitialBahan(selectedChoice);
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }
    }

    private void HandleChoice1(string selectedChoice)
    {
        switch (selectedChoice)
        {
            case "BahanMasakan":
                view.ShowChoice("BahanMasakan");
                break;
            case "Kebutuhan":
                view.ShowChoice("Kebutuhan");
                break;
            case "JualMasakan":
                view.ShowChoice("JualMasakan");
                break;
            case "TujuanFinansial":
                GameState.Instance.SetSavingText(0);
                view.UpdateSavingText(GameState.Instance.SavingText);
                view.ShowChoice("Menabung");
                break;
            case "KerjaLepas":
                HandleChoiceKL();
                break;
            case "PinjamanSyariah":
                view.ShowChoice("PinjamanSyariah");
                break;
            case "Asuransi":
                HandleChoiceAsuransi();
                break;
            default:
                Debug.Log("Pilihan tidak valid");
                break;
        }
    }

    // Helper method untuk menampilkan system dialog
    protected void ShowSystemDialogThen(string text, System.Action onComplete)
    {
        StartCoroutine(ShowSystemDialogThenRoutine(text, onComplete));
    }

    private System.Collections.IEnumerator ShowSystemDialogThenRoutine(string text, System.Action onComplete)
    {
        yield return view.PlaySystemDialogSteps(new System.Collections.Generic.List<string> { text });
        view.HideDialog();
        onComplete?.Invoke();
    }
}
