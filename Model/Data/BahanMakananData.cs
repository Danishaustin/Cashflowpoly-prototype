using System;
using System.Collections.Generic;

[Serializable]
public class BahanMakananData
{
    public string nama;
    public int hargaBeli;
}

[Serializable]
public class BahanMakananDatabase
{
    public List<BahanMakananData> bahan;
}