using System;
using System.Collections.Generic;

[Serializable]
public class DialogKarakterData
{
    public string id;
    public string namaKarakter;
    public string spritePath;
    public bool menghadapKanan;
    public string aksiKarakter;
    public List<DialogPrerequisiteData> prerequisite;
    public List<PrerequisiteAksiData> prerequisiteAksi;
    public List<string> dialog;
    public List<DialogKarakterStepData> steps;
}

[Serializable]
public class DialogKarakterStepData
{
    public string namaKarakter;
    public string spritePath;
    public bool posisiKarakterDiKiri;
    public bool menghadapKanan;
    public string aksiKarakter;
    public List<string> dialog;
}

[Serializable]
public class DialogPrerequisiteData
{
    public string aksi;
    public int aksiValue;
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

    // Legacy fields are kept so older JSON can still be loaded safely.
    public string stat;
    public string comparison;
    public int value;
    public string bahan;
    public string kebutuhan;
    public string tipeKebutuhan;
    public int jumlah;
    public string tujuanFinansial;
    public int hariDalamMinggu;
    public int turnKe;
    public int playerCount;
    public string kondisi;
    public bool aktif;
}

[Serializable]
public class DialogKarakterDatabase
{
    public List<DialogKarakterData> dialogKarakter;
}
