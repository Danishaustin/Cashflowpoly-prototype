using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public class UIManagerPlay : MonoBehaviour
{
    [SerializeField] private ChoiceController choiceController;
    private Label dialog;
    private Label coinCounter;
    private Label happinessCounter;
    private Label dayCounter;
    private Label playerTurn;
    private Label savingCounter;
    private Label savingText;
    private Label jumatBerkahText;
    private Label hargaBeliText;
    private Label selesaiText;
    private VisualElement playerContainer;
    private VisualElement textContainer;
    private Dictionary<string, VisualElement> choiceContainers;
    private List<string> specialButtons;
    private EventCallback<TransitionEndEvent> transitionCallback;
    private string selectedChoice;
    private Tween dialogTween;
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        dialog = root.Q<Label>("Dialog");
        coinCounter = root.Q<Label>("CoinCounter");
        happinessCounter = root.Q<Label>("HappinessCounter");
        dayCounter = root.Q<Label>("DayCounter");
        playerTurn = root.Q<Label>("PlayerTurn");
        savingText = root.Q<Label>("SavingText");
        savingCounter = root.Q<Label>("SavingCounter");
        jumatBerkahText = root.Q<Label>("JumatBerkahText");
        hargaBeliText = root.Q<Label>("KHargaBeliText");
        selesaiText = root.Q<Label>("SelesaiText");

        playerContainer = root.Q<VisualElement>("PlayerContainer");
        textContainer = root.Q<VisualElement>("TextContainer");

        choiceContainers = new();

        specialButtons = new() { 
            "MinButton", 
            "MaxButton", 
            "DecreaseButton", 
            "IncreaseButton" ,
            "MinButtonJB",
            "MaxButtonJB",
            "DecreaseButtonJB",
            "IncreaseButtonJB",
            "MinButtonK",
            "MaxButtonK",
            "DecreaseButtonK",
            "IncreaseButtonK",
        };

        string[] choiceNames = {
            "Choice1",
            "ChoiceBM",
            "ChoiceK",
            "ChoiceJM",
            "ChoiceTF",
            "ChoiceKL",
            "ChoiceMenabung",
            "JumatBerkah",
            "ChoiceKJumlah",
            "PermainanSelesai"
        };

        foreach (var name in choiceNames)
        {
            RegisterChoiceGroup(name, root);
        }

        // playerContainer.style.display = DisplayStyle.None;
        textContainer.style.display = DisplayStyle.None;
        // choice1.style.display = DisplayStyle.None;

        playerContainer.RegisterCallback<TransitionEndEvent>(ShowTextContainer);
        UpdateDay(GameState.Instance.day);
        UpdatePlayerTurn(GameState.Instance.turn);
        UpdatePlayerStats();

        Invoke("ShowPlayerContainer", .1f);
        Invoke("ShowChoice1", 1.5f);
    }

    void RegisterChoiceGroup(string containerName, VisualElement root)
    {
        var container = root.Q<VisualElement>(containerName);
        choiceContainers[containerName] = container;
        var buttons = container.Query<Button>().ToList();

        foreach (var button in buttons)
        {
            if(specialButtons.Contains(button.name))
            {
                button.RegisterCallback<ClickEvent>(evt => OnChoiceTFClicked(evt, container));
                continue;
            }

            button.RegisterCallback<ClickEvent>(evt => OnChoiceClicked(evt, container));
        }
    }

    private void ShowPlayerContainer()
    {
        playerContainer.style.display = DisplayStyle.Flex;
        playerContainer.schedule.Execute(() =>
        {
            playerContainer.AddToClassList("show-character");
        }).StartingIn(1);
    }

    private void ShowTextContainer(TransitionEndEvent evt)
    {
        textContainer.style.display = DisplayStyle.Flex;
        Dialog(dialog, "Sekarang ngapain ya?");
    }

    private void ShowChoice1()
    {
        choiceContainers["Choice1"].style.display = DisplayStyle.Flex;
        choiceContainers["Choice1"].schedule.Execute(() =>
        {
            choiceContainers["Choice1"].AddToClassList("show-choice");
        }).StartingIn(1);
    }

    private void Dialog(Label l, string text, float duration = 0.5f)
    {
        if (l == dialog)
        {
            dialogTween?.Kill();
        }

        l.text = string.Empty;
        string m = text;
        Tween tween = DOTween.To(()=> l.text, x => l.text = x, m, duration) .SetEase(Ease.Linear);

        if (l == dialog)
        {
            dialogTween = tween;
        }
    }

    public void AddTextToDialog(string text, float duration = 0.5f, bool newLine = false)
    {
        Dialog(dialog, text, duration);

        if (newLine)
        {
            string currentText = dialog.text;
            string newText = currentText + text;
            DOTween.To(() => dialog.text, x => dialog.text = x, newText, duration).SetEase(Ease.Linear);
        }  
    }

    public IEnumerator PlayDialogSteps(List<string> texts, float duration = 0.5f)
    {
        foreach (var text in texts)
        {
            AddTextToDialog(text, duration);
            yield return null;

            bool goToNextText = false;
            while (!goToNextText)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (dialogTween != null && dialogTween.IsActive() && dialogTween.IsPlaying())
                    {
                        dialogTween.Complete();
                    }
                    else
                    {
                        goToNextText = true;
                    }
                }

                yield return null;
            }
        }
    }

    private void OnChoiceClicked(ClickEvent evt, VisualElement choiceContainer)
    {
        Button clickedButton = evt.target as Button;
        if (clickedButton == null)
        {
            return;
        }
        
        string choiceText = clickedButton.text;
        Debug.Log("Choice clicked: " + choiceText + " (Button name: " + clickedButton.name + ")");
        selectedChoice = clickedButton.name;
        
        transitionCallback = e => OnChoiceHidden(e, choiceContainer);
        choiceContainer.RegisterCallback(transitionCallback);
        SetButtonsEnabled(choiceContainer, false);
        choiceContainer.RemoveFromClassList("show-choice");
    }

    private void OnChoiceHidden(TransitionEndEvent evt, VisualElement choiceContainer)
    {
        // choiceContainer.style.display = DisplayStyle.None;
        choiceContainer.UnregisterCallback(transitionCallback);
        SetButtonsEnabled(choiceContainer, true);

        choiceController.HandleChoice(choiceContainer.name, selectedChoice);
    }

    private void OnChoiceTFClicked(ClickEvent evt, VisualElement choiceContainer)
    {
        Button clickedButton = evt.target as Button;
        if (clickedButton == null)
        {
            return;
        }
        
        string choiceText = clickedButton.text;
        Debug.Log("Choice clicked: " + choiceText + " (Button name: " + clickedButton.name + ")");
        selectedChoice = clickedButton.name;
        choiceController.HandleChoice(choiceContainer.name, selectedChoice);
    }

    private void SetButtonsEnabled(VisualElement container, bool enabled)
    {
        foreach (var btn in container.Query<Button>().ToList())
        {
            btn.SetEnabled(enabled);
        }
    }

    public void UpdateCoins(int coins)
    {
        Dialog(coinCounter, coins.ToString(), duration: 0.1f);
    }

    public void UpdateHappiness(int happiness)
    {
        Dialog(happinessCounter, happiness.ToString(), duration: 0.1f);
    }

    public void UpdateDay(int day)
    {
        dayCounter.text = "Day " + day;
    }

    public void UpdatePlayerTurn(int turn)
    {
        playerTurn.text = "Player " + turn;
    }

    public void UpdatePlayerStats()
    {
        UpdateCoins(GameState.Instance.Coins);
        UpdateHappiness(GameState.Instance.Happiness);
        UpdateSaving(GameState.Instance.Saving);
    }

    public void UpdateSaving(int saving)
    {
        savingCounter.text = saving.ToString();
    }

    public void UpdateSavingText(int saving)
    {
        savingText.text = saving.ToString();
    }

    public void UpdateJumatBerkahText(int amount)
    {
        jumatBerkahText.text = amount.ToString();
    }

    public void UpdateKebutuhanText(int amount)
    {
        hargaBeliText.text = amount.ToString();
    }

    public void ShowChoice(string id)
    {
        switch (id)
        {
            case "Choice1":
                if (GameState.Instance.IsGameOver())
                {
                    AddTextToDialog("Permainan selesai! Mendapatkan uang " + GameState.Instance.Coins + " dengan poin kebahagiaan " + GameState.Instance.Happiness + "\n", 0.5f, true);
                    choiceContainers["PermainanSelesai"].AddToClassList("show-choice");
                    return;
                }
                choiceContainers["Choice1"].AddToClassList("show-choice");
                break;
            case "BahanMasakan":
                choiceContainers["ChoiceBM"].AddToClassList("show-choice");
                break;
            case "Kebutuhan":
                choiceContainers["ChoiceK"].AddToClassList("show-choice");
                break;
            case "JualMasakan":
                choiceContainers["ChoiceJM"].AddToClassList("show-choice");
                break;
            case "TujuanFinansial":
                choiceContainers["ChoiceTF"].AddToClassList("show-choice");
                break;
            case "KerjaLepas":
                choiceContainers["ChoiceKL"].AddToClassList("show-choice");
                break;
            case "Menabung":
                choiceContainers["ChoiceMenabung"].AddToClassList("show-choice");
                break;
            case "JumatBerkah":
                choiceContainers["JumatBerkah"].AddToClassList("show-choice");
                GameState.Instance.SetSavingText(0);
                UpdateJumatBerkahText(GameState.Instance.SavingText);
                break;
            case "ChoiceKJumlah":
                choiceContainers["ChoiceKJumlah"].AddToClassList("show-choice");
                break;
            default:
                break;
        }
    }

}
