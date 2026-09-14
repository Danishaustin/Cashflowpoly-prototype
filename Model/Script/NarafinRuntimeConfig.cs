public static class NarafinRuntimeConfig
{
    // Ubah ke false untuk memakai API Narafin kembali.
    public static bool UseOfflineMode = false;

    // Narafin tetap menjadi auth utama; UGS auth dipakai untuk fitur UGS seperti user files.
    public static bool UseUnityGameServicesAuth = true;

    // Ubah ke true kalau fitur UGS sudah wajib dan login harus gagal saat UGS auth gagal.
    public static bool RequireUnityGameServicesAuth = false;

    // Ubah ke false jika editor narasi cukup memakai file lokal di Resources/Data/Narasi.
    public static bool UseUgsNarasiCloudSave = true;
}
