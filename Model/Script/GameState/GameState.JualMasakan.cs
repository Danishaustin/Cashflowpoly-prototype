using System.Collections.Generic;
using System.Text;

public partial class GameState
{
    // Nama resep lokal (sprite, narasi) yang ejaannya berbeda dari kartu pesanan ruleset.
    private static readonly Dictionary<string, string> LocalResepNames = new Dictionary<string, string>
    {
        { "semanggi_surabaya", "SemanggiSuroboyo" },
        { "tahu_telur", "TahuTelor" }
    };

    public static string GetOrderDisplayName(NarafinSetupOrder order)
    {
        if (order == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(order.nama) ? order.id : order.nama;
    }

    // Nama resep lokal untuk kartu pesanan, mis. "lontong_balap" -> "LontongBalap".
    public static string GetOrderLocalName(NarafinSetupOrder order)
    {
        if (order == null || string.IsNullOrWhiteSpace(order.id))
        {
            return string.Empty;
        }

        if (LocalResepNames.TryGetValue(order.id, out string localName))
        {
            return localName;
        }

        string idKey = NarafinActiveSession.NormalizeName(order.id);
        string nameKey = NarafinActiveSession.NormalizeName(order.nama);
        if (DataManager.Instance != null && DataManager.Instance.resepDict != null)
        {
            foreach (string resepName in DataManager.Instance.resepDict.Keys)
            {
                string resepKey = NarafinActiveSession.NormalizeName(resepName);
                if (resepKey == idKey || resepKey == nameKey)
                {
                    return resepName;
                }
            }
        }

        StringBuilder builder = new StringBuilder(order.id.Length);
        foreach (string part in order.id.Split('_'))
        {
            if (part.Length > 0)
            {
                builder.Append(char.ToUpperInvariant(part[0]));
                builder.Append(part.Substring(1));
            }
        }

        return builder.ToString();
    }
}
