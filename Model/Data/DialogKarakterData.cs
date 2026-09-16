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
    public bool asuransiDimiliki;
    public List<string> bahanDimiliki;
    public List<string> kebutuhanDimiliki;
    public List<string> tujuanFinansialDimiliki;
    public List<string> masakanDijual;

    // Syarat quest: dialog hanya muncul bila quest ini sedang berada pada state tersebut.
    public string questId;
    public string questState;
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
