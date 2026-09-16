using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameState
{
    // Penghitung aksi untuk narasi. Aksi lama memakai field khusus, aksi lain memakai penghitung umum
    // agar semua aksi bisa memicu dialog dan prasyarat "sudah X kali".
    private readonly Dictionary<string, int> generalActionCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public int GetActionCount(string aksi)
    {
        string key = NormalizeActionName(aksi);
        switch (key)
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
                return generalActionCounters.TryGetValue(key, out int count) ? Mathf.Max(0, count - 1) : 0;
        }
    }

    // Nomor urut aksi (mulai dari 1) untuk memilih narasi, lalu penghitungnya dinaikkan.
    public int NextActionKe(string aksi)
    {
        string key = NormalizeActionName(aksi);
        switch (key)
        {
            case "BahanMasakan":
                return bmAksiKe++;
            case "JualMasakan":
                return jmAksiKe++;
            case "Kebutuhan":
                return kAksiKe++;
            case "KerjaLepas":
                return klAksiKe++;
            case "TujuanFinansial":
                return tfAksiKe++;
            default:
                generalActionCounters.TryGetValue(key, out int current);
                int aksiKe = current < 1 ? 1 : current;
                generalActionCounters[key] = aksiKe + 1;
                return aksiKe;
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
