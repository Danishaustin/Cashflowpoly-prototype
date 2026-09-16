using System.Threading.Tasks;
using UnityEngine;

public partial class ChoiceController
{
    private void BeginServerWait(string progressText)
    {
        if (view != null)
        {
            view.BeginServerWait(progressText);
        }
    }

    // Bila pesan penungguan sempat tampil, ditahan sebentar supaya tidak muncul lalu langsung hilang.
    private async Task EndServerWaitAsync()
    {
        if (this == null || view == null)
        {
            return;
        }

        float remainingSeconds = view.EndServerWait();
        if (remainingSeconds <= 0f)
        {
            return;
        }

        await Task.Delay(Mathf.RoundToInt(remainingSeconds * 1000f));
    }
}
