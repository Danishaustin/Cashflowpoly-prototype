public static class NarafinRuntimeConfig
{
    // Narafin tetap menjadi auth utama; UGS auth dipakai untuk fitur UGS seperti user files.
    public static bool UseUnityGameServicesAuth = true;

    // Fitur UGS wajib: login gagal bila UGS auth gagal.
    public static bool RequireUnityGameServicesAuth = true;

    // Ubah ke false jika editor narasi cukup memakai file lokal di Resources/Data/Narasi.
    public static bool UseUgsNarasiCloudSave = true;

    // Aplikasi yang ditutup paksa (mis. di-swipe dari daftar aplikasi) tidak memanggil OnApplicationQuit,
    // sehingga sesi login lama akan tetap tersimpan. Dengan true, sesi selalu dibersihkan saat aplikasi
    // dibuka sehingga pengguna kembali ke menu login. Ubah ke false bila ingin tetap login saat dibuka lagi.
    public static bool ClearSessionOnAppStart = true;
}
