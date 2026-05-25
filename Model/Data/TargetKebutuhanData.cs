using System;
using System.Collections.Generic;

[Serializable]
public class TargetKebutuhanItemData
{
    public string jenisTarget;
    public string value;
}

[Serializable]
public class TargetKebutuhanData
{
    public string id;
    public string nama;
    public List<TargetKebutuhanItemData> kebutuhanTarget;
    public int poinKebahagiaanBerhasil;
    public int poinKebahagiaanGagal;
}

[Serializable]
public class TargetKebutuhanDatabase
{
    public List<TargetKebutuhanData> targetKebutuhan;
}
