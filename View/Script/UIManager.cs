using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
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
    private TextField nameInput;
    private TextField playerCountInput;
    private Label playValidationText;

    private VisualElement questContainer;
    private VisualElement loginContainer;
    private VisualElement playContainer;
    private VisualElement scrim;
    private VisualElement profileContainer;
    private VisualElement profilePage;

    // Start is called before the first frame update
    async void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

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
        nameInput = root.Q<TextField>("NameInput");
        playerCountInput = root.Q<TextField>("PlayerCountInput");
        playValidationText = root.Q<Label>("PlayValidationText");

        questContainer = root.Q<VisualElement>("QuestContainer");
        loginContainer = root.Q<VisualElement>("LoginContainer");
        playContainer = root.Q<VisualElement>("PlayContainer");
        scrim = root.Q<VisualElement>("Scrim");
        profileContainer = root.Q<VisualElement>("ProfileContainer");
        profilePage = root.Q<VisualElement>("ProfilePage");

        profileButton.RegisterCallback<ClickEvent>(OnProfileClicked);
        questButton.RegisterCallback<ClickEvent>(OnQuestClicked);
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

        profileContainer.style.display = DisplayStyle.None;

        // await LoginManager.Instance.InitializeServicesAsync();

        // if (!LoginManager.Instance.IsSignedIn())
        // {
        //     loginContainer.AddToClassList("show-login");
        // }     
    }

    private void OnProfileClicked(ClickEvent evt)
    {
        Debug.Log("Profile button clicked!");
        profileContainer.style.display = DisplayStyle.Flex;
        profilePage.AddToClassList("show-profile");
        scrim.AddToClassList("show-scrim");
    }

    private void OnScrimClicked(ClickEvent evt)
    {
        Debug.Log("Scrim clicked!");
        profilePage.RemoveFromClassList("show-profile");
        scrim.RemoveFromClassList("show-scrim");
        profileContainer.style.display = DisplayStyle.None;
    }

    private void OnQuestClicked(ClickEvent evt)
    {
        Debug.Log("Quest button clicked!");
        questContainer.style.display = DisplayStyle.Flex;
        questContainer.AddToClassList("show-quest");
    }

    private void OnPlayClicked(ClickEvent evt)
    {
        Debug.Log("Play button clicked!");
        playContainer.AddToClassList("show-play");
    }

    private void OnPlay2Clicked(ClickEvent evt)
    {
        Debug.Log("Play2 button clicked!");
        if (nameInput.value == string.Empty)
        {
            Debug.Log("Name is empty.");
            playValidationText.text = "Nama tidak boleh kosong.";
            return;
        }

        if (!int.TryParse(playerCountInput.value, out int playerCount) || playerCount < 3 || playerCount > 4)
        {
            Debug.Log("Player count must be 3 or 4.");
            playValidationText.text = "Jumlah pemain harus 3 atau 4.";
            return;
        }

        playValidationText.text = string.Empty;
        PlayerPrefs.SetInt("PlayerCount", playerCount);
        PlayerPrefs.Save();
        ChangeScene.Instance.ChangeToScene(1);
    }

    private void OnEditRulesetClicked(ClickEvent evt)
    {
        Debug.Log("Edit Ruleset button clicked!");
    }

    private void OnExitClicked(ClickEvent evt)
    {
        Debug.Log("Exit button clicked!");
        Application.Quit();
        // LoginManager.Instance.SignOut();
        // loginContainer.AddToClassList("show-login");
    }

    private void OnBackClicked(ClickEvent evt, VisualElement container, string className)
    {
        Debug.Log("Back button clicked!");
        container.RemoveFromClassList(className);
    }

    private async void OnLoginClicked(ClickEvent evt)
    {
        Debug.Log("Login button clicked!");

        RegisterButton.SetEnabled(false);
        loginButton.SetEnabled(false);
        bool success = await LoginManager.Instance.SignIn(usernameInput.value, passwordInput.value);
        RegisterButton.SetEnabled(true);
        loginButton.SetEnabled(true);

        if (success)
        {
            loginContainer.RemoveFromClassList("show-login");
            usernameInput.value = string.Empty;
            passwordInput.value = string.Empty;
        }
        else
        {
            Debug.Log("Login failed. Please check your credentials.");
        }
    }

    private async void OnRegisterClicked(ClickEvent evt)
    {
        Debug.Log("Register button clicked!");

        RegisterButton.SetEnabled(false);
        loginButton.SetEnabled(false);
        bool success = await LoginManager.Instance.SignUp(usernameInput.value, passwordInput.value);
        RegisterButton.SetEnabled(true);
        loginButton.SetEnabled(true);

        if (!success)        {
            Debug.Log("Registration failed. Please check your input.");
            return;
        }
        usernameInput.value = string.Empty;
        passwordInput.value = string.Empty;
    }

    private void OnSignOutClicked(ClickEvent evt)
    {
        Debug.Log("Sign Out button clicked!");
        LoginManager.Instance.SignOut();
        profilePage.RemoveFromClassList("show-profile");
        scrim.RemoveFromClassList("show-scrim");
        loginContainer.AddToClassList("show-login");
        profileContainer.style.display = DisplayStyle.None;
    }

    private void OnQuestTransitionEnd(TransitionEndEvent evt)
    {
        // if (!questContainer.ClassListContains("show-quest"))
        // {
        //     questContainer.style.display = DisplayStyle.None;
        // }
    }

    private void OnLoginTransitionEnd(TransitionEndEvent evt)
    {
        // if (!loginContainer.ClassListContains("show-login"))
        // {
        //     loginContainer.style.display = DisplayStyle.None;
        // }
    }



    // // Update is called once per frame
    // void Update()
    // {
        
    // }
}
