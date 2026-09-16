using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public partial class UIManager : MonoBehaviour
{
    private const int HomeSceneBuildIndex = 0;

    private Button addPlayerButton;
    private Button playButton;
    private Button editButton;
    private Button backEditButton;
    private Button backEditPemilihanNarasiButton;
    private Button backEditNarasiButton;
    private Button editNarasiButton;
    private Button editAssetButton;
    private Button editNarasiPackNewButton;
    private Button editNarasiPackDeleteButton;
    private Button nextEditPemilihanNarasiButton;
    private Button editNarasiNewButton;
    private Button editNarasiDeleteButton;
    private Button editNarasiAddLineButton;
    private Button editNarasiResetButton;
    private Button editNarasiSaveButton;
    private Button exitButton;
    private Button backAddPlayer;
    private Button loginButton;
    private Button RegisterButton;
    private Button play2Button;
    private Button backPlayButton;
    private Button signOutButton;
    private Button addPlayerRegisterButton;
    private Button backSessionSetupButton;
    private Button nextSessionSetupButton;
    private Button errorPopupOkButton;

    private TextField usernameInput;
    private TextField passwordInput;
    private TextField addPlayerUsernameInput;
    private TextField addPlayerPasswordInput;
    private TextField sessionNameInput;
    private TextField rulesetSearchInput;
    private TextField editNarasiIdInput;
    private TextField editNarasiActivePackNameInput;
    private IntegerField editNarasiAksiValueInput;
    private TextField editNarasiNpcNameInput;
    private DropdownField editNarasiActionTypeInput;
    private DropdownField editNarasiNpcSpriteInput;
    private TextField editNarasiRequiredBahanInput;
    private TextField editNarasiRequiredKebutuhanInput;
    private TextField editNarasiRequiredTujuanFinansialInput;
    private TextField editNarasiRequiredMasakanInput;
    private Button editNarasiLineRemove1;
    private Button editNarasiLineRemove2;
    private TextField editNarasiLineText1;
    private TextField editNarasiLineText2;
    private VisualElement editNarasiLinesSection;
    private IntegerField editNarasiMinCoinInput;
    private IntegerField editNarasiMinHappinessInput;
    private IntegerField editNarasiMinSavingInput;
    private IntegerField editNarasiMinGoldInput;
    private IntegerField editNarasiLoanCardInput;
    private IntegerField editNarasiWeekInput;
    private IntegerField editNarasiDayInput;
    private Toggle editNarasiHasInsuranceToggle;
    private VisualElement editNarasiNpcSpritePreview;
    private DropdownField rulesetModeDropdown;
    private DropdownField rulesetDropdown;
    private DropdownField narasiPackDropdown;
    private DropdownField questPackDropdown;
    private DropdownField editNarasiQuestEffectDropdown;
    private DropdownField editNarasiQuestEffectStateDropdown;
    private DropdownField editNarasiQuestPrereqDropdown;
    private DropdownField editNarasiQuestPrereqStateDropdown;
    private DropdownField editNarasiPackDropdown;
    private DropdownField editNarasiDialogDropdown;
    private DropdownField editNarasiLineSpeaker1;
    private DropdownField editNarasiLineSpeaker2;
    private readonly List<Button> editNarasiLineRemoveButtons = new List<Button>();
    private readonly HashSet<Button> editNarasiLineRemoveButtonsRegistered = new HashSet<Button>();
    private readonly List<DropdownField> editNarasiLineSpeakerInputs = new List<DropdownField>();
    private readonly List<TextField> editNarasiLineTextInputs = new List<TextField>();
    private bool editNarasiLineInputsInitialized;
    private DropdownField playerCountDropdown;
    private TextField[] playerNameInputs;
    private VisualElement[] playerNameGroups;
    private VisualElement playerSuggestionsOverlay;
    private VisualElement playerSuggestionsPanel;
    private Button[] playerSuggestionButtons;
    private VisualElement rulesetSuggestionsContainer;
    private Button[] rulesetSuggestionButtons;
    private readonly List<NarafinRulesetSummary> currentRulesetOptions = new List<NarafinRulesetSummary>();
    private readonly List<NarasiPackData> currentNarasiPackOptions = new List<NarasiPackData>();
    private readonly List<QuestPackData> currentQuestPackOptions = new List<QuestPackData>();
    private Label playValidationText;
    private Label addPlayerValidationText;
    private Label sessionSetupValidationText;
    private Label dialogKarakterStatusText;
    private Label authLoadingText;
    private Label playLoadingText;
    private Label addPlayerLoadingText;
    private Label errorPopupTitle;
    private Label errorPopupMessage;

    private VisualElement addPlayerContainer;
    private VisualElement editContainer;
    private VisualElement editPemilihanNarasiContainer;
    private VisualElement editNarasiContainer;
    private VisualElement loginContainer;
    private VisualElement sessionSetupContainer;
    private VisualElement playContainer;
    private VisualElement authLoadingOverlay;
    private VisualElement authLoadingSpinner;
    private Coroutine authLoadingSpinnerCoroutine;
    private VisualElement addPlayerLoadingOverlay;
    private VisualElement addPlayerLoadingSpinner;
    private Coroutine addPlayerLoadingSpinnerCoroutine;
    private VisualElement playLoadingOverlay;
    private VisualElement playLoadingSpinner;
    private Coroutine playLoadingSpinnerCoroutine;
    private VisualElement errorPopupOverlay;
    private VisualElement errorPopupCard;
    private Coroutine errorPopupHideCoroutine;
    private bool isPlayLoading;
    private VisualElement rootElement;
    private bool isHomeUiReady;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // GameObject ini satu paket dengan LoginManager dan ChangeScene yang memakai DontDestroyOnLoad,
    // jadi UI Home ikut bertahan lintas scene: disembunyikan saat di scene lain dan dipulihkan saat kembali.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isHomeUiReady)
        {
            return;
        }

        bool isHomeScene = scene.buildIndex == HomeSceneBuildIndex;
        if (rootElement != null)
        {
            rootElement.style.display = isHomeScene ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (!isHomeScene)
        {
            return;
        }

        // Tanpa ini, overlay "Memuat permainan..." dan tombol yang dinonaktifkan sebelum pindah scene
        // masih tertinggal saat pemain kembali ke Home.
        ResetPlayLoadingState();
        EndAuthLoading();
        EndAddPlayerLoading();
        ResetHomeNavigationState();

        if (LoginManager.Instance != null && !LoginManager.Instance.IsSignedIn())
        {
            loginContainer?.AddToClassList("show-login");
        }
    }

    async void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        rootElement = root;
        root.style.display = DisplayStyle.Flex;
        UIAspectRatioUtility.ApplyResponsiveScale(root, root.Q<VisualElement>("MainContainer"));

        BindElements(root);
        ResetHomeNavigationState();
        ResetPlayLoadingState();
        isHomeUiReady = true;
        RegisterCallbacks();
        SetupMobileKeyboardFocusHandlers();
        AudioController.Instance.RegisterButtonSounds(root);
        SetupPlayerCountDropdown();

        // Display DialogKarakter loading status
        if (DataManager.Instance != null)
        {
            dialogKarakterStatusText.text = DataManager.Instance.DialogKarakterLoadStatus;
            if (!DataManager.Instance.IsDialogKarakterLoaded)
            {
                dialogKarakterStatusText.style.color = new Color(255, 100, 100);  // Red for error
            }
            else
            {
                dialogKarakterStatusText.style.color = new Color(100, 200, 100);  // Green for success
            }
        }

        // LoginManager hidup di GameObject terpisah yang bertahan lintas scene; tanpa itu Home tidak bisa dipakai.
        if (LoginManager.Instance == null)
        {
            Debug.LogError("LoginManager tidak ditemukan. Pastikan komponennya ada di scene Home.");
            loginContainer?.AddToClassList("show-login");
            return;
        }

        await LoginManager.Instance.InitializeServicesAsync();

        if (!LoginManager.Instance.IsSignedIn())
        {
            loginContainer.AddToClassList("show-login");
        }
    }

    private void BindElements(VisualElement root)
    {
        addPlayerButton = root.Q<Button>("AddPlayerButton");
        playButton = root.Q<Button>("PlayButton");
        editButton = root.Q<Button>("EditButton");
        backEditButton = root.Q<Button>("BackEditButton");
        backEditPemilihanNarasiButton = root.Q<Button>("BackEditPemilihanNarasiButton");
        backEditNarasiButton = root.Q<Button>("BackEditNarasiButton");
        editNarasiButton = root.Q<Button>("EditNarasiButton");
        editAssetButton = root.Q<Button>("EditAssetButton");
        editNarasiPackNewButton = root.Q<Button>("EditNarasiPackNewButton");
        editNarasiPackDeleteButton = root.Q<Button>("EditNarasiPackDeleteButton");
        nextEditPemilihanNarasiButton = root.Q<Button>("NextEditPemilihanNarasiButton");
        editNarasiNewButton = root.Q<Button>("EditNarasiNewButton");
        editNarasiDeleteButton = root.Q<Button>("EditNarasiDeleteButton");
        editNarasiAddLineButton = root.Q<Button>("EditNarasiAddLineButton");
        editNarasiResetButton = root.Q<Button>("EditNarasiResetButton");
        editNarasiSaveButton = root.Q<Button>("EditNarasiSaveButton");
        exitButton = root.Q<Button>("ExitButton");
        backAddPlayer = root.Q<Button>("BackAddPlayerButton");
        loginButton = root.Q<Button>("LoginButton");
        RegisterButton = root.Q<Button>("RegisterButton");
        play2Button = root.Q<Button>("Play2Button");
        backPlayButton = root.Q<Button>("BackPlayButton");
        signOutButton = root.Q<Button>("SignOutButton");
        addPlayerRegisterButton = root.Q<Button>("AddPlayerRegisterButton");
        backSessionSetupButton = root.Q<Button>("BackSessionSetupButton");
        nextSessionSetupButton = root.Q<Button>("NextSessionSetupButton");
        errorPopupOkButton = root.Q<Button>("ErrorPopupOkButton");

        usernameInput = root.Q<TextField>("UsernameInput");
        passwordInput = root.Q<TextField>("PasswordInput");
        addPlayerUsernameInput = root.Q<TextField>("AddPlayerUsernameInput");
        addPlayerPasswordInput = root.Q<TextField>("AddPlayerPasswordInput");
        sessionNameInput = root.Q<TextField>("SessionNameInput");
        rulesetSearchInput = root.Q<TextField>("RulesetSearchInput");
        editNarasiIdInput = root.Q<TextField>("EditNarasiIdInput");
        editNarasiActivePackNameInput = root.Q<TextField>("EditNarasiActivePackNameInput");
        editNarasiAksiValueInput = root.Q<IntegerField>("EditNarasiAksiValueInput");
        editNarasiNpcNameInput = root.Q<TextField>("EditNarasiNpcNameInput");
        editNarasiActionTypeInput = root.Q<DropdownField>("EditNarasiActionTypeInput");
        editNarasiNpcSpriteInput = root.Q<DropdownField>("EditNarasiNpcSpriteInput");
        editNarasiRequiredBahanInput = root.Q<TextField>("EditNarasiRequiredBahanInput");
        editNarasiRequiredKebutuhanInput = root.Q<TextField>("EditNarasiRequiredKebutuhanInput");
        editNarasiRequiredTujuanFinansialInput = root.Q<TextField>("EditNarasiRequiredTujuanFinansialInput");
        editNarasiRequiredMasakanInput = root.Q<TextField>("EditNarasiRequiredMasakanInput");
        editNarasiLineRemove1 = root.Q<Button>("EditNarasiLineRemove1");
        editNarasiLineRemove2 = root.Q<Button>("EditNarasiLineRemove2");
        editNarasiLineText1 = root.Q<TextField>("EditNarasiLineText1");
        editNarasiLineText2 = root.Q<TextField>("EditNarasiLineText2");
        editNarasiLinesSection = root.Q<VisualElement>("EditNarasiLinesSection");
        editNarasiMinCoinInput = root.Q<IntegerField>("EditNarasiMinCoinInput");
        editNarasiMinHappinessInput = root.Q<IntegerField>("EditNarasiMinHappinessInput");
        editNarasiMinSavingInput = root.Q<IntegerField>("EditNarasiMinSavingInput");
        editNarasiMinGoldInput = root.Q<IntegerField>("EditNarasiMinGoldInput");
        editNarasiLoanCardInput = root.Q<IntegerField>("EditNarasiLoanCardInput");
        editNarasiWeekInput = root.Q<IntegerField>("EditNarasiWeekInput");
        editNarasiDayInput = root.Q<IntegerField>("EditNarasiDayInput");
        editNarasiHasInsuranceToggle = root.Q<Toggle>("EditNarasiHasInsuranceToggle");
        editNarasiNpcSpritePreview = root.Q<VisualElement>("EditNarasiNpcSpritePreview");
        rulesetModeDropdown = root.Q<DropdownField>("RulesetModeDropdown");
        rulesetDropdown = root.Q<DropdownField>("RulesetDropdown");
        narasiPackDropdown = root.Q<DropdownField>("NarasiPackDropdown");
        questPackDropdown = root.Q<DropdownField>("QuestPackDropdown");
        editNarasiQuestEffectDropdown = root.Q<DropdownField>("EditNarasiQuestEffectDropdown");
        editNarasiQuestEffectStateDropdown = root.Q<DropdownField>("EditNarasiQuestEffectStateDropdown");
        editNarasiQuestPrereqDropdown = root.Q<DropdownField>("EditNarasiQuestPrereqDropdown");
        editNarasiQuestPrereqStateDropdown = root.Q<DropdownField>("EditNarasiQuestPrereqStateDropdown");
        BindEditQuestElements(root);
        editNarasiPackDropdown = root.Q<DropdownField>("EditNarasiPackDropdown");
        editNarasiDialogDropdown = root.Q<DropdownField>("EditNarasiDialogDropdown");
        editNarasiLineSpeaker1 = root.Q<DropdownField>("EditNarasiLineSpeaker1");
        editNarasiLineSpeaker2 = root.Q<DropdownField>("EditNarasiLineSpeaker2");
        InitializeEditNarasiLineInputs();
        playerCountDropdown = root.Q<DropdownField>("PlayerCountDropdown");
        playerNameInputs = new TextField[]
        {
            root.Q<TextField>("PlayerNameInput1"),
            root.Q<TextField>("PlayerNameInput2"),
            root.Q<TextField>("PlayerNameInput3"),
            root.Q<TextField>("PlayerNameInput4")
        };
        playerNameGroups = new VisualElement[]
        {
            root.Q<VisualElement>("PlayerNameGroup1"),
            root.Q<VisualElement>("PlayerNameGroup2"),
            root.Q<VisualElement>("PlayerNameGroup3"),
            root.Q<VisualElement>("PlayerNameGroup4")
        };
        playerSuggestionsOverlay = root.Q<VisualElement>("PlayerNameSuggestionsOverlay");
        playerSuggestionsPanel = root.Q<VisualElement>("PlayerNameSuggestionsPanel");
        playerSuggestionButtons = new Button[]
        {
            root.Q<Button>("PlayerSuggestionButton1"),
            root.Q<Button>("PlayerSuggestionButton2"),
            root.Q<Button>("PlayerSuggestionButton3"),
            root.Q<Button>("PlayerSuggestionButton4")
        };
        rulesetSuggestionsContainer = root.Q<VisualElement>("RulesetSuggestionsContainer");
        rulesetSuggestionButtons = new Button[]
        {
            root.Q<Button>("RulesetSuggestionButton1"),
            root.Q<Button>("RulesetSuggestionButton2"),
            root.Q<Button>("RulesetSuggestionButton3"),
            root.Q<Button>("RulesetSuggestionButton4")
        };
        playValidationText = root.Q<Label>("PlayValidationText");
        addPlayerValidationText = root.Q<Label>("AddPlayerValidationText");
        sessionSetupValidationText = root.Q<Label>("SessionSetupValidationText");
        dialogKarakterStatusText = root.Q<Label>("DialogKarakterStatusText");
        authLoadingText = root.Q<Label>("AuthLoadingText");
        playLoadingText = root.Q<Label>("PlayLoadingText");
        addPlayerLoadingText = root.Q<Label>("AddPlayerLoadingText");
        errorPopupTitle = root.Q<Label>("ErrorPopupTitle");
        errorPopupMessage = root.Q<Label>("ErrorPopupMessage");

        addPlayerContainer = root.Q<VisualElement>("AddPlayerContainer");
        editContainer = root.Q<VisualElement>("EditContainer");
        editPemilihanNarasiContainer = root.Q<VisualElement>("EditPemilihanNarasiContainer");
        editNarasiContainer = root.Q<VisualElement>("EditNarasiContainer");
        loginContainer = root.Q<VisualElement>("LoginContainer");
        sessionSetupContainer = root.Q<VisualElement>("SessionSetupContainer");
        playContainer = root.Q<VisualElement>("PlayContainer");
        authLoadingOverlay = root.Q<VisualElement>("AuthLoadingOverlay");
        authLoadingSpinner = root.Q<VisualElement>("AuthLoadingSpinner");
        addPlayerLoadingOverlay = root.Q<VisualElement>("AddPlayerLoadingOverlay");
        addPlayerLoadingSpinner = root.Q<VisualElement>("AddPlayerLoadingSpinner");
        playLoadingOverlay = root.Q<VisualElement>("PlayLoadingOverlay");
        playLoadingSpinner = root.Q<VisualElement>("PlayLoadingSpinner");
        errorPopupOverlay = root.Q<VisualElement>("ErrorPopupOverlay");
        errorPopupCard = root.Q<VisualElement>("ErrorPopupCard");
    }

    private void RegisterCallbacks()
    {
        addPlayerButton?.RegisterCallback<ClickEvent>(OnAddPlayerClicked);
        playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        editButton?.RegisterCallback<ClickEvent>(OnEditClicked);
        backEditButton?.RegisterCallback<ClickEvent>(OnBackEditClicked);
        backEditPemilihanNarasiButton?.RegisterCallback<ClickEvent>(OnBackEditPemilihanNarasiClicked);
        backEditNarasiButton?.RegisterCallback<ClickEvent>(OnBackEditNarasiClicked);
        editNarasiButton?.RegisterCallback<ClickEvent>(OnEditNarasiClicked);
        editAssetButton?.RegisterCallback<ClickEvent>(OnEditAssetClicked);
        editNarasiPackNewButton?.RegisterCallback<ClickEvent>(OnEditNarasiPackNewClicked);
        editNarasiPackDeleteButton?.RegisterCallback<ClickEvent>(OnEditNarasiPackDeleteClicked);
        nextEditPemilihanNarasiButton?.RegisterCallback<ClickEvent>(OnNextEditPemilihanNarasiClicked);
        editNarasiNewButton?.RegisterCallback<ClickEvent>(OnEditNarasiNewClicked);
        editNarasiDeleteButton?.RegisterCallback<ClickEvent>(OnEditNarasiDeleteClicked);
        editNarasiAddLineButton?.RegisterCallback<ClickEvent>(OnEditNarasiAddLineClicked);
        editNarasiResetButton?.RegisterCallback<ClickEvent>(OnEditNarasiResetClicked);
        editNarasiSaveButton?.RegisterCallback<ClickEvent>(OnEditNarasiSaveClicked);
        RegisterEditQuestCallbacks();
        exitButton.RegisterCallback<ClickEvent>(OnExitClicked);
        backAddPlayer.RegisterCallback<ClickEvent>(evt => OnBackClicked(evt, addPlayerContainer, "show-add-player"));
        loginButton.RegisterCallback<ClickEvent>(OnLoginClicked);
        RegisterButton.RegisterCallback<ClickEvent>(OnRegisterClicked);
        addPlayerRegisterButton?.RegisterCallback<ClickEvent>(OnAddPlayerRegisterClicked);
        backSessionSetupButton?.RegisterCallback<ClickEvent>(OnBackSessionSetupClicked);
        nextSessionSetupButton?.RegisterCallback<ClickEvent>(OnNextSessionSetupClicked);
        play2Button.RegisterCallback<ClickEvent>(OnPlay2Clicked);
        backPlayButton.RegisterCallback<ClickEvent>(OnBackPlayClicked);
        signOutButton.RegisterCallback<ClickEvent>(OnSignOutClicked);
        errorPopupOkButton?.RegisterCallback<ClickEvent>(OnErrorPopupOkClicked);

        rulesetModeDropdown?.RegisterValueChangedCallback(OnRulesetModeChanged);
        rulesetDropdown?.RegisterValueChangedCallback(OnRulesetDropdownChanged);
        editNarasiDialogDropdown?.RegisterValueChangedCallback(OnEditNarasiDialogChanged);
        editNarasiNpcSpriteInput?.RegisterValueChangedCallback(OnEditNarasiNpcSpriteChanged);
        RegisterPlayerNameCallbacks();
        if (playerSuggestionButtons != null)
        {
            for (int i = 0; i < playerSuggestionButtons.Length; i++)
            {
                int suggestionIndex = i;
                playerSuggestionButtons[i]?.RegisterCallback<ClickEvent>(evt => OnPlayerSuggestionClicked(suggestionIndex));
            }
        }

        addPlayerContainer.RegisterCallback<TransitionEndEvent>(OnAddPlayerTransitionEnd);
        loginContainer.RegisterCallback<TransitionEndEvent>(OnLoginTransitionEnd);
    }

    private void SetupMobileKeyboardFocusHandlers()
    {
        ConfigureMobileKeyboardFocus(rulesetSearchInput);

        if (playerNameInputs == null)
        {
            return;
        }

        for (int i = 0; i < playerNameInputs.Length; i++)
        {
            ConfigureMobileKeyboardFocus(playerNameInputs[i]);
        }
    }

    private void ConfigureMobileKeyboardFocus(TextField textField)
    {
        if (textField == null)
        {
            return;
        }

        textField.focusable = true;
        textField.selectAllOnFocus = true;
        textField.RegisterCallback<PointerDownEvent>(evt =>
        {
            textField.Focus();
        });
    }
}






