using System;
using System.Collections.Generic;

[Serializable]
public class NarasiData
{
    public string nama;
    public string aksi;
    public int aksiKe;
    public string narasi1;
    public string narasi2;
    public string narasi3;

}

[Serializable]
public class NarasiDatabase
{
    public List<NarasiData> narasi;
}