using System;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Nama session yang dipakai bila instruktur membiarkan kolomnya kosong. Isinya dibekukan saat
    // panel Setup Session dibuka, bukan dihitung ulang saat Next ditekan: yang terkirim ke server
    // harus persis sama dengan yang terbaca di layar, termasuk bila panelnya dibiarkan terbuka
    // melewati tengah malam.
    private string sessionNamePlaceholderValue = string.Empty;

    // Kolom yang sedang disorot sudah jelas sedang diisi, jadi bayangannya tidak perlu lagi.
    private bool isSessionNameInputAktif;

    // Tanggal ditulis tahun-bulan-hari supaya daftar session di dasbor terurut benar saat diurutkan
    // sebagai teks.
    private static string BuildSessionNamePlaceholder()
    {
        return "Sesi-" + DateTime.Now.ToString("yyyy-MM-dd");
    }

    // Dipanggil tiap panel Setup Session disiapkan ulang.
    private void ResetSessionNamePlaceholder()
    {
        sessionNamePlaceholderValue = BuildSessionNamePlaceholder();
        isSessionNameInputAktif = false;

        if (sessionNamePlaceholder != null)
        {
            sessionNamePlaceholder.text = sessionNamePlaceholderValue;
        }

        UpdateSessionNamePlaceholderVisibility();
    }

    private void RegisterSessionNamePlaceholder()
    {
        if (sessionNameInput == null)
        {
            return;
        }

        sessionNameInput.RegisterValueChangedCallback(evt => UpdateSessionNamePlaceholderVisibility());

        // FocusInEvent dan FocusOutEvent dipakai, bukan FocusEvent dan BlurEvent: sorotan sebenarnya
        // jatuh pada elemen teks di dalam TextField, dan hanya sepasang yang pertama yang merambat
        // naik ke TextField-nya.
        sessionNameInput.RegisterCallback<FocusInEvent>(evt =>
        {
            isSessionNameInputAktif = true;
            UpdateSessionNamePlaceholderVisibility();
        });

        sessionNameInput.RegisterCallback<FocusOutEvent>(evt =>
        {
            isSessionNameInputAktif = false;
            UpdateSessionNamePlaceholderVisibility();
        });

        ResetSessionNamePlaceholder();
    }

    // Bayangan hanya tampil selama kolomnya kosong DAN tidak sedang disorot. Kolomnya sendiri tidak
    // pernah diisi teks bayangan, jadi tidak ada keadaan rancu "ini ketikan instruktur atau isian
    // kami". Meninggalkan kolom dalam keadaan kosong memunculkannya lagi, sehingga instruktur tetap
    // melihat nama apa yang akan dipakai bila ia tidak jadi mengisi.
    private void UpdateSessionNamePlaceholderVisibility()
    {
        if (sessionNamePlaceholder == null)
        {
            return;
        }

        bool kosong = sessionNameInput == null || string.IsNullOrWhiteSpace(sessionNameInput.value);
        bool tampil = kosong && !isSessionNameInputAktif;
        sessionNamePlaceholder.style.display = tampil ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Satu-satunya tempat nama session dibaca, supaya layar Next dan pembuatan session di server
    // tidak mungkin memakai nama yang berbeda.
    private string GetEffectiveSessionName()
    {
        string diketik = sessionNameInput != null ? sessionNameInput.value.Trim() : string.Empty;
        if (!string.IsNullOrWhiteSpace(diketik))
        {
            return diketik;
        }

        return sessionNamePlaceholderValue;
    }
}
