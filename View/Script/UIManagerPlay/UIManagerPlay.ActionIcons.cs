using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private const string MainChoiceIconResourceRoot = "Sprite/ItemSprite/";

    // Gambar kartu yang mewakili tiap aksi utama, relatif terhadap Resources/Sprite/ItemSprite.
    private static readonly Dictionary<string, string> MainChoiceIconPaths = new Dictionary<string, string>
    {
        { "BahanMasakan", "Bahan/Nasi" },
        { "Kebutuhan", "Kebutuhan/Baju" },
        { "JualMasakan", "JualMakanan/NasiGoreng" },
        { "TujuanFinansial", "TujuanFinansial/Rumah" },
        { "KerjaLepas", "KerjaLepas/KerjaLepas" },
        { "PinjamanSyariah", "KartuPinjaman/PinjamanSyariah" },
        { "Asuransi", "Asuransi/Asuransi" }
    };

    // Lencana kartu di pojok kiri atas tiap tombol aksi utama. Tombol yang gambarnya tidak ditemukan
    // tetap tampil seperti biasa tanpa lencana.
    private void AddMainChoiceIcons(VisualElement choiceContainer)
    {
        if (choiceContainer == null)
        {
            return;
        }

        foreach (Button button in choiceContainer.Query<Button>().ToList())
        {
            if (button == null
                || string.IsNullOrWhiteSpace(button.name)
                || !MainChoiceIconPaths.TryGetValue(button.name, out string iconPath))
            {
                continue;
            }

            Sprite iconSprite = Resources.Load<Sprite>(MainChoiceIconResourceRoot + iconPath);
            if (iconSprite == null)
            {
                Debug.LogWarning("Ikon aksi tidak ditemukan: Resources/" + MainChoiceIconResourceRoot + iconPath);
                continue;
            }

            // Lencana tidak menerima klik supaya klik tetap diteruskan ke tombolnya.
            var iconBadge = new VisualElement
            {
                name = button.name + "Icon",
                pickingMode = PickingMode.Ignore
            };
            iconBadge.AddToClassList("main-choice-icon");
            iconBadge.style.backgroundImage = new StyleBackground(iconSprite);
            button.Add(iconBadge);
        }
    }
}
