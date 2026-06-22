using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager : MonoBehaviour
{
    private Button profileButton;
    private Button questButton;
    private Button playButton;
    private Button editRulesetButton;
    private Button exitButton;
    private Button backQuest;
    private Button loginButton;
    private Button RegisterButton;
    private Button play2Button;
    private Button backPlayButton;
    private Button signOutButton;

    private TextField usernameInput;
    private TextField passwordInput;
    private DropdownField playerCountDropdown;
    private TextField[] playerNameInputs;
    private VisualElement[] playerNameGroups;
    private Label playValidationText;
    private Label authValidationText;
    private Label dialogKarakterStatusText;

    private VisualElement questContainer;
    private VisualElement loginContainer;
    private VisualElement playContainer;
    private VisualElement scrim;
    private VisualElement profileContainer;
    private VisualElement profilePage;

    async void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        UIAspectRatioUtility.ApplyResponsiveScale(root, root.Q<VisualElement>("MainContainer"));

        BindElements(root);
        RegisterCallbacks();
        AudioController.Instance.RegisterButtonSounds(root);
        SetupPlayerCountDropdown();

        profileContainer.style.display = DisplayStyle.None;

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

        await LoginManager.Instance.InitializeServicesAsync();

        if (!LoginManager.Instance.IsSignedIn())
        {
            loginContainer.AddToClassList("show-login");
        }
    }

    private void BindElements(VisualElement root)
    {
        profileButton = root.Q<Button>("ProfileButton");
        questButton = root.Q<Button>("QuestButton");
        playButton = root.Q<Button>("PlayButton");
        editRulesetButton = root.Q<Button>("EditRulesetButton");
        exitButton = root.Q<Button>("ExitButton");
        backQuest = root.Q<Button>("BackQuestButton");
        loginButton = root.Q<Button>("LoginButton");
        RegisterButton = root.Q<Button>("RegisterButton");
        play2Button = root.Q<Button>("Play2Button");
        backPlayButton = root.Q<Button>("BackPlayButton");
        signOutButton = root.Q<Button>("SignOutButton");

        usernameInput = root.Q<TextField>("UsernameInput");
        passwordInput = root.Q<TextField>("PasswordInput");
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
        playValidationText = root.Q<Label>("PlayValidationText");
        authValidationText = root.Q<Label>("AuthValidationText");
        dialogKarakterStatusText = root.Q<Label>("DialogKarakterStatusText");

        questContainer = root.Q<VisualElement>("QuestContainer");
        loginContainer = root.Q<VisualElement>("LoginContainer");
        playContainer = root.Q<VisualElement>("PlayContainer");
        scrim = root.Q<VisualElement>("Scrim");
        profileContainer = root.Q<VisualElement>("ProfileContainer");
        profilePage = root.Q<VisualElement>("ProfilePage");
    }

    private void RegisterCallbacks()
    {
        profileButton.RegisterCallback<ClickEvent>(OnProfileClicked);
        questButton?.RegisterCallback<ClickEvent>(OnQuestClicked);
        playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        editRulesetButton.RegisterCallback<ClickEvent>(OnEditRulesetClicked);
        exitButton.RegisterCallback<ClickEvent>(OnExitClicked);
        backQuest.RegisterCallback<ClickEvent>(evt => OnBackClicked(evt, questContainer, "show-quest"));
        loginButton.RegisterCallback<ClickEvent>(OnLoginClicked);
        RegisterButton.RegisterCallback<ClickEvent>(OnRegisterClicked);
        play2Button.RegisterCallback<ClickEvent>(OnPlay2Clicked);
        backPlayButton.RegisterCallback<ClickEvent>(evt => OnBackClicked(evt, playContainer, "show-play"));
        signOutButton.RegisterCallback<ClickEvent>(OnSignOutClicked);
        scrim.RegisterCallback<ClickEvent>(OnScrimClicked);

        questContainer.RegisterCallback<TransitionEndEvent>(OnQuestTransitionEnd);
        loginContainer.RegisterCallback<TransitionEndEvent>(OnLoginTransitionEnd);
    }
}
