using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

public partial class UIManagerPlay : MonoBehaviour
{
    private const int BahanPageSize = 5;
    private const int KebutuhanPageSize = 5;
    private const int JualMasakanPageSize = 5;
    private const int TujuanFinansialPageSize = 5;
    private const int BackgroundTransitionDurationMs = 400;
    private const int DefaultDebugTargetDay = 6;

    [SerializeField] private int debugTargetDay = 6;
    private static readonly string[] BahanIconClasses =
    {
        "bahan-nasi-icon",
        "bahan-telur-icon",
        "bahan-daging-icon",
        "bahan-sayuran-icon",
        "bahan-tahu-icon"
    };

    private static readonly string[] BackgroundClasses =
    {
        "background-city",
        "background-choice-bm",
        "background-choice-k",
        "background-choice-jm",
        "background-choice-tf"
    };

    private static readonly Dictionary<string, string> ChoiceBackgroundClasses = new()
    {
        { "Choice1", "background-city" },
        { "BahanMasakan", "background-choice-bm" },
        { "ChoiceBM", "background-choice-bm" },
        { "Kebutuhan", "background-choice-k" },
        { "ChoiceK", "background-choice-k" },
        { "ChoiceKJumlah", "background-choice-k" },
        { "JualMasakan", "background-choice-jm" },
        { "ChoiceJM", "background-choice-jm" },
        { "Menabung", "background-choice-tf" },
        { "ChoiceMenabung", "background-choice-tf" },
        { "TujuanFinansial", "background-choice-tf" },
        { "ChoiceTF", "background-choice-tf" },
        { "ChoiceTFConfirm", "background-choice-tf" },
        { "PinjamanSyariah", "background-city" },
        { "ChoicePS", "background-city" },
        { "HargaEmas", "background-city" },
        { "ChoiceHargaEmas", "background-city" },
        { "EmasAction", "background-city" },
        { "ChoiceEmasAction", "background-city" },
        { "JumlahEmas", "background-city" },
        { "ChoiceJumlahEmas", "background-city" },
        { "ChoiceInitialBahan", "background-choice-bm" },
        { "ChoiceTargetKebutuhan", "background-city" }
    };

    [SerializeField] private ChoiceController choiceController;

    private Label nameTag;
    private Label dialog;
    private Label coinCounter;
    private Label happinessCounter;
    private Label dayCounter;
    private Label weekCounter;
    private Label dayNameCounter;
    private Label playerTurn;
    private Label savingCounter;
    private Label savingText;
    private Label jumatBerkahText;
    private Label hargaBeliText;
    private Label hargaEmasText;
    private Label jumlahEmasText;
    private Label risikoCoinChangeText;
    private Label risikoHargaChangeText;
    private Label risikoWarningText;
    private Label risikoDecisionPlayerText;
    private Label targetKebutuhanTitle;
    private Label initialBahanTitle;
    private Label finalHappinessPlayerText;
    private Label finalHappinessInputText;
    private Label selesaiText;
    private Label inventoryPlayerText;
    private Label tfConfirmText;
    private Label emasActionDecisionText;
    private Button pauseToggleButton;
    private Button backChoiceBMButton;
    private Button previousBahanButton;
    private Button nextBahanButton;
    private Button backChoiceKButton;
    private Button previousKebutuhanButton;
    private Button nextKebutuhanButton;
    private Button backChoiceJMButton;
    private Button previousJualMasakanButton;
    private Button nextJualMasakanButton;
    private Button backChoicePSButton;
    private Button backChoiceMenabungButton;
    private Button backChoiceTFButton;
    private Button previousTujuanFinansialButton;
    private Button nextTujuanFinansialButton;
    private Button resumePauseButton;
    private Button restartPauseButton;
    private Button homePauseButton;
    private Button questToggleButton;
    private Button closeQuestButton;
    private Button inventoryToggleButton;
    private Button closeInventoryButton;
    private Button previousInventoryButton;
    private Button nextInventoryButton;
    private Button debugSetDayButton;
    private Button asuransiButton;
    private Button bahanMasakanButton;
    private Button initialBahanNextButton;
    private Button targetKebutuhanResetButton;
    private Button targetKebutuhanNextButton;
    private VisualElement rootElement;
    private VisualElement mainContainer;
    private VisualElement backgroundTransitionOverlay;
    private VisualElement playerContainer;
    private VisualElement textContainer;
    private VisualElement targetKebutuhanOverlay;
    private VisualElement targetKebutuhanGrid;
    private VisualElement initialBahanGrid;
    private VisualElement risikoSetupContent;
    private VisualElement risikoCoinDecisionContent;
    private VisualElement risikoCoinInputContainer;
    private VisualElement risikoDampakInputContainer;
    private Toggle risikoInvestasiEmasToggle;
    private Toggle risikoCoinToggle;
    private Toggle risikoDampakSepekanToggle;
    private Toggle risikoHargaBahanToggle;
    private Toggle risikoHargaKebutuhanToggle;
    private Toggle risikoUseAsuransiToggle;
    private Toggle risikoBayarBankToggle;
    private Toggle risikoTidakCukupToggle;
    private VisualElement questPanel;
    private VisualElement inventoryInputBlocker;
    private VisualElement inventoryPanel;
    private VisualElement inventoryList;
    private Label inventoryPageText;
    private VisualElement pauseInputBlocker;
    private VisualElement pausePanel;
    private Dictionary<string, VisualElement> choiceContainers;
    private List<Button> bahanChoiceButtons;
    private List<Button> kebutuhanChoiceButtons;
    private List<Button> jualMasakanChoiceButtons;
    private List<Button> tujuanFinansialChoiceButtons;
    private List<Button> targetKebutuhanChoiceButtons;
    private List<Button> initialBahanChoiceButtons;
    private List<string> specialButtons;
    private EventCallback<TransitionEndEvent> transitionCallback;
    private string selectedChoice;
    private int bahanPage;
    private int kebutuhanPage;
    private int jualMasakanPage;
    private int tujuanFinansialPage;
    private int inventoryPage;
    private Tween dialogTween;
    private string activeBackgroundClass = "background-city";
    private int backgroundTransitionVersion;
    private int ignoredDialogClickFrame = -1;
    private bool hasStartedOpeningChoiceFlow;
    private bool showOnlyAffordableTujuanFinansial;

    void Start()
    {
        SetPauseState(false);
        rootElement = GetComponent<UIDocument>().rootVisualElement;
        var root = rootElement;
        UIAspectRatioUtility.ApplyResponsiveScale(root, root.Q<VisualElement>("RootContainer"));

        mainContainer = root.Q<VisualElement>("MainContainer");
        backgroundTransitionOverlay = root.Q<VisualElement>("BackgroundTransitionOverlay");
        if (backgroundTransitionOverlay != null)
        {
            backgroundTransitionOverlay.pickingMode = PickingMode.Ignore;
        }

        nameTag = root.Q<Label>("NameTag");
        dialog = root.Q<Label>("Dialog");
        coinCounter = root.Q<Label>("CoinCounter");
        happinessCounter = root.Q<Label>("HappinessCounter");
        dayCounter = root.Q<Label>("DayCounter");
        weekCounter = root.Q<Label>("WeekCounter");
        dayNameCounter = root.Q<Label>("DayNameCounter");
        playerTurn = root.Q<Label>("PlayerTurn");
        savingText = root.Q<Label>("SavingText");
        savingCounter = root.Q<Label>("SavingCounter");
        jumatBerkahText = root.Q<Label>("JumatBerkahText");
        hargaBeliText = root.Q<Label>("KHargaBeliText");
        hargaEmasText = root.Q<Label>("HargaEmasText");
        jumlahEmasText = root.Q<Label>("JumlahEmasText");
        risikoCoinChangeText = root.Q<Label>("RisikoCoinChangeText");
        risikoHargaChangeText = root.Q<Label>("RisikoHargaChangeText");
        risikoWarningText = root.Q<Label>("RisikoWarningText");
        risikoDecisionPlayerText = root.Q<Label>("RisikoDecisionPlayerText");
        targetKebutuhanTitle = root.Q<Label>("TargetKebutuhanTitle");
        initialBahanTitle = root.Q<Label>("InitialBahanTitle");
        finalHappinessPlayerText = root.Q<Label>("FinalHappinessPlayerText");
        finalHappinessInputText = root.Q<Label>("FinalHappinessInputText");
        selesaiText = root.Q<Label>("SelesaiText");
        inventoryPlayerText = root.Q<Label>("InventoryPlayerText");
        tfConfirmText = root.Q<Label>("TFConfirmText");
        emasActionDecisionText = root.Q<Label>("EmasActionDecisionText");
        pauseToggleButton = root.Q<Button>("PauseToggleButton");
        backChoiceBMButton = root.Q<Button>("BackChoiceBMButton");
        previousBahanButton = root.Q<Button>("PreviousBahanButton");
        nextBahanButton = root.Q<Button>("NextBahanButton");
        backChoiceKButton = root.Q<Button>("BackChoiceKButton");
        previousKebutuhanButton = root.Q<Button>("PreviousKebutuhanButton");
        nextKebutuhanButton = root.Q<Button>("NextKebutuhanButton");
        backChoiceJMButton = root.Q<Button>("BackChoiceJMButton");
        previousJualMasakanButton = root.Q<Button>("PreviousJualMasakanButton");
        nextJualMasakanButton = root.Q<Button>("NextJualMasakanButton");
        backChoicePSButton = root.Q<Button>("BackChoicePSButton");
        backChoiceMenabungButton = root.Q<Button>("BackChoiceMenabungButton");
        backChoiceTFButton = root.Q<Button>("BackChoiceTFButton");
        previousTujuanFinansialButton = root.Q<Button>("PreviousTujuanFinansialButton");
        nextTujuanFinansialButton = root.Q<Button>("NextTujuanFinansialButton");
        resumePauseButton = root.Q<Button>("ResumePauseButton");
        restartPauseButton = root.Q<Button>("RestartPauseButton");
        homePauseButton = root.Q<Button>("HomePauseButton");
        questToggleButton = root.Q<Button>("QuestToggleButton");
        closeQuestButton = root.Q<Button>("CloseQuestButton");
        inventoryToggleButton = root.Q<Button>("InventoryToggleButton");
        closeInventoryButton = root.Q<Button>("CloseInventoryButton");
        previousInventoryButton = root.Q<Button>("PreviousInventoryButton");
        nextInventoryButton = root.Q<Button>("NextInventoryButton");
        debugSetDayButton = root.Q<Button>("DebugSetDayButton");
        asuransiButton = root.Q<Button>("Asuransi");
        bahanMasakanButton = root.Q<Button>("BahanMasakan");
        initialBahanNextButton = root.Q<Button>("InitialBahanNextButton");
        targetKebutuhanResetButton = root.Q<Button>("TargetKebutuhanResetButton");
        targetKebutuhanNextButton = root.Q<Button>("TargetKebutuhanNextButton");
        EnsureDebugTargetDay();
        if (debugSetDayButton != null)
        {
            debugSetDayButton.text = "Day " + debugTargetDay;
            debugSetDayButton.BringToFront();
        }

        playerContainer = root.Q<VisualElement>("PlayerContainer");
        textContainer = root.Q<VisualElement>("TextContainer");
        targetKebutuhanOverlay = root.Q<VisualElement>("TargetKebutuhanOverlay");
        targetKebutuhanGrid = root.Q<VisualElement>("TargetKebutuhanGrid");
        initialBahanGrid = root.Q<VisualElement>("InitialBahanGrid");
        BindRisikoKehidupanElements(root);
        questPanel = root.Q<VisualElement>("QuestPanel");
        inventoryInputBlocker = root.Q<VisualElement>("InventoryInputBlocker");
        inventoryPanel = root.Q<VisualElement>("InventoryPanel");
        inventoryList = root.Q<VisualElement>("InventoryList");
        inventoryPageText = root.Q<Label>("InventoryPageText");
        pauseInputBlocker = root.Q<VisualElement>("PauseInputBlocker");
        pausePanel = root.Q<VisualElement>("PausePanel");

        BuildBahanChoiceButtons(root.Q<VisualElement>("ChoiceBM"));
        BuildKebutuhanChoiceButtons(root.Q<VisualElement>("ChoiceK"));
        BuildJualMasakanChoiceButtons(root.Q<VisualElement>("ChoiceJM"));
        BuildTujuanFinansialChoiceButtons(root.Q<VisualElement>("ChoiceTF"));
        BuildInitialBahanChoiceButtons(initialBahanGrid);
        BuildTargetKebutuhanChoiceButtons(targetKebutuhanGrid);

        choiceContainers = new();
        specialButtons = new()
        {
            "MinButton",
            "MaxButton",
            "DecreaseButton",
            "IncreaseButton",
            "MinButtonJB",
            "MaxButtonJB",
            "DecreaseButtonJB",
            "IncreaseButtonJB",
            "MinButtonK",
            "MaxButtonK",
            "DecreaseButtonK",
            "IncreaseButtonK",
            "MinButtonHargaEmas",
            "MaxButtonHargaEmas",
            "DecreaseButtonHargaEmas",
            "IncreaseButtonHargaEmas",
            "MinButtonJumlahEmas",
            "MaxButtonJumlahEmas",
            "DecreaseButtonJumlahEmas",
            "IncreaseButtonJumlahEmas",
            "MinButtonFinalHappiness",
            "MaxButtonFinalHappiness",
            "DecreaseButtonFinalHappiness",
            "IncreaseButtonFinalHappiness",
            "DecreaseButtonRisikoCoin",
            "IncreaseButtonRisikoCoin",
            "DecreaseButtonRisikoHarga",
            "IncreaseButtonRisikoHarga",
            "NextButtonRisikoKehidupan",
        };

        string[] choiceNames = {
            "Choice1",
            "ChoiceBM",
            "ChoiceK",
            "ChoiceJM",
            "ChoiceTF",
            "ChoiceKL",
            "ChoicePS",
            "ChoiceHargaEmas",
            "ChoiceEmasAction",
            "ChoiceJumlahEmas",
            "ChoiceInitialBahan",
            "ChoiceTargetKebutuhan",
            "ChoiceRisikoKehidupan",
            "ChoiceMenabung",
            "ChoiceTFConfirm",
            "JumatBerkah",
            "ChoiceKJumlah",
            "ChoiceFinalHappiness",
            "PermainanSelesai"
        };

        foreach (var name in choiceNames)
        {
            RegisterChoiceGroup(name, root);
        }

        textContainer.style.display = DisplayStyle.None;

        playerContainer.RegisterCallback<TransitionEndEvent>(ShowTextContainer);
        pauseToggleButton.RegisterCallback<ClickEvent>(TogglePause);
        backChoiceBMButton.RegisterCallback<ClickEvent>(BackFromChoiceBM);
        previousBahanButton.RegisterCallback<ClickEvent>(ShowPreviousBahanPage);
        nextBahanButton.RegisterCallback<ClickEvent>(ShowNextBahanPage);
        backChoiceKButton.RegisterCallback<ClickEvent>(BackFromChoiceK);
        previousKebutuhanButton.RegisterCallback<ClickEvent>(ShowPreviousKebutuhanPage);
        nextKebutuhanButton.RegisterCallback<ClickEvent>(ShowNextKebutuhanPage);
        backChoiceJMButton.RegisterCallback<ClickEvent>(BackFromChoiceJM);
        previousJualMasakanButton.RegisterCallback<ClickEvent>(ShowPreviousJualMasakanPage);
        nextJualMasakanButton.RegisterCallback<ClickEvent>(ShowNextJualMasakanPage);
        backChoicePSButton.RegisterCallback<ClickEvent>(BackFromChoicePS);
        backChoiceMenabungButton.RegisterCallback<ClickEvent>(BackFromChoiceMenabung);
        backChoiceTFButton.RegisterCallback<ClickEvent>(BackFromChoiceTF);
        previousTujuanFinansialButton.RegisterCallback<ClickEvent>(ShowPreviousTujuanFinansialPage);
        nextTujuanFinansialButton.RegisterCallback<ClickEvent>(ShowNextTujuanFinansialPage);
        resumePauseButton.RegisterCallback<ClickEvent>(ResumePause);
        restartPauseButton.RegisterCallback<ClickEvent>(RestartGame);
        homePauseButton.RegisterCallback<ClickEvent>(GoToHome);
        questToggleButton.RegisterCallback<ClickEvent>(ToggleQuestPanel);
        closeQuestButton.RegisterCallback<ClickEvent>(HideQuestPanel);
        inventoryToggleButton.RegisterCallback<ClickEvent>(ToggleInventoryPanel);
        closeInventoryButton.RegisterCallback<ClickEvent>(HideInventoryPanel);
        previousInventoryButton.RegisterCallback<ClickEvent>(ShowPreviousInventoryPage);
        nextInventoryButton.RegisterCallback<ClickEvent>(ShowNextInventoryPage);
        debugSetDayButton.RegisterCallback<ClickEvent>(SetDebugTargetDay);
        RegisterDialogClickBlockers(root);
        AudioController.Instance.RegisterButtonSounds(root);

        UpdateNameTag();
        UpdateDay(GameState.Instance.day);
        UpdatePlayerTurn(GameState.Instance.turn);
        UpdatePlayerStats();
        UpdateBahanPage();
        UpdateKebutuhanPage();
        UpdateJualMasakanPage();
        UpdateTujuanFinansialPage();
        SetChoiceBackground("ChoiceInitialBahan");

        Invoke("ShowInitialBahanChoice", .1f);
    }

    void OnDestroy()
    {
        SetPauseState(false);
    }

    void RegisterChoiceGroup(string containerName, VisualElement root)
    {
        var container = root.Q<VisualElement>(containerName);
        choiceContainers[containerName] = container;
        var buttons = container.Query<Button>().ToList();

        foreach (var button in buttons)
        {
            if (button == backChoiceBMButton || button == previousBahanButton || button == nextBahanButton
                || button == backChoiceKButton || button == previousKebutuhanButton || button == nextKebutuhanButton
                || button == backChoiceJMButton || button == previousJualMasakanButton || button == nextJualMasakanButton
                || button == backChoicePSButton
                || button == backChoiceMenabungButton
                || button == backChoiceTFButton || button == previousTujuanFinansialButton || button == nextTujuanFinansialButton)
            {
                continue;
            }

            if (containerName == "ChoiceTargetKebutuhan")
            {
                button.RegisterCallback<ClickEvent>(evt => OnChoiceTFClicked(evt, container));
                continue;
            }

            if (containerName == "ChoiceInitialBahan")
            {
                button.RegisterCallback<ClickEvent>(evt => OnChoiceTFClicked(evt, container));
                continue;
            }

            if (specialButtons.Contains(button.name))
            {
                button.RegisterCallback<ClickEvent>(evt => OnChoiceTFClicked(evt, container));
                continue;
            }

            button.RegisterCallback<ClickEvent>(evt => OnChoiceClicked(evt, container));
        }
    }

    private void RegisterDialogClickBlockers(VisualElement root)
    {
        var buttons = root.Query<Button>().ToList();
        foreach (var button in buttons)
        {
            button.RegisterCallback<PointerDownEvent>(IgnoreDialogClickForButton, TrickleDown.TrickleDown);
        }
    }

    private void IgnoreDialogClickForButton(PointerDownEvent evt)
    {
        ignoredDialogClickFrame = Time.frameCount;
    }

    private void SetDebugTargetDay(ClickEvent evt)
    {
        if (GameState.Instance == null)
        {
            return;
        }

        EnsureDebugTargetDay();
        GameState.Instance.SetDay(debugTargetDay);
        UpdateDay(GameState.Instance.day);
        UpdatePlayerTurn(GameState.Instance.turn);
        UpdatePlayerStats();
        HideDialogContainer();
        HideAllChoiceContainers();

        if (choiceController != null)
        {
            choiceController.ShowCurrentDayChoice();
        }
        else
        {
            ShowChoice("Choice1");
        }
    }

    private void EnsureDebugTargetDay()
    {
        if (debugTargetDay < 1)
        {
            debugTargetDay = DefaultDebugTargetDay;
        }
    }

    private void HideAllChoiceContainers()
    {
        if (choiceContainers == null)
        {
            return;
        }

        foreach (VisualElement container in choiceContainers.Values)
        {
            container.RemoveFromClassList("show-choice");
        }
    }
}
