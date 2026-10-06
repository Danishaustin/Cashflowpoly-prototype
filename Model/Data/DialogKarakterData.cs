using System;
using System.Collections.Generic;

[Serializable]
public class DialogKarakterData
{
    public string id;
    public List<DialogPrerequisiteData> prerequisite;
    public string aksi;
    public int aksiValue;
    public string npcName;
    public string npcSprite;

    // Efek quest: setelah dialog selesai diputar, quest ini diubah ke state tersebut.
    public string questId;
    public string questState;
    public List<DialogKarakterLineData> lines;
}

[Serializable]
public class DialogPrerequisiteData
{
    public int uang;
    public int kebahagiaan;
    public int tabungan;
    public int emas;
    public int kartuPinjaman;
    public int mingguKe;
    public int hariKe;
    // Syarat asuransi punya tiga keadaan, jadi tidak bisa berupa bool. Lihat AsuransiPrerequisiteStatus.
    public int asuransiStatus;
    public List<string> bahanDimiliki;
    public List<string> kebutuhanDimiliki;
    public List<string> tujuanFinansialDimiliki;
    public List<string> masakanDijual;

    // Giliran pemain yang boleh memunculkan dialog ini, mis. [1, 3]. Daftar kosong maupun null berarti
    // syarat ini tidak dipakai, sehingga dialog berlaku untuk SEMUA pemain. JsonUtility tidak pernah
    // menghasilkan null untuk List, jadi daftar yang tidak ada di JSON menjadi daftar kosong.
    public List<int> giliranPemain;

    // Syarat quest: dialog hanya muncul bila quest ini sedang berada pada state tersebut.
    public string questId;
    public string questState;
}

public static class AsuransiPrerequisiteStatus
{
    public const int TidakDipakai = 0;
    public const int HarusDimiliki = 1;
    public const int TidakBolehDimiliki = 2;
}

[Serializable]
public class DialogKarakterLineData
{
    public string speaker;
    public string text;
}

[Serializable]
public class DialogKarakterDatabase
{
    public List<DialogKarakterData> dialogKarakter;
}
