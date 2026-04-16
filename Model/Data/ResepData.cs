using System;
using System.Collections.Generic;

[Serializable]
public class ResepData
{
    public string nama;
    public List<string> bahan;
    public int hargaJual;
    public int poinKebahagiaan;
}

[Serializable]
public class ResepDatabase
{
    public List<ResepData> resep;
}