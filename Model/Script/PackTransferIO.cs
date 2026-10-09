using System;
using System.IO;
using UnityEngine;

// Lapisan pengangkut berkas paket narasi dan paket tantangan. Hanya bagian ini yang tahu soal Android; pembentuk,
// pemeriksa, dan pemasang amplopnya ada di NarasiPackTransfer dan tidak tahu-menahu soal berkas.
//
// Pemisahannya disengaja agar ekspor bisa dicabut murah. Ekspor hanya masuk akal selama narasi
// ditulis di dalam aplikasi; begitu penulisan pindah ke dasbor web, dasbor itu mengekspor JSON-nya
// sendiri dan tidak ada lagi yang orisinal di sini untuk diekspor. Impor tetap berguna selamanya,
// karena "ambil paket dari server lalu pasang" adalah jalur yang sama dengan sumber berbeda.
//
// Dua jalur Android memang tidak setara sulitnya. Ekspor cukup menulis berkas lalu melempar
// ACTION_SEND. Impor harus lewat ACTION_OPEN_DOCUMENT karena scoped storage sejak Android 10 menutup
// pembacaan langsung folder Download, dan hasilnya berupa content:// URI yang tidak bisa dibuka
// File.ReadAllText sebelum disalin ke sandbox aplikasi. NativeFilePicker yang menanganinya.
public static class PackTransferIO
{
    public static bool IsImporSedangBerjalan { get; private set; }

    // Di Editor plugin Android tidak tersedia, jadi keduanya jatuh ke berkas biasa di folder ini.
    // Bukan sekadar penambal: alur ekspor-impor bisa diuji penuh tanpa perangkat.
    public static string EditorFolder => Path.Combine(Application.persistentDataPath, "Pertukaran");

    // ---------------------------------------------------------------- ekspor
    // Menerima amplop yang sudah jadi, bukan paketnya. Pembentuk amplop berbeda antara narasi dan
    // tantangan, tetapi menulis berkas lalu melemparnya ke share sheet sama persis untuk keduanya.
    public static PackTransferIOResult Ekspor(string json, string namaBerkas, string judul)
    {
        PackTransferIOResult hasil = new PackTransferIOResult();

        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(namaBerkas))
        {
            hasil.ErrorMessage = "Tidak ada yang bisa dibagikan.";
            return hasil;
        }

        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // temporaryCachePath, bukan persistentDataPath: berkas ini hanya titipan untuk share
            // sheet, dan sistem boleh membersihkannya kapan saja setelah terkirim.
            string path = Path.Combine(Application.temporaryCachePath, namaBerkas);
            File.WriteAllBytes(path, NarasiPackTransfer.ToBytes(json));

            new NativeShare()
                .AddFile(path, "application/json")
                .SetSubject(judul)
                .SetText("Cashflowpoly - " + judul)
                .Share();

            hasil.Success = true;
            hasil.Keterangan = judul + " siap dibagikan.";
            hasil.Path = path;
            return hasil;
#else
            Directory.CreateDirectory(EditorFolder);
            string path = Path.Combine(EditorFolder, namaBerkas);
            File.WriteAllBytes(path, NarasiPackTransfer.ToBytes(json));

            hasil.Success = true;
            hasil.Keterangan = "Paket diekspor ke:\n" + path;
            hasil.Path = path;
            return hasil;
#endif
        }
        catch (Exception ex)
        {
            hasil.ErrorMessage = "Gagal menulis berkas ekspor. Detail: " + ex.Message;
            return hasil;
        }
    }

    // ---------------------------------------------------------------- impor
    //
    // Pemilih berkas Android menjawab lewat callback, bukan nilai balik, jadi bentuknya callback juga
    // di sini. onSelesai dipanggil tepat satu kali pada setiap jalur, termasuk saat instruktur
    // membatalkan pemilihan, supaya pemanggilnya tidak perlu menebak kapan harus berhenti menunggu.
    public static void PilihBerkas(Action<PackTransferIOResult> onSelesai)
    {
        if (onSelesai == null)
        {
            return;
        }

        if (IsImporSedangBerjalan)
        {
            return;
        }

        IsImporSedangBerjalan = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            NativeFilePicker.PickFile(path =>
            {
                IsImporSedangBerjalan = false;

                if (string.IsNullOrEmpty(path))
                {
                    // Dibatalkan instruktur. Bukan galat, jadi tidak ada pesan merah.
                    onSelesai(new PackTransferIOResult { Dibatalkan = true });
                    return;
                }

                onSelesai(BacaBerkas(path));
            }, new string[] { "application/json", "text/plain", "*/*" });
        }
        catch (Exception ex)
        {
            IsImporSedangBerjalan = false;
            onSelesai(new PackTransferIOResult
            {
                ErrorMessage = "Pemilih berkas tidak bisa dibuka. Detail: " + ex.Message
            });
        }
#else
        IsImporSedangBerjalan = false;

        // Di Editor berkas pertama di folder pertukaran yang dipakai, supaya alurnya tetap bisa diuji.
        try
        {
            if (!Directory.Exists(EditorFolder))
            {
                onSelesai(new PackTransferIOResult
                {
                    ErrorMessage = "Folder pertukaran belum ada:\n" + EditorFolder
                });
                return;
            }

            string[] berkas = Directory.GetFiles(EditorFolder, "*.json");
            if (berkas.Length == 0)
            {
                onSelesai(new PackTransferIOResult
                {
                    ErrorMessage = "Tidak ada berkas .json di folder pertukaran:\n" + EditorFolder
                });
                return;
            }

            Array.Sort(berkas, StringComparer.Ordinal);
            onSelesai(BacaBerkas(berkas[berkas.Length - 1]));
        }
        catch (Exception ex)
        {
            onSelesai(new PackTransferIOResult
            {
                ErrorMessage = "Gagal membaca folder pertukaran. Detail: " + ex.Message
            });
        }
#endif
    }

    private static PackTransferIOResult BacaBerkas(string path)
    {
        PackTransferIOResult hasil = new PackTransferIOResult { Path = path };

        try
        {
            hasil.Json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            hasil.ErrorMessage = "Berkas tidak bisa dibaca. Detail: " + ex.Message;
            return hasil;
        }

        hasil.Success = true;
        return hasil;
    }
}

public class PackTransferIOResult
{
    public bool Success;
    public bool Dibatalkan;
    public string ErrorMessage;
    public string Keterangan;
    public string Path;
    public string Json;
}
