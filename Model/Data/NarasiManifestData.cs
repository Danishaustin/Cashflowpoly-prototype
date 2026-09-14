using System;
using System.Collections.Generic;

[Serializable]
public class NarasiManifestData
{
    public List<NarasiPackData> narasiPacks;
}

[Serializable]
public class NarasiPackData
{
    public string id;
    public string name;
    public string file;
}
