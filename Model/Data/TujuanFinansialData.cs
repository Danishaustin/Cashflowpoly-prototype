using System;
using System.Collections.Generic;

[Serializable]
public class TujuanFinansialData
{
    public string nama;
    public int hargaBeli;
    public int poinKebahagiaan;
}

[Serializable]
public class TujuanFinansialDatabase
{
    public List<TujuanFinansialData> tujuanFinansial;
}