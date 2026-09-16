using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManagerPlay
{
    private const int InventoryBahanPage = 0;
    private const int InventoryKebutuhanPage = 1;
    private const int InventoryAsetPage = 2;
    private const int InventoryLastPage = InventoryAsetPage;
    private const int MaxKebutuhanDifferentItems = 15;

    // Quest and inventory panel rendering.
    private void ToggleQuestPanel(ClickEvent evt)
    {
        bool isVisible = questPanel.style.display == DisplayStyle.Flex;
        if (!isVisible)
        {
            RefreshQuestPanel();
        }

        questPanel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
        if (!isVisible && inventoryPanel != null)
        {
            HideInventoryPanel();
        }
    }

    private void HideQuestPanel(ClickEvent evt)
    {
        questPanel.style.display = DisplayStyle.None;
    }

    // Hanya quest pemain yang sedang bergiliran yang ditampilkan, dan namanya ditulis agar jelas quest siapa.
    // Quest yang belum diaktifkan narasi tidak ditampilkan karena belum diterima pemain itu.
    private void RefreshQuestPanel()
    {
        if (questList == null || GameState.Instance == null)
        {
            return;
        }

        questList.Clear();
        List<QuestData> quests = GameState.Instance.GetQuestList();
        if (quests.Count == 0)
        {
            var noQuestLabel = new Label("Paket quest ini belum punya quest.");
            noQuestLabel.AddToClassList("quest-item-title");
            questList.Add(noQuestLabel);
            return;
        }

        int activePlayer = GameState.Instance.turn;
        var playerLabel = new Label(PlayerPrefs.GetString("PlayerName_" + activePlayer, "Player " + activePlayer));
        playerLabel.AddToClassList("quest-player-title");
        questList.Add(playerLabel);

        bool hasQuest = false;
        foreach (QuestData quest in quests)
        {
            string questState = GameState.Instance.GetQuestState(activePlayer, quest.id);
            if (questState == QuestState.BelumAktif)
            {
                continue;
            }

            hasQuest = true;
            string questName = string.IsNullOrWhiteSpace(quest.nama) ? quest.id : quest.nama;
            var questRow = new VisualElement();
            questRow.AddToClassList("quest-item-row");

            var titleLabel = new Label(questName + " - ");
            titleLabel.AddToClassList("quest-item-title");
            questRow.Add(titleLabel);

            var stateLabel = new Label(QuestState.GetLabel(questState));
            stateLabel.AddToClassList("quest-item-state");
            stateLabel.AddToClassList(GetQuestStateClass(questState));
            questRow.Add(stateLabel);

            questList.Add(questRow);

            if (!string.IsNullOrWhiteSpace(quest.perintah))
            {
                var detailLabel = new Label(quest.perintah);
                detailLabel.AddToClassList("quest-item-detail");
                questList.Add(detailLabel);
            }
        }

        if (!hasQuest)
        {
            var emptyLabel = new Label("Belum ada quest.");
            emptyLabel.AddToClassList("quest-item-detail");
            questList.Add(emptyLabel);
        }
    }
    private static string GetQuestStateClass(string questState)
    {
        switch (QuestState.Normalize(questState))
        {
            case QuestState.Aktif:
                return "quest-state-aktif";
            case QuestState.Selesai:
                return "quest-state-selesai";
            case QuestState.Gagal:
                return "quest-state-gagal";
            default:
                return "quest-state-belum";
        }
    }


    private void ToggleInventoryPanel(ClickEvent evt)
    {
        if (inventoryPanel == null)
        {
            return;
        }

        bool isVisible = inventoryPanel.style.display == DisplayStyle.Flex
            && inventoryPanel.ClassListContains("show-inventory-panel");
        if (isVisible)
        {
            HideInventoryPanel();
            return;
        }

        inventoryPage = InventoryBahanPage;
        RefreshInventoryPanel();
        if (inventoryInputBlocker != null)
        {
            inventoryInputBlocker.style.display = DisplayStyle.Flex;
            inventoryInputBlocker.BringToFront();
        }

        inventoryPanel.style.display = DisplayStyle.Flex;
        inventoryPanel.BringToFront();
        inventoryPanel.schedule.Execute(() =>
        {
            inventoryPanel.AddToClassList("show-inventory-panel");
        }).StartingIn(1);

        if (questPanel != null)
        {
            questPanel.style.display = DisplayStyle.None;
        }
    }

    private void HideInventoryPanel(ClickEvent evt)
    {
        HideInventoryPanel();
    }

    private void HideInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        inventoryPanel.RemoveFromClassList("show-inventory-panel");
        inventoryPanel.schedule.Execute(() =>
        {
            if (!inventoryPanel.ClassListContains("show-inventory-panel"))
            {
                inventoryPanel.style.display = DisplayStyle.None;

                if (inventoryInputBlocker != null)
                {
                    inventoryInputBlocker.style.display = DisplayStyle.None;
                }
            }
        }).StartingIn(350);
    }

    private bool IsInventoryPanelOpen()
    {
        return inventoryPanel != null
            && inventoryPanel.style.display == DisplayStyle.Flex
            && inventoryPanel.ClassListContains("show-inventory-panel");
    }

    private void ShowPreviousInventoryPage(ClickEvent evt)
    {
        if (inventoryPage <= InventoryBahanPage)
        {
            return;
        }

        inventoryPage--;
        RefreshInventoryPanel();
    }

    private void ShowNextInventoryPage(ClickEvent evt)
    {
        if (inventoryPage >= InventoryLastPage)
        {
            return;
        }

        inventoryPage++;
        RefreshInventoryPanel();
    }

    private void RefreshInventoryPanel()
    {
        if (inventoryList == null)
        {
            return;
        }

        inventoryList.Clear();
        UpdateInventoryPlayerText();
        UpdateInventoryPageControls();

        if (GameState.Instance == null)
        {
            AddInventorySection("Inventory", new List<string> { "Data inventory belum siap." });
            return;
        }

        if (inventoryPage == InventoryBahanPage)
        {
            int totalBahan = GetBahanDifferentItemCount();
            AddInventorySection(
                "Bahan Masakan",
                BuildBahanInventoryTexts(totalBahan));
            return;
        }

        if (inventoryPage == InventoryKebutuhanPage)
        {
            int totalKebutuhan = GetKebutuhanDifferentItemCount();
            AddInventorySection(
                "Kebutuhan (" + totalKebutuhan + "/" + MaxKebutuhanDifferentItems + " jenis)",
                BuildKebutuhanInventoryTexts(totalKebutuhan));
            return;
        }

        AddInventorySection("Aset & Kartu", BuildAsetInventoryTexts());
    }

    private void UpdateInventoryPageControls()
    {
        if (inventoryPageText != null)
        {
            inventoryPageText.text = inventoryPage switch
            {
                InventoryBahanPage => "Bahan Masakan",
                InventoryKebutuhanPage => "Kebutuhan",
                _ => "Aset & Kartu"
            };
        }

        if (previousInventoryButton != null)
        {
            previousInventoryButton.SetEnabled(inventoryPage > InventoryBahanPage);
        }

        if (nextInventoryButton != null)
        {
            nextInventoryButton.SetEnabled(inventoryPage < InventoryLastPage);
        }
    }

    private List<string> BuildBahanInventoryTexts(int totalBahan)
    {
        List<string> items = new();
        if (GameState.Instance.bahanList == null || totalBahan == 0)
        {
            items.Add("Belum ada bahan.");
            return items;
        }

        foreach (var bahan in GameState.Instance.bahanList)
        {
            if (bahan.Value <= 0)
            {
                continue;
            }

            items.Add(FormatItemName(GameState.Instance.GetBahanDisplayName(bahan.Key)) + " x" + bahan.Value);
        }

        return items;
    }

    private List<string> BuildKebutuhanInventoryTexts(int totalKebutuhan)
    {
        List<string> items = new();
        if (GameState.Instance.kebutuhanList == null || totalKebutuhan == 0)
        {
            items.Add("Belum ada kebutuhan.");
            return items;
        }

        Dictionary<string, int> itemCounts = BuildKebutuhanItemCounts();
        Dictionary<string, string> itemTypes = BuildKebutuhanItemTypes();
        int shownItems = 0;

        foreach (var kebutuhan in itemCounts)
        {
            if (shownItems >= MaxKebutuhanDifferentItems)
            {
                continue;
            }

            string tipe = itemTypes.ContainsKey(kebutuhan.Key) ? itemTypes[kebutuhan.Key] : "-";
            items.Add(FormatItemName(kebutuhan.Key) + " x" + kebutuhan.Value + " (" + FormatItemName(tipe) + ")");
            shownItems++;
        }

        if (totalKebutuhan > MaxKebutuhanDifferentItems)
        {
            items.Add("+" + (totalKebutuhan - MaxKebutuhanDifferentItems) + " jenis kebutuhan lain tidak ditampilkan.");
        }

        return items;
    }

    private List<string> BuildAsetInventoryTexts()
    {
        if (GameState.Instance == null)
        {
            return new List<string> { "Data aset belum siap." };
        }

        int player = GameState.Instance.turn;
        string statusAsuransi = GameState.Instance.GetAsuransiDimiliki(player) ? "Aktif" : "Tidak Aktif";
        int jumlahEmas = GameState.Instance.GetEmas(player);
        int jumlahPinjamanSyariah = GameState.Instance.GetPinjamanSyariahCards(player);

        return new List<string>
        {
            "Asuransi: " + statusAsuransi,
            "Emas: " + jumlahEmas,
            "Kartu Pinjaman Syariah: " + jumlahPinjamanSyariah
        };
    }

    private int GetBahanDifferentItemCount()
    {
        if (GameState.Instance == null || GameState.Instance.bahanList == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var bahan in GameState.Instance.bahanList)
        {
            if (bahan.Value > 0)
            {
                count++;
            }
        }

        return count;
    }

    private int GetKebutuhanDifferentItemCount()
    {
        return BuildKebutuhanItemCounts().Count;
    }

    private Dictionary<string, int> BuildKebutuhanItemCounts()
    {
        Dictionary<string, int> itemCounts = new();
        if (GameState.Instance == null || GameState.Instance.kebutuhanList == null)
        {
            return itemCounts;
        }

        foreach (var kebutuhanGroup in GameState.Instance.kebutuhanList)
        {
            if (kebutuhanGroup.Value == null)
            {
                continue;
            }

            foreach (string kebutuhan in kebutuhanGroup.Value)
            {
                if (string.IsNullOrEmpty(kebutuhan))
                {
                    continue;
                }

                // Kartu disimpan dengan card_id; varian dengan nama sama (buku_1, buku_2) digabung per nama.
                string displayName = GameState.Instance.GetKebutuhanDisplayName(kebutuhan);
                itemCounts[displayName] = itemCounts.ContainsKey(displayName) ? itemCounts[displayName] + 1 : 1;
            }
        }

        return itemCounts;
    }

    private Dictionary<string, string> BuildKebutuhanItemTypes()
    {
        Dictionary<string, string> itemTypes = new();
        if (GameState.Instance == null || GameState.Instance.kebutuhanList == null)
        {
            return itemTypes;
        }

        foreach (var kebutuhanGroup in GameState.Instance.kebutuhanList)
        {
            if (kebutuhanGroup.Value == null)
            {
                continue;
            }

            foreach (string kebutuhan in kebutuhanGroup.Value)
            {
                string displayName = string.IsNullOrEmpty(kebutuhan) ? string.Empty : GameState.Instance.GetKebutuhanDisplayName(kebutuhan);
                if (!string.IsNullOrEmpty(displayName) && !itemTypes.ContainsKey(displayName))
                {
                    itemTypes[displayName] = kebutuhanGroup.Key;
                }
            }
        }

        return itemTypes;
    }

    private void AddInventorySection(string title, List<string> details)
    {
        VisualElement section = new();
        section.AddToClassList("inventory-section");

        Label titleLabel = new(title);
        titleLabel.AddToClassList("inventory-section-title");
        section.Add(titleLabel);

        foreach (string detail in details)
        {
            Label detailLabel = new(detail);
            detailLabel.AddToClassList("inventory-detail");
            section.Add(detailLabel);
        }

        inventoryList.Add(section);
    }

    private void UpdateInventoryPlayerText()
    {
        if (inventoryPlayerText == null)
        {
            return;
        }

        int player = GameState.Instance != null ? GameState.Instance.turn : PlayerPrefs.GetInt("PlayerTurn", 1);
        inventoryPlayerText.text = "Player: " + GetPlayerDisplayName(player);
    }

    private string GetPlayerDisplayName(int player)
    {
        string fallbackName = "Player " + player;
        return PlayerPrefs.GetString("PlayerName_" + player, fallbackName);
    }
}
