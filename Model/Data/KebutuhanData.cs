using System;
using System.Collections.Generic;

[Serializable]
public class KebutuhanData
{
    public string nama;
    public string tipe;
}

[Serializable]
public class KebutuhanDatabase
{
    public List<KebutuhanData> kebutuhan;
}