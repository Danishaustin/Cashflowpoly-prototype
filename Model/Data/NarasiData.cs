using System;
using System.Collections.Generic;

[Serializable]
public class PrerequisiteAksiData
{
    public string aksi;
    public int value;
}

[Serializable]
public class NarasiData
{
    public string nama;
    public List<PrerequisiteAksiData> prerequisiteAksi;
    public string narasi1;
    public string narasi2;
    public string narasi3;

}

[Serializable]
public class NarasiDatabase
{
    public List<NarasiData> narasi;
}
