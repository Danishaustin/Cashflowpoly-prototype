using System;
using System.Collections.Generic;
using UnityEngine;

// Editor narasi berjalan di scene Home tanpa sesi, jadi tidak bisa membaca katalog server. Pilihan
// prasyaratnya diambil dari berkas ruleset lokal yang juga diunggah ke server
// (Resources/Data/rulesetMahir.json dan rulesetPemula.json), bukan dari berkas data lama.
// Nama dari kedua mode digabung supaya satu paket narasi bisa dipakai di keduanya.
public static class NarafinLocalRulesetCatalog
{
    // Nama untuk ditampilkan, Id untuk disimpan ke data narasi. Memisahkan keduanya membuat prasyarat
    // tahan terhadap perubahan ejaan nama kartu di ruleset.
    public sealed class Choice
    {
        public string Nama;
        public string Id;
    }

    private static readonly string[] RulesetResourcePaths = { "Data/rulesetMahir", "Data/rulesetPemula" };
    private static readonly string[] NeedTiers = { "primer", "sekunder", "tersier" };

    private static List<NarafinRulesetSetupDefinition> cachedDefinitions;
    private static List<Choice> cachedIngredients;
    private static List<Choice> cachedOrders;
    private static List<Choice> cachedNeeds;
    private static List<Choice> cachedFinancialGoals;

    public static List<Choice> GetIngredientChoices()
    {
        if (cachedIngredients == null)
        {
            cachedIngredients = new List<Choice>();
            foreach (NarafinRulesetSetupDefinition definition in GetDefinitions())
            {
                if (definition.ingredients == null)
                {
                    continue;
                }

                foreach (NarafinSetupIngredient ingredient in definition.ingredients)
                {
                    AddUnique(cachedIngredients, ingredient?.nama, ingredient?.id);
                }
            }
        }

        return cachedIngredients;
    }

    public static List<Choice> GetOrderChoices()
    {
        if (cachedOrders == null)
        {
            cachedOrders = new List<Choice>();
            foreach (NarafinRulesetSetupDefinition definition in GetDefinitions())
            {
                if (definition.orders == null)
                {
                    continue;
                }

                foreach (NarafinSetupOrder order in definition.orders)
                {
                    AddUnique(cachedOrders, order?.nama, order?.id);
                }
            }
        }

        return cachedOrders;
    }

    // Syarat kebutuhan bekerja pada tingkat family dan tipe, bukan kartu tunggal, jadi itulah yang disimpan.
    public static List<Choice> GetNeedChoices()
    {
        if (cachedNeeds == null)
        {
            cachedNeeds = new List<Choice>();
            foreach (string tier in NeedTiers)
            {
                AddUnique(cachedNeeds, tier, tier);
            }

            foreach (NarafinRulesetSetupDefinition definition in GetDefinitions())
            {
                if (definition.needs == null)
                {
                    continue;
                }

                foreach (NarafinSetupNeed need in definition.needs)
                {
                    AddUnique(cachedNeeds, need?.nama, need?.family);
                }
            }
        }

        return cachedNeeds;
    }

    public static List<Choice> GetFinancialGoalChoices()
    {
        if (cachedFinancialGoals == null)
        {
            cachedFinancialGoals = new List<Choice>();
            foreach (NarafinRulesetSetupDefinition definition in GetDefinitions())
            {
                if (definition.financial_goals == null)
                {
                    continue;
                }

                foreach (NarafinSetupFinancialGoal goal in definition.financial_goals)
                {
                    AddUnique(cachedFinancialGoals, goal?.nama, goal?.id);
                }
            }
        }

        return cachedFinancialGoals;
    }

    private static List<NarafinRulesetSetupDefinition> GetDefinitions()
    {
        if (cachedDefinitions != null)
        {
            return cachedDefinitions;
        }

        cachedDefinitions = new List<NarafinRulesetSetupDefinition>();
        foreach (string resourcePath in RulesetResourcePaths)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogWarning("Berkas ruleset lokal tidak ditemukan: " + resourcePath);
                continue;
            }

            NarafinRulesetComponentsResponse parsed = JsonUtility.FromJson<NarafinRulesetComponentsResponse>(asset.text);
            if (parsed?.definition != null)
            {
                cachedDefinitions.Add(parsed.definition);
            }
        }

        return cachedDefinitions;
    }

    private static void AddUnique(List<Choice> target, string nama, string id)
    {
        string cleanId = (id ?? string.Empty).Trim();
        if (cleanId.Length == 0)
        {
            return;
        }

        foreach (Choice existing in target)
        {
            if (string.Equals(existing.Id, cleanId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        string cleanNama = (nama ?? string.Empty).Trim();
        target.Add(new Choice { Nama = cleanNama.Length > 0 ? cleanNama : cleanId, Id = cleanId });
    }
}
