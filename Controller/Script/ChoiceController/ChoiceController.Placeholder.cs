using System.Collections;
using System.Collections.Generic;

public partial class ChoiceController
{
    // Keeps new menu buttons usable before their game rules are implemented.
    private void ShowUnavailableChoice(string choiceName)
    {
        StartCoroutine(ShowUnavailableChoiceDialog(choiceName));
    }

    private IEnumerator ShowUnavailableChoiceDialog(string choiceName)
    {
        yield return view.PlayDialogSteps(new List<string>
        {
            "Fitur " + choiceName + " belum tersedia."
        });

        view.ShowChoice("Choice1");
    }
}
