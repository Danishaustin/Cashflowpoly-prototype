using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    // Item choice button creation and pagination.
    private void BuildInitialBahanChoiceButtons(VisualElement initialBahanContainer)
    {
        initialBahanChoiceButtons = new List<Button>();

        if (initialBahanContainer == null)
        {
            Debug.LogWarning("InitialBahanGrid tidak ditemukan.");
            return;
        }

        initialBahanContainer.Clear();
        foreach (BahanChoiceOption option in GetBahanChoiceOptions())
        {
            if (initialBahanChoiceButtons.Count >= 5)
            {
                break;
            }

            var button = new Button
            {
                name = "InitialBahanOption_" + option.Key
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("initial-bahan-button");
            button.text = string.Empty;

            Sprite bahanSprite = LoadBahanSprite(option.Key, option.DisplayName);
            if (bahanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(bahanSprite);
            }
            else
            {
                button.text = option.DisplayName;
            }

            var checkBadge = new Label("✔");
            checkBadge.AddToClassList("initial-bahan-check");
            button.Add(checkBadge);

            initialBahanContainer.Add(button);
            initialBahanChoiceButtons.Add(button);
        }
    }

    public void SetSelectedInitialBahanButton(string buttonName)
    {
        if (initialBahanChoiceButtons == null)
        {
            return;
        }

        foreach (var button in initialBahanChoiceButtons)
        {
            bool isSelected = button.name == buttonName;
            if (isSelected)
            {
                button.AddToClassList("selected-initial-bahan");
            }
            else
            {
                button.RemoveFromClassList("selected-initial-bahan");
            }
        }
    }

    private void ResetInitialBahanSelectionButtons()
    {
        if (initialBahanChoiceButtons == null)
        {
            return;
        }

        foreach (var button in initialBahanChoiceButtons)
        {
            button.RemoveFromClassList("selected-initial-bahan");
        }
    }

    private sealed class BahanChoiceOption
    {
        public string Key;
        public string DisplayName;
    }

    // Bahan dari katalog ruleset session (kunci card_id, nama dari server); tanpa katalog memakai data lokal.
    private List<BahanChoiceOption> GetBahanChoiceOptions()
    {
        List<BahanChoiceOption> options = new List<BahanChoiceOption>();
        List<NarafinSetupIngredient> ingredients = NarafinActiveSession.GetIngredients(NarafinActiveSession.Catalog);
        if (ingredients.Count > 0)
        {
            foreach (NarafinSetupIngredient ingredient in ingredients)
            {
                options.Add(new BahanChoiceOption
                {
                    Key = ingredient.id,
                    DisplayName = string.IsNullOrWhiteSpace(ingredient.nama) ? ingredient.id : ingredient.nama
                });
            }

            return options;
        }

        Debug.LogWarning("Katalog bahan ruleset session kosong, tombol bahan tidak dibuat.");
        return options;
    }

    private void BuildBahanChoiceButtons(VisualElement choiceBMContainer)
    {
        bahanChoiceButtons = new List<Button>();

        if (choiceBMContainer == null)
        {
            Debug.LogWarning("ChoiceBM tidak ditemukan.");
            return;
        }

        foreach (BahanChoiceOption option in GetBahanChoiceOptions())
        {
            var button = new Button
            {
                name = option.Key
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("item-choice-button");
            button.text = string.Empty;

            Sprite bahanSprite = LoadBahanSprite(option.Key, option.DisplayName);
            if (bahanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(bahanSprite);
            }
            else
            {
                button.text = option.DisplayName;
                Debug.LogWarning("Sprite bahan tidak ditemukan untuk: " + option.DisplayName);
            }

            choiceBMContainer.Add(button);
            bahanChoiceButtons.Add(button);
        }
    }

    // Nama berkas sprite diambil dari id bahan katalog lewat NarafinSpriteMap; nama server dipakai
    // sebagai percobaan terakhir untuk ruleset yang id-nya belum terdaftar.
    private Sprite LoadBahanSprite(string bahanKey, string displayName)
    {
        Sprite sprite = LoadBahanButtonSprite(NarafinSpriteMap.GetIngredientSpriteName(bahanKey, displayName));
        return sprite != null ? sprite : LoadBahanButtonSprite(displayName);
    }

    private Sprite LoadBahanButtonSprite(string bahanName)
    {
        if (string.IsNullOrWhiteSpace(bahanName))
        {
            return null;
        }

        // Prioritaskan path sesuai request, fallback ke struktur Resources yang ada saat ini.
        Sprite sprite = Resources.Load<Sprite>("ItemSprite/Bahan/" + bahanName);
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.Load<Sprite>("Sprite/ItemSprite/Bahan/" + bahanName);
    }

    private void BuildKebutuhanChoiceButtons(VisualElement choiceKContainer)
    {
        kebutuhanChoiceButtons = new List<Button>();

        if (choiceKContainer == null)
        {
            Debug.LogWarning("ChoiceK tidak ditemukan.");
            return;
        }

        // Satu tombol per family kartu kebutuhan ruleset session; varian harga dipilih di ChoiceKJumlah.
        List<NarafinSetupNeed> families = NarafinActiveSession.GetNeedFamilies(NarafinActiveSession.Catalog);
        if (families.Count > 0)
        {
            foreach (NarafinSetupNeed need in families)
            {
                string family = NarafinActiveSession.GetNeedFamily(need);
                AddKebutuhanChoiceButton(choiceKContainer, family, GameState.GetKebutuhanSpriteName(family, need.nama), need.nama);
            }

            return;
        }

        Debug.LogWarning("Katalog kebutuhan ruleset session kosong, tombol kebutuhan tidak dibuat.");
    }

    private void AddKebutuhanChoiceButton(VisualElement choiceKContainer, string kebutuhanKey, string spriteName, string label)
    {
        var button = new Button
        {
            name = kebutuhanKey
        };
        button.AddToClassList("choice-button");
        button.AddToClassList("item-choice-button");
        button.text = string.Empty;

        Sprite kebutuhanSprite = LoadKebutuhanButtonSprite(spriteName);
        if (kebutuhanSprite != null)
        {
            button.style.backgroundImage = new StyleBackground(kebutuhanSprite);
        }
        else
        {
            button.text = label;
            Debug.LogWarning("Sprite kebutuhan tidak ditemukan untuk: " + label);
        }

        choiceKContainer.Add(button);
        kebutuhanChoiceButtons.Add(button);
    }

    private Sprite LoadKebutuhanButtonSprite(string kebutuhanName)
    {
        if (string.IsNullOrWhiteSpace(kebutuhanName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>("ItemSprite/Kebutuhan/" + kebutuhanName);
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.Load<Sprite>("Sprite/ItemSprite/Kebutuhan/" + kebutuhanName);
    }

    private void BuildJualMasakanChoiceButtons(VisualElement choiceJMContainer)
    {
        jualMasakanChoiceButtons = new List<Button>();

        if (choiceJMContainer == null)
        {
            Debug.LogWarning("ChoiceJM tidak ditemukan.");
            return;
        }

        // Kartu pesanan dari katalog ruleset session (kunci order id); sprite memakai nama resep lokal.
        List<NarafinSetupOrder> orders = NarafinActiveSession.GetOrders(NarafinActiveSession.Catalog);
        if (orders.Count > 0)
        {
            foreach (NarafinSetupOrder order in orders)
            {
                AddJualMasakanChoiceButton(choiceJMContainer, order.id, GameState.GetOrderLocalName(order), GameState.GetOrderDisplayName(order));
            }

            return;
        }

        Debug.LogWarning("Katalog pesanan ruleset session kosong, tombol jual masakan tidak dibuat.");
    }

    private void AddJualMasakanChoiceButton(VisualElement choiceJMContainer, string resepKey, string spriteName, string label)
    {
        var button = new Button
        {
            name = resepKey
        };
        button.AddToClassList("choice-button");
        button.AddToClassList("item-choice-button");
        button.AddToClassList("jual-masakan-choice-button");
        button.text = string.Empty;

        Sprite jualMasakanSprite = LoadJualMasakanButtonSprite(spriteName);
        if (jualMasakanSprite != null)
        {
            button.style.backgroundImage = new StyleBackground(jualMasakanSprite);
        }
        else
        {
            button.text = label;
            Debug.LogWarning("Sprite jual masakan tidak ditemukan untuk: " + spriteName);
        }

        choiceJMContainer.Add(button);
        jualMasakanChoiceButtons.Add(button);
    }

    private void BuildTujuanFinansialChoiceButtons(VisualElement choiceTFContainer)
    {
        tujuanFinansialChoiceButtons = new List<Button>();

        if (choiceTFContainer == null)
        {
            Debug.LogWarning("ChoiceTF tidak ditemukan.");
            return;
        }

        // Tujuan dari katalog ruleset session (kunci goal_id); sprite lokal dipetakan berdasarkan harga.
        List<NarafinSetupFinancialGoal> goals = NarafinActiveSession.GetFinancialGoals(NarafinActiveSession.Catalog);
        if (goals.Count > 0)
        {
            foreach (NarafinSetupFinancialGoal goal in goals)
            {
                AddTujuanFinansialChoiceButton(choiceTFContainer, goal.id, GameState.GetTujuanFinansialSpriteName(goal), goal.nama);
            }

            return;
        }

        Debug.LogWarning("Katalog tujuan finansial ruleset session kosong, tombol tujuan tidak dibuat.");
    }

    private void AddTujuanFinansialChoiceButton(VisualElement choiceTFContainer, string tujuanKey, string spriteName, string label)
    {
        var button = new Button
        {
            name = tujuanKey
        };
        button.AddToClassList("choice-button");
        button.AddToClassList("item-choice-button");
        button.text = string.Empty;

        Sprite tujuanFinansialSprite = LoadTujuanFinansialButtonSprite(spriteName);
        if (tujuanFinansialSprite != null)
        {
            button.style.backgroundImage = new StyleBackground(tujuanFinansialSprite);
        }
        else
        {
            button.text = label;
            Debug.LogWarning("Sprite tujuan finansial tidak ditemukan untuk: " + label);
        }

        choiceTFContainer.Add(button);
        tujuanFinansialChoiceButtons.Add(button);
    }

    private Sprite LoadJualMasakanButtonSprite(string masakanName)
    {
        if (string.IsNullOrWhiteSpace(masakanName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>("ItemSprite/JualMasakan/" + masakanName);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = Resources.Load<Sprite>("ItemSprite/JualMakanan/" + masakanName);
        if (sprite != null)
        {
            return sprite;
        }

        sprite = Resources.Load<Sprite>("Sprite/ItemSprite/JualMasakan/" + masakanName);
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.Load<Sprite>("Sprite/ItemSprite/JualMakanan/" + masakanName);
    }

    private Sprite LoadTujuanFinansialButtonSprite(string tujuanName)
    {
        if (string.IsNullOrWhiteSpace(tujuanName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>("ItemSprite/TujuanFinansial/" + tujuanName);
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.Load<Sprite>("Sprite/ItemSprite/TujuanFinansial/" + tujuanName);
    }

    private void BuildTargetKebutuhanChoiceButtons(VisualElement targetKebutuhanContainer)
    {
        targetKebutuhanChoiceButtons = new List<Button>();

        if (targetKebutuhanContainer == null)
        {
            Debug.LogWarning("TargetKebutuhanGrid tidak ditemukan.");
            return;
        }

        targetKebutuhanContainer.Clear();

        // Target kebutuhan diambil dari misi ruleset session agar mission_id pada pembagian awal valid.
        List<NarafinSetupMission> missions = NarafinActiveSession.GetMissions(NarafinActiveSession.Catalog);
        if (missions.Count > 0)
        {
            int missionCount = Mathf.Min(4, missions.Count);
            for (int i = 0; i < missionCount; i++)
            {
                NarafinSetupMission mission = missions[i];
                AddTargetKebutuhanButton(targetKebutuhanContainer, mission.id, LoadMissionSprite(mission.id, mission.nama), FormatItemName(mission.nama));
            }

            return;
        }

        Debug.LogWarning("Katalog misi ruleset session kosong, tombol target kebutuhan tidak dibuat.");
    }

    private void AddTargetKebutuhanButton(VisualElement targetKebutuhanContainer, string targetId, Sprite targetSprite, string label)
    {
        var button = new Button
        {
            name = targetId
        };
        button.AddToClassList("choice-button");
        button.AddToClassList("target-kebutuhan-button");
        button.AddToClassList("target-kebutuhan-" + targetId);

        if (targetSprite != null)
        {
            button.style.backgroundImage = new StyleBackground(targetSprite);
        }
        else
        {
            button.text = label;
            Debug.LogWarning("Sprite target kebutuhan tidak ditemukan untuk: " + label);
        }

        var orderBadge = new Label(string.Empty);
        orderBadge.AddToClassList("target-kebutuhan-order-badge");
        button.Add(orderBadge);

        targetKebutuhanContainer.Add(button);
        targetKebutuhanChoiceButtons.Add(button);
    }

    // Nama misi ruleset yang tidak bisa dicocokkan dari nama sprite target kebutuhan.
    private static readonly Dictionary<string, string> MissionSpriteNames = new Dictionary<string, string>
    {
        { "boneka", "Target Kebutuhan Boneka" },
        { "gameboy", "Target Kebutuhan Game Console" },
        { "jam", "Target Kebutuhan Jam Tangan" },
        { "hiburan", "Target Kebutuhan Wahana Bermain" }
    };

    // Misi ruleset memakai nama singkat (mis. "jam"), sprite lokal memakai nama target lengkap
    // ("Target Kebutuhan Jam Tangan"), jadi sprite dicari dari pemetaan lalu dari target lokal yang namanya memuat nama misi.
    // Sprite misi dikunci id misi katalog; nama misi hanya dipakai bila id-nya belum terdaftar di peta.
    private Sprite LoadMissionSprite(string missionId, string missionName)
    {
        Sprite sprite = LoadTargetKebutuhanSprite(NarafinSpriteMap.GetMissionSpriteName(missionId, missionName));
        if (sprite != null)
        {
            return sprite;
        }

        string missionKey = NarafinActiveSession.NormalizeName(missionName);
        return MissionSpriteNames.TryGetValue(missionKey, out string spriteName)
            ? LoadTargetKebutuhanSprite(spriteName)
            : null;
    }

    private Sprite LoadTargetKebutuhanSprite(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        // Prioritas path sesuai permintaan, fallback ke folder asset yang ada saat ini.
        Sprite sprite = Resources.Load<Sprite>("Sprite/ItemImage/TargetKebutuhan/" + targetName);
        if (sprite != null)
        {
            return sprite;
        }

        return Resources.Load<Sprite>("Sprite/ItemSprite/TargetKebutuhan/" + targetName);
    }

    private void RefreshTargetKebutuhanButtons()
    {
        if (targetKebutuhanChoiceButtons == null)
        {
            return;
        }

        foreach (var button in targetKebutuhanChoiceButtons)
        {
            button.SetEnabled(true);
        }
    }

    public void RefreshTargetKebutuhanSelectionUI(List<string> selectedTargetIds, int playerCount)
    {
        if (targetKebutuhanChoiceButtons == null)
        {
            return;
        }

        selectedTargetIds ??= new List<string>();
        int selectedCount = selectedTargetIds.Count;
        bool isSelectionFull = selectedCount >= playerCount;

        if (targetKebutuhanTitle != null)
        {
            targetKebutuhanTitle.text = "Pilih Target Kebutuhan Tiap Player";
        }

        foreach (var button in targetKebutuhanChoiceButtons)
        {
            int selectionOrder = selectedTargetIds.IndexOf(button.name);
            bool isSelected = selectionOrder >= 0;
            bool canSelectMore = !isSelectionFull || isSelected;

            button.SetEnabled(canSelectMore);

            if (isSelected)
            {
                button.AddToClassList("selected-target-kebutuhan");
            }
            else
            {
                button.RemoveFromClassList("selected-target-kebutuhan");
            }

            Label orderBadge = button.Q<Label>(className: "target-kebutuhan-order-badge");
            if (orderBadge == null)
            {
                continue;
            }

            if (isSelected)
            {
                orderBadge.text = (selectionOrder + 1).ToString();
                orderBadge.style.display = DisplayStyle.Flex;
            }
            else
            {
                orderBadge.text = string.Empty;
                orderBadge.style.display = DisplayStyle.None;
            }
        }

        if (targetKebutuhanResetButton != null)
        {
            targetKebutuhanResetButton.SetEnabled(selectedCount > 0);
        }

        if (targetKebutuhanNextButton != null)
        {
            targetKebutuhanNextButton.SetEnabled(selectedCount == playerCount);
        }
    }

    public bool IsTargetKebutuhanOption(string targetId)
    {
        if (targetKebutuhanChoiceButtons == null || string.IsNullOrEmpty(targetId))
        {
            return false;
        }

        foreach (var button in targetKebutuhanChoiceButtons)
        {
            if (button.name == targetId)
            {
                return true;
            }
        }

        return false;
    }

    public void SetOpeningSetupSubmitting(bool isSubmitting)
    {
        if (targetKebutuhanChoiceButtons != null)
        {
            foreach (var button in targetKebutuhanChoiceButtons)
            {
                button.SetEnabled(!isSubmitting);
            }
        }

        targetKebutuhanResetButton?.SetEnabled(!isSubmitting);
        targetKebutuhanNextButton?.SetEnabled(!isSubmitting);

        if (isSubmitting && targetKebutuhanTitle != null)
        {
            targetKebutuhanTitle.text = "Menyimpan pembagian awal...";
        }
    }

    public void ShowOpeningSetupError(string message)
    {
        if (targetKebutuhanTitle == null)
        {
            return;
        }

        targetKebutuhanTitle.text = "Pembagian awal gagal: "
            + (string.IsNullOrWhiteSpace(message) ? "silakan coba lagi." : message);
    }

    private string FormatItemName(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var formattedText = text[0].ToString();
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]) && !char.IsWhiteSpace(text[i - 1]))
            {
                formattedText += " ";
            }

            formattedText += text[i];
        }

        return formattedText;
    }


    // Kembali ke Choice1 harus menyegarkan keadaan tombol, sama seperti ShowChoice("Choice1").
    // Tanpa ini tombol yang semestinya mati — mis. Asuransi saat polis sudah dimiliki — kembali hidup.
    private void ShowChoice1AfterBack(string fromContainerId)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        if (!string.IsNullOrEmpty(fromContainerId) && choiceContainers.ContainsKey(fromContainerId))
        {
            choiceContainers[fromContainerId].RemoveFromClassList("show-choice");
        }

        UpdateMainChoiceButtonStates();
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoiceBM(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoiceBM");
    }

    private void BackFromChoiceK(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoiceK");
    }

    // Varian harga dipilih setelah jenis kebutuhan, jadi Back kembali ke daftar jenis kebutuhan.
    private void BackFromChoiceKJumlah(ClickEvent evt)
    {
        if (choiceController != null && choiceController.IsSubmittingKebutuhan)
        {
            return;
        }

        HideDialogContainer();
        SetChoiceBackground("ChoiceK");
        choiceContainers["ChoiceKJumlah"].RemoveFromClassList("show-choice");
        UpdateKebutuhanPage();
        choiceContainers["ChoiceK"].AddToClassList("show-choice");
    }

    private void BackFromChoiceJM(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoiceJM");
    }

    private void BackFromChoicePS(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoicePS");
    }

    private void BackFromChoiceKL(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoiceKL");
    }

    // Kembali ke pilihan beli, jual, atau lewati pada investasi emas.
    private void BackFromChoiceJumlahEmas(ClickEvent evt)
    {
        HideDialogContainer();
        choiceContainers["ChoiceJumlahEmas"].RemoveFromClassList("show-choice");
        choiceController?.BackToInvestasiEmasAction();
    }

    private void BackFromChoiceMenabung(ClickEvent evt)
    {
        ShowChoice1AfterBack("ChoiceMenabung");
    }

    private void BackFromChoiceTF(ClickEvent evt)
    {
        if (showOnlyAffordableTujuanFinansial)
        {
            choiceContainers["ChoiceTF"].RemoveFromClassList("show-choice");
            choiceController.BackFromTujuanFinansialList();
            return;
        }

        showOnlyAffordableTujuanFinansial = false;
        ShowChoice1AfterBack("ChoiceTF");
    }

    private void ShowPreviousBahanPage(ClickEvent evt)
    {
        if (bahanPage <= 0)
        {
            return;
        }

        bahanPage--;
        UpdateBahanPage();
    }

    private void ShowNextBahanPage(ClickEvent evt)
    {
        if (bahanPage >= GetLastBahanPage())
        {
            return;
        }

        bahanPage++;
        UpdateBahanPage();
    }

    private int GetLastBahanPage()
    {
        if (bahanChoiceButtons == null || bahanChoiceButtons.Count == 0)
        {
            return 0;
        }

        return Mathf.CeilToInt((float)bahanChoiceButtons.Count / BahanPageSize) - 1;
    }

    private void UpdateBahanPage()
    {
        if (bahanChoiceButtons == null || bahanChoiceButtons.Count == 0)
        {
            previousBahanButton.SetEnabled(false);
            nextBahanButton.SetEnabled(false);
            previousBahanButton.style.display = DisplayStyle.None;
            nextBahanButton.style.display = DisplayStyle.None;
            return;
        }

        bahanPage = Mathf.Clamp(bahanPage, 0, GetLastBahanPage());
        int firstIndex = bahanPage * BahanPageSize;
        int lastIndex = firstIndex + BahanPageSize;

        for (int i = 0; i < bahanChoiceButtons.Count; i++)
        {
            bahanChoiceButtons[i].style.display = i >= firstIndex && i < lastIndex
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            bool canBuyThisBahan = GameState.Instance != null
                && !GameState.Instance.IsBahanTotalAtLimit(GameState.Instance.turn)
                && !GameState.Instance.IsBahanAtLimit(GameState.Instance.turn, bahanChoiceButtons[i].name);
            bahanChoiceButtons[i].SetEnabled(canBuyThisBahan);
        }

        bool hasPreviousPage = bahanPage > 0;
        bool hasNextPage = bahanPage < GetLastBahanPage();

        previousBahanButton.SetEnabled(hasPreviousPage);
        nextBahanButton.SetEnabled(hasNextPage);
        previousBahanButton.style.display = hasPreviousPage ? DisplayStyle.Flex : DisplayStyle.None;
        nextBahanButton.style.display = hasNextPage ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowPreviousKebutuhanPage(ClickEvent evt)
    {
        if (kebutuhanPage <= 0)
        {
            return;
        }

        kebutuhanPage--;
        UpdateKebutuhanPage();
    }

    private void ShowNextKebutuhanPage(ClickEvent evt)
    {
        if (kebutuhanPage >= GetLastKebutuhanPage())
        {
            return;
        }

        kebutuhanPage++;
        UpdateKebutuhanPage();
    }

    private int GetLastKebutuhanPage()
    {
        if (kebutuhanChoiceButtons == null || kebutuhanChoiceButtons.Count == 0)
        {
            return 0;
        }

        return Mathf.CeilToInt((float)kebutuhanChoiceButtons.Count / KebutuhanPageSize) - 1;
    }

    private void UpdateKebutuhanPage()
    {
        if (kebutuhanChoiceButtons == null || kebutuhanChoiceButtons.Count == 0)
        {
            previousKebutuhanButton.SetEnabled(false);
            nextKebutuhanButton.SetEnabled(false);
            previousKebutuhanButton.style.display = DisplayStyle.None;
            nextKebutuhanButton.style.display = DisplayStyle.None;
            return;
        }

        kebutuhanPage = Mathf.Clamp(kebutuhanPage, 0, GetLastKebutuhanPage());
        int firstIndex = kebutuhanPage * KebutuhanPageSize;
        int lastIndex = firstIndex + KebutuhanPageSize;

        for (int i = 0; i < kebutuhanChoiceButtons.Count; i++)
        {
            kebutuhanChoiceButtons[i].style.display = i >= firstIndex && i < lastIndex
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            bool canBuyKebutuhan = CanSelectKebutuhanForActivePlayer(kebutuhanChoiceButtons[i].name);
            kebutuhanChoiceButtons[i].SetEnabled(canBuyKebutuhan);
        }

        bool hasPreviousPage = kebutuhanPage > 0;
        bool hasNextPage = kebutuhanPage < GetLastKebutuhanPage();

        previousKebutuhanButton.SetEnabled(hasPreviousPage);
        nextKebutuhanButton.SetEnabled(hasNextPage);
        previousKebutuhanButton.style.display = hasPreviousPage ? DisplayStyle.Flex : DisplayStyle.None;
        nextKebutuhanButton.style.display = hasNextPage ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // kebutuhanKey berisi family kartu ruleset session, atau nama lokal bila tanpa katalog.
    private bool CanSelectKebutuhanForActivePlayer(string kebutuhanKey)
    {
        if (GameState.Instance == null)
        {
            return false;
        }

        string tipe;
        List<NarafinSetupNeed> variants = NarafinActiveSession.GetNeedVariants(NarafinActiveSession.Catalog, kebutuhanKey);
        if (variants.Count > 0)
        {
            tipe = variants[0].tipe;
        }
        else
        {
            return false;
        }

        if (string.Equals(tipe, "primer", System.StringComparison.OrdinalIgnoreCase) || !GameState.Instance.RequirePrimaryBeforeOthers)
        {
            return true;
        }

        return GameState.Instance.HasKebutuhanPrimer(GameState.Instance.turn);
    }

    private void ShowPreviousJualMasakanPage(ClickEvent evt)
    {
        if (jualMasakanPage <= 0)
        {
            return;
        }

        jualMasakanPage--;
        UpdateJualMasakanPage();
    }

    private void ShowNextJualMasakanPage(ClickEvent evt)
    {
        if (jualMasakanPage >= GetLastJualMasakanPage())
        {
            return;
        }

        jualMasakanPage++;
        UpdateJualMasakanPage();
    }

    private int GetLastJualMasakanPage()
    {
        if (jualMasakanChoiceButtons == null || jualMasakanChoiceButtons.Count == 0)
        {
            return 0;
        }

        return Mathf.CeilToInt((float)jualMasakanChoiceButtons.Count / JualMasakanPageSize) - 1;
    }

    private void UpdateJualMasakanPage()
    {
        if (jualMasakanChoiceButtons == null || jualMasakanChoiceButtons.Count == 0)
        {
            previousJualMasakanButton.SetEnabled(false);
            nextJualMasakanButton.SetEnabled(false);
            previousJualMasakanButton.style.display = DisplayStyle.None;
            nextJualMasakanButton.style.display = DisplayStyle.None;
            return;
        }

        jualMasakanPage = Mathf.Clamp(jualMasakanPage, 0, GetLastJualMasakanPage());
        int firstIndex = jualMasakanPage * JualMasakanPageSize;
        int lastIndex = firstIndex + JualMasakanPageSize;

        for (int i = 0; i < jualMasakanChoiceButtons.Count; i++)
        {
            jualMasakanChoiceButtons[i].style.display = i >= firstIndex && i < lastIndex
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        bool hasPreviousPage = jualMasakanPage > 0;
        bool hasNextPage = jualMasakanPage < GetLastJualMasakanPage();

        previousJualMasakanButton.SetEnabled(hasPreviousPage);
        nextJualMasakanButton.SetEnabled(hasNextPage);
        previousJualMasakanButton.style.display = hasPreviousPage ? DisplayStyle.Flex : DisplayStyle.None;
        nextJualMasakanButton.style.display = hasNextPage ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowPreviousTujuanFinansialPage(ClickEvent evt)
    {
        if (tujuanFinansialPage <= 0)
        {
            return;
        }

        tujuanFinansialPage--;
        UpdateTujuanFinansialPage();
    }

    private void ShowNextTujuanFinansialPage(ClickEvent evt)
    {
        if (tujuanFinansialPage >= GetLastTujuanFinansialPage())
        {
            return;
        }

        tujuanFinansialPage++;
        UpdateTujuanFinansialPage();
    }

    private int GetLastTujuanFinansialPage()
    {
        if (tujuanFinansialChoiceButtons == null || tujuanFinansialChoiceButtons.Count == 0)
        {
            return 0;
        }

        return Mathf.CeilToInt((float)tujuanFinansialChoiceButtons.Count / TujuanFinansialPageSize) - 1;
    }

    private void UpdateTujuanFinansialPage()
    {
        if (tujuanFinansialChoiceButtons == null || tujuanFinansialChoiceButtons.Count == 0)
        {
            previousTujuanFinansialButton.SetEnabled(false);
            nextTujuanFinansialButton.SetEnabled(false);
            previousTujuanFinansialButton.style.display = DisplayStyle.None;
            nextTujuanFinansialButton.style.display = DisplayStyle.None;
            return;
        }

        var pageCandidates = new List<int>();
        for (int i = 0; i < tujuanFinansialChoiceButtons.Count; i++)
        {
            if (!showOnlyAffordableTujuanFinansial || IsTujuanFinansialAffordable(tujuanFinansialChoiceButtons[i].name))
            {
                pageCandidates.Add(i);
            }
        }

        if (pageCandidates.Count == 0)
        {
            foreach (var button in tujuanFinansialChoiceButtons)
            {
                button.style.display = DisplayStyle.None;
                button.SetEnabled(false);
            }

            previousTujuanFinansialButton.SetEnabled(false);
            nextTujuanFinansialButton.SetEnabled(false);
            previousTujuanFinansialButton.style.display = DisplayStyle.None;
            nextTujuanFinansialButton.style.display = DisplayStyle.None;
            return;
        }

        int filteredLastPage = Mathf.CeilToInt((float)pageCandidates.Count / TujuanFinansialPageSize) - 1;
        tujuanFinansialPage = Mathf.Clamp(tujuanFinansialPage, 0, filteredLastPage);
        int firstFilteredIndex = tujuanFinansialPage * TujuanFinansialPageSize;
        int endFilteredExclusive = Mathf.Min(firstFilteredIndex + TujuanFinansialPageSize, pageCandidates.Count);
        var visibleIndices = new HashSet<int>();

        for (int i = firstFilteredIndex; i < endFilteredExclusive; i++)
        {
            visibleIndices.Add(pageCandidates[i]);
        }

        for (int i = 0; i < tujuanFinansialChoiceButtons.Count; i++)
        {
            bool isAffordable = IsTujuanFinansialAffordable(tujuanFinansialChoiceButtons[i].name);
            bool shouldShow = visibleIndices.Contains(i);
            tujuanFinansialChoiceButtons[i].style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
            tujuanFinansialChoiceButtons[i].SetEnabled(!showOnlyAffordableTujuanFinansial || isAffordable);
        }

        bool hasPreviousPage = tujuanFinansialPage > 0;
        bool hasNextPage = tujuanFinansialPage < filteredLastPage;

        previousTujuanFinansialButton.SetEnabled(hasPreviousPage);
        nextTujuanFinansialButton.SetEnabled(hasNextPage);
        previousTujuanFinansialButton.style.display = hasPreviousPage ? DisplayStyle.Flex : DisplayStyle.None;
        nextTujuanFinansialButton.style.display = hasNextPage ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private bool IsTujuanFinansialAffordable(string tujuanName)
    {
        if (string.IsNullOrEmpty(tujuanName) || GameState.Instance == null)
        {
            return false;
        }

        // Tombol tujuan dari katalog ruleset memakai goal_id; tabungan pemain aktif yang dipakai sebagai batas.
        NarafinSetupFinancialGoal catalogGoal = NarafinActiveSession.FindFinancialGoal(NarafinActiveSession.Catalog, tujuanName);
        if (catalogGoal != null)
        {
            int player = GameState.Instance.turn;
            return !GameState.Instance.IsTujuanFinansialDimiliki(player, catalogGoal.id)
                && GameState.Instance.GetSaving(player) >= catalogGoal.hargaBeli;
        }

        return false;
    }

}
