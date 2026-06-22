using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    // Player count, player names, validation, and starting the play scene.
    private void SetupPlayerCountDropdown()
    {
        playerCountDropdown.choices = new List<string>() { "3", "4" };
        playerCountDropdown.value = "3";
        playerCountDropdown.RegisterValueChangedCallback(evt => UpdatePlayerNameInputs());
        UpdatePlayerNameInputs();
    }

    private void OnPlayClicked(ClickEvent evt)
    {
        Debug.Log("Play button clicked!");
        playContainer.AddToClassList("show-play");
    }

    private void OnPlay2Clicked(ClickEvent evt)
    {
        Debug.Log("Play2 button clicked!");
        int playerCount = GetSelectedPlayerCount();
        for (int i = 0; i < playerCount; i++)
        {
            string playerName = playerNameInputs[i].value.Trim();

            if (playerName == string.Empty)
            {
                Debug.Log("Player name is empty.");
                playValidationText.text = "Nama Player " + (i + 1) + " tidak boleh kosong.";
                return;
            }

            if (playerName.Length > 8)
            {
                Debug.Log("Player name is too long.");
                playValidationText.text = "Nama Player " + (i + 1) + " maksimal 8 karakter.";
                return;
            }
        }

        playValidationText.text = string.Empty;
        PlayerPrefs.SetInt("PlayerCount", playerCount);

        for (int i = 0; i < playerNameInputs.Length; i++)
        {
            if (i < playerCount)
            {
                string playerName = playerNameInputs[i].value.Trim();
                PlayerPrefs.SetString("PlayerName_" + (i + 1), playerName);

                if (i == 0)
                {
                    PlayerPrefs.SetString("PlayerName", playerName);
                }
            }
            else
            {
                PlayerPrefs.DeleteKey("PlayerName_" + (i + 1));
            }
        }

        PlayerPrefs.Save();
        ChangeScene.Instance.ChangeToScene(1);
    }

    private int GetSelectedPlayerCount()
    {
        if (int.TryParse(playerCountDropdown.value, out int playerCount))
        {
            return Mathf.Clamp(playerCount, 3, 4);
        }

        return 3;
    }

    private void UpdatePlayerNameInputs()
    {
        int playerCount = GetSelectedPlayerCount();

        for (int i = 0; i < playerNameInputs.Length; i++)
        {
            playerNameGroups[i].style.display = i < playerCount ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
