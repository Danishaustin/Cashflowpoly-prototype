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

        if (DataManager.Instance == null || DataManager.Instance.bahanDict == null)
        {
            Debug.LogWarning("Data bahan belum siap.");
            return;
        }

        initialBahanContainer.Clear();
        var bahanList = new List<BahanMakananData>(DataManager.Instance.bahanDict.Values);
        int itemCount = Mathf.Min(5, bahanList.Count);

        for (int i = 0; i < itemCount; i++)
        {
            BahanMakananData bahan = bahanList[i];
            var button = new Button
            {
                name = "InitialBahanOption_" + bahan.nama
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("initial-bahan-button");
            button.text = string.Empty;

            Sprite bahanSprite = LoadBahanButtonSprite(bahan.nama);
            if (bahanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(bahanSprite);
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

    private void BuildBahanChoiceButtons(VisualElement choiceBMContainer)
    {
        bahanChoiceButtons = new List<Button>();

        if (choiceBMContainer == null)
        {
            Debug.LogWarning("ChoiceBM tidak ditemukan.");
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.bahanDict == null)
        {
            Debug.LogWarning("Data bahan belum siap.");
            return;
        }

        foreach (var bahan in DataManager.Instance.bahanDict.Values)
        {
            var button = new Button
            {
                name = bahan.nama
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("item-choice-button");
            button.text = string.Empty;

            Sprite bahanSprite = LoadBahanButtonSprite(bahan.nama);
            if (bahanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(bahanSprite);
            }
            else
            {
                Debug.LogWarning("Sprite bahan tidak ditemukan untuk: " + bahan.nama);
            }

            choiceBMContainer.Add(button);
            bahanChoiceButtons.Add(button);
        }
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

        if (DataManager.Instance == null || DataManager.Instance.kebutuhanDict == null)
        {
            Debug.LogWarning("Data kebutuhan belum siap.");
            return;
        }

        foreach (var kebutuhan in DataManager.Instance.kebutuhanDict.Values)
        {
            var button = new Button
            {
                name = kebutuhan.nama
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("item-choice-button");
            button.text = string.Empty;

            Sprite kebutuhanSprite = LoadKebutuhanButtonSprite(kebutuhan.nama);
            if (kebutuhanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(kebutuhanSprite);
            }
            else
            {
                Debug.LogWarning("Sprite kebutuhan tidak ditemukan untuk: " + kebutuhan.nama);
            }

            choiceKContainer.Add(button);
            kebutuhanChoiceButtons.Add(button);
        }
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

        if (DataManager.Instance == null || DataManager.Instance.resepDict == null)
        {
            Debug.LogWarning("Data resep belum siap.");
            return;
        }

        foreach (var resep in DataManager.Instance.resepDict.Values)
        {
            var button = new Button
            {
                name = resep.nama
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("item-choice-button");
            button.AddToClassList("jual-masakan-choice-button");
            button.text = string.Empty;

            Sprite jualMasakanSprite = LoadJualMasakanButtonSprite(resep.nama);
            if (jualMasakanSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(jualMasakanSprite);
            }
            else
            {
                Debug.LogWarning("Sprite jual masakan tidak ditemukan untuk: " + resep.nama);
            }

            choiceJMContainer.Add(button);
            jualMasakanChoiceButtons.Add(button);
        }
    }

    private void BuildTujuanFinansialChoiceButtons(VisualElement choiceTFContainer)
    {
        tujuanFinansialChoiceButtons = new List<Button>();

        if (choiceTFContainer == null)
        {
            Debug.LogWarning("ChoiceTF tidak ditemukan.");
            return;
        }

        if (DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null)
        {
            Debug.LogWarning("Data tujuan finansial belum siap.");
            return;
        }

        foreach (var tujuanFinansial in DataManager.Instance.tujuanFinansialDict.Values)
        {
            var button = new Button
            {
                name = tujuanFinansial.nama
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("item-choice-button");
            button.text = string.Empty;

            Sprite tujuanFinansialSprite = LoadTujuanFinansialButtonSprite(tujuanFinansial.nama);
            if (tujuanFinansialSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(tujuanFinansialSprite);
            }
            else
            {
                Debug.LogWarning("Sprite tujuan finansial tidak ditemukan untuk: " + tujuanFinansial.nama);
            }

            choiceTFContainer.Add(button);
            tujuanFinansialChoiceButtons.Add(button);
        }
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

        if (DataManager.Instance == null || DataManager.Instance.targetKebutuhanList == null)
        {
            Debug.LogWarning("Data target kebutuhan belum siap.");
            return;
        }

        targetKebutuhanContainer.Clear();
        int itemCount = Mathf.Min(4, DataManager.Instance.targetKebutuhanList.Count);

        for (int i = 0; i < itemCount; i++)
        {
            var target = DataManager.Instance.targetKebutuhanList[i];
            var button = new Button
            {
                name = target.id
            };
            button.AddToClassList("choice-button");
            button.AddToClassList("target-kebutuhan-button");
            button.AddToClassList("target-kebutuhan-" + target.id);

            Sprite targetSprite = LoadTargetKebutuhanSprite(target.nama);
            if (targetSprite != null)
            {
                button.style.backgroundImage = new StyleBackground(targetSprite);
            }
            else
            {
                Debug.LogWarning("Sprite target kebutuhan tidak ditemukan untuk: " + target.nama);
            }

            var orderBadge = new Label(string.Empty);
            orderBadge.AddToClassList("target-kebutuhan-order-badge");
            button.Add(orderBadge);

            targetKebutuhanContainer.Add(button);
            targetKebutuhanChoiceButtons.Add(button);
        }
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


    private void BackFromChoiceBM(ClickEvent evt)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["ChoiceBM"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoiceK(ClickEvent evt)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["ChoiceK"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoiceJM(ClickEvent evt)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["ChoiceJM"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoicePS(ClickEvent evt)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["ChoicePS"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoiceMenabung(ClickEvent evt)
    {
        HideDialogContainer();
        SetChoiceBackground("Choice1");
        choiceContainers["ChoiceMenabung"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
    }

    private void BackFromChoiceTF(ClickEvent evt)
    {
        if (showOnlyAffordableTujuanFinansial)
        {
            choiceContainers["ChoiceTF"].RemoveFromClassList("show-choice");
            choiceController.CancelPendingTujuanFinansialConfirmation();
            return;
        }

        SetChoiceBackground("Choice1");
        showOnlyAffordableTujuanFinansial = false;
        choiceContainers["ChoiceTF"].RemoveFromClassList("show-choice");
        choiceContainers["Choice1"].AddToClassList("show-choice");
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

    private bool CanSelectKebutuhanForActivePlayer(string kebutuhanName)
    {
        if (GameState.Instance == null || DataManager.Instance == null || DataManager.Instance.kebutuhanDict == null)
        {
            return false;
        }

        if (!DataManager.Instance.kebutuhanDict.TryGetValue(kebutuhanName, out KebutuhanData kebutuhanData))
        {
            return false;
        }

        if (string.Equals(kebutuhanData.tipe, "primer", System.StringComparison.OrdinalIgnoreCase))
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
        if (string.IsNullOrEmpty(tujuanName) || GameState.Instance == null || DataManager.Instance == null || DataManager.Instance.tujuanFinansialDict == null)
        {
            return false;
        }

        if (!DataManager.Instance.tujuanFinansialDict.TryGetValue(tujuanName, out TujuanFinansialData tujuan))
        {
            return false;
        }

        return GameState.Instance.Saving >= tujuan.hargaBeli;
    }

}
