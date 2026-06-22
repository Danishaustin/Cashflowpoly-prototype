using UnityEngine;

public partial class GameState
{
    // Narasi prerequisite helpers for action counters.
    public int GetActionCount(string aksi)
    {
        switch (NormalizeActionName(aksi))
        {
            case "BahanMasakan":
                return Mathf.Max(0, bmAksiKe - 1);
            case "JualMasakan":
                return Mathf.Max(0, jmAksiKe - 1);
            case "Kebutuhan":
                return Mathf.Max(0, kAksiKe - 1);
            case "KerjaLepas":
                return Mathf.Max(0, klAksiKe - 1);
            case "TujuanFinansial":
                return Mathf.Max(0, tfAksiKe - 1);
            case "JumatBerkah":
                return JumatBerkah;
            default:
                return 0;
        }
    }

    public string NormalizeActionName(string aksi)
    {
        switch (aksi)
        {
            case "BeliBahan":
                return "BahanMasakan";
            case "JualMakanan":
                return "JualMasakan";
            case "BeliKebutuhan":
                return "Kebutuhan";
            default:
                return aksi;
        }
    }
}
