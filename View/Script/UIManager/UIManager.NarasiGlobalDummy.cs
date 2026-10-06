using System.Collections.Generic;
using UnityEngine.UIElements;

// DUMMY, BUKAN FITUR SUNGGUHAN.
//
// Berkas ini hanya memperagakan alur narasi dan quest global di Unity supaya bisa di-screenshot dan
// dijelaskan ke tim backend. Tidak ada panggilan API di sini; isi katalog, nama pemilik, dan hasil
// adopsi semuanya data karangan di dalam berkas ini.
//
// Dibuat terpisah supaya mudah dilepas: hapus berkas ini, hapus dua pemanggilan di UIManager.cs yang
// ditandai komentar "narasi global dummy", lalu hapus blok UXML bernama NarasiGlobal*/QuestGlobal*
// beserta kelas .narasi-global-* dan .narasi-select-* di Style.uss.
public partial class UIManager
{
    // Narasi dan quest memakai satu kelas peraga yang sama, bukan kode yang disalin dua kali, supaya
    // perilakunya dijamin identik dan perubahan cukup dilakukan di satu tempat.
    private sealed class GlobalPackDemo
    {
        private readonly List<string> katalog;
        private readonly Dictionary<string, int> jumlahAdopsi = new Dictionary<string, int>();

        private Button shareButton;
        private Label shareStatus;
        private DropdownField catalogDropdown;
        private Button adoptButton;
        private Label hint;
        private DropdownField ownPackDropdown;

        private bool shared;

        public GlobalPackDemo(List<string> katalog)
        {
            this.katalog = katalog;
        }

        public void Bind(VisualElement root, string awalan, DropdownField ownPackDropdown)
        {
            shareButton = root.Q<Button>(awalan + "GlobalShareButton");
            shareStatus = root.Q<Label>(awalan + "GlobalShareStatus");
            catalogDropdown = root.Q<DropdownField>(awalan + "GlobalCatalogDropdown");
            adoptButton = root.Q<Button>(awalan + "GlobalAdoptButton");
            hint = root.Q<Label>(awalan + "GlobalHint");
            this.ownPackDropdown = ownPackDropdown;

            if (catalogDropdown != null && katalog.Count > 0)
            {
                catalogDropdown.choices = new List<string>(katalog);
                catalogDropdown.SetValueWithoutNotify(katalog[0]);
            }

            shared = false;
            ApplyShareState();
        }

        public void RegisterCallbacks()
        {
            shareButton?.RegisterCallback<ClickEvent>(_ =>
            {
                shared = !shared;
                ApplyShareState();
            });
            adoptButton?.RegisterCallback<ClickEvent>(_ => Adopt());
        }

        // Dua keadaan dibuat berbeda warna dan berbeda teks tombol, supaya pada screenshot terlihat
        // bahwa membagikan paket adalah perubahan keadaan, bukan sekadar menekan tombol.
        private void ApplyShareState()
        {
            if (shareStatus != null)
            {
                shareStatus.text = shared ? "Global" : "Pribadi";
                shareStatus.EnableInClassList("narasi-global-badge-shared", shared);
            }

            if (shareButton != null)
            {
                shareButton.text = shared ? "Batal Bagikan" : "Bagikan";
            }

            if (hint != null)
            {
                hint.text = shared
                    ? "Instruktur lain bisa menemukan dan menyalin paket ini, tetapi tidak bisa mengubah punyamu."
                    : "Salinan menjadi milikmu sendiri dan bisa diedit bebas.";
            }
        }

        private void Adopt()
        {
            string dipilih = catalogDropdown?.value;
            if (string.IsNullOrEmpty(dipilih))
            {
                return;
            }

            // Nama paket adalah bagian sebelum tanda pemisah nama pemilik.
            int pemisah = dipilih.IndexOf(" - ");
            string namaPaket = (pemisah > 0 ? dipilih.Substring(0, pemisah) : dipilih).Trim();

            jumlahAdopsi.TryGetValue(namaPaket, out int sudah);
            sudah++;
            jumlahAdopsi[namaPaket] = sudah;

            // Server memberi akhiran (2), (3), ... bila nama sudah dipakai, jadi peragaan meniru itu
            // supaya alurnya tidak terlihat seolah adopsi berulang akan gagal.
            string namaSalinan = sudah == 1 ? namaPaket : namaPaket + " (" + sudah + ")";

            if (hint != null)
            {
                hint.text = "Tersalin sebagai \"" + namaSalinan + "\" dan sudah masuk daftar paketmu.";
            }

            // Paket hasil adopsi ikut muncul di dropdown paket milik sendiri, supaya screenshot
            // memperlihatkan hasil akhirnya, bukan hanya pesannya.
            if (ownPackDropdown != null)
            {
                List<string> pilihan = ownPackDropdown.choices != null
                    ? new List<string>(ownPackDropdown.choices)
                    : new List<string>();
                if (!pilihan.Contains(namaSalinan))
                {
                    pilihan.Add(namaSalinan);
                    ownPackDropdown.choices = pilihan;
                }

                ownPackDropdown.SetValueWithoutNotify(namaSalinan);
            }
        }
    }

    // Dua entri pertama tiap katalog sengaja bernama sama dari pemilik berbeda, karena nama paket
    // hanya unik per pemilik sehingga nama pemilik adalah satu-satunya pembeda di katalog.
    private readonly GlobalPackDemo narasiGlobalDemo = new GlobalPackDemo(new List<string>
    {
        "Narasi Dasar - Marco (24 dialog)",
        "Narasi Dasar - Danish (18 dialog)",
        "Narasi Ramadan - Marco (31 dialog)",
        "Narasi Pemula Singkat - Hugo (12 dialog)",
    });

    private readonly GlobalPackDemo questGlobalDemo = new GlobalPackDemo(new List<string>
    {
        "Quest Dasar - Marco (8 quest)",
        "Quest Dasar - Danish (5 quest)",
        "Quest Hemat Harian - Hugo (6 quest)",
    });

    private void BindNarasiGlobalDummy(VisualElement root)
    {
        narasiGlobalDemo.Bind(root, "Narasi", root.Q<DropdownField>("EditNarasiPackDropdown"));
        questGlobalDemo.Bind(root, "Quest", root.Q<DropdownField>("EditQuestPackDropdown"));
    }

    private void RegisterNarasiGlobalDummyCallbacks()
    {
        narasiGlobalDemo.RegisterCallbacks();
        questGlobalDemo.RegisterCallbacks();
    }
}
