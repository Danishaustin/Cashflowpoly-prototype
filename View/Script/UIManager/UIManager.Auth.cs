using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
    private const string AuthErrorNetworkOffline = "NETWORK_OFFLINE";
    private const string AuthErrorRequestTimeout = "REQUEST_TIMEOUT";
    private const string AuthErrorServerDown = "SERVER_DOWN";
    private const string AuthErrorServerUnreachable = "SERVER_UNREACHABLE";
    private const string AuthErrorInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    private const string AuthErrorUsernameTaken = "USERNAME_ALREADY_EXISTS";
    private const string AuthErrorRoleNotAllowed = "AUTH_ROLE_NOT_ALLOWED";

    // Validation helper method
    private bool ValidateAuthInput(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(usernameInput.value))
        {
            errorMessage = "Username tidak boleh kosong";
            return false;
        }

        if (string.IsNullOrWhiteSpace(passwordInput.value))
        {
            errorMessage = "Password tidak boleh kosong";
            return false;
        }

        // if (usernameInput.value.Length < 3)
        // {
        //     errorMessage = "Username minimal 3 karakter";
        //     return false;
        // }

        // Password validation according to backend requirements
        string password = passwordInput.value;

        if (password.Length < 12)
        {
            errorMessage = "Password minimal 12 karakter";
            return false;
        }

        // if (password.Length > 30)
        // {
        //     errorMessage = "Password maksimal 30 karakter";
        //     return false;
        // }

        // if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]"))
        // {
        //     errorMessage = "Password harus mengandung minimal 1 huruf besar";
        //     return false;
        // }

        // if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]"))
        // {
        //     errorMessage = "Password harus mengandung minimal 1 huruf kecil";
        //     return false;
        // }

        // if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]"))
        // {
        //     errorMessage = "Password harus mengandung minimal 1 angka";
        //     return false;
        // }

        // if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\",./<>?]"))
        // {
        //     errorMessage = "Password harus mengandung minimal 1 simbol (!@#$%^&*)";
        //     return false;
        // }

        return true;
    }

    private bool ValidateAddPlayerInput(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(addPlayerUsernameInput.value))
        {
            errorMessage = "Username player tidak boleh kosong";
            return false;
        }

        if (string.IsNullOrWhiteSpace(addPlayerPasswordInput.value))
        {
            errorMessage = "Password player tidak boleh kosong";
            return false;
        }

        if (addPlayerPasswordInput.value.Length < 12)
        {
            errorMessage = "Password minimal 12 karakter";
            return false;
        }

        return true;
    }

    // Login, register, and sign-out actions.
    private async void OnLoginClicked(ClickEvent evt)
    {
        Debug.Log("Login button clicked!");

        // Validate input first
        if (!ValidateAuthInput(out string errorMessage))
        {
            ShowErrorPopup(errorMessage);
            return;
        }

        bool success = false;
        BeginAuthLoading("Memproses login...");
        try
        {
            RegisterButton.SetEnabled(false);
            loginButton.SetEnabled(false);
            success = await LoginManager.Instance.SignIn(usernameInput.value, passwordInput.value);
        }
        finally
        {
            RegisterButton.SetEnabled(true);
            loginButton.SetEnabled(true);
            EndAuthLoading();
        }

        if (success)
        {
            loginContainer.RemoveFromClassList("show-login");
            usernameInput.value = string.Empty;
            passwordInput.value = string.Empty;
            ShowSuccessPopup("Login berhasil.");
        }
        else
        {
            string failureMessage = GetLoginFailureMessage();
            ShowErrorPopup(failureMessage);
            Debug.Log("Login failed. Please check your credentials.");
        }
    }

    private async void OnRegisterClicked(ClickEvent evt)
    {
        Debug.Log("Register button clicked!");

        // Validate input first
        if (!ValidateAuthInput(out string errorMessage))
        {
            ShowErrorPopup(errorMessage);
            return;
        }

        bool success = false;
        BeginAuthLoading("Memproses registrasi...");
        try
        {
            RegisterButton.SetEnabled(false);
            loginButton.SetEnabled(false);
            success = await LoginManager.Instance.SignUp(usernameInput.value, passwordInput.value);
        }
        finally
        {
            RegisterButton.SetEnabled(true);
            loginButton.SetEnabled(true);
            EndAuthLoading();
        }

        if (!success)
        {
            string failureMessage = GetRegisterFailureMessage();
            ShowErrorPopup(failureMessage);
            Debug.Log("Registration failed. Please check your input.");
            return;
        }

        loginContainer.RemoveFromClassList("show-login");
        usernameInput.value = string.Empty;
        passwordInput.value = string.Empty;
        ShowSuccessPopup("Registrasi berhasil.");
    }

    private async void OnAddPlayerRegisterClicked(ClickEvent evt)
    {
        Debug.Log("Add Player register button clicked!");

        if (!ValidateAddPlayerInput(out string errorMessage))
        {
            ShowAddPlayerMessage(errorMessage, false);
            return;
        }

        addPlayerValidationText.text = string.Empty;

        bool success = false;
        BeginAddPlayerLoading("Mendaftarkan player...");
        try
        {
            addPlayerRegisterButton?.SetEnabled(false);
            backAddPlayer?.SetEnabled(false);
            success = await LoginManager.Instance.RegisterPlayer(addPlayerUsernameInput.value, addPlayerPasswordInput.value);
        }
        finally
        {
            addPlayerRegisterButton?.SetEnabled(true);
            backAddPlayer?.SetEnabled(true);
            EndAddPlayerLoading();
        }

        if (!success)
        {
            string failureMessage = GetRegisterFailureMessage();
            ShowAddPlayerMessage(failureMessage, false);
            ShowErrorPopup(failureMessage);
            Debug.Log("Register player failed. Please check your input.");
            return;
        }

        addPlayerUsernameInput.value = string.Empty;
        addPlayerPasswordInput.value = string.Empty;
        ShowAddPlayerMessage("Player berhasil didaftarkan.", true);
        ShowSuccessPopup("Player berhasil didaftarkan.");
    }

    private void OnSignOutClicked(ClickEvent evt)
    {
        Debug.Log("Sign Out button clicked!");
        LoginManager.Instance.SignOut();
        loginContainer.AddToClassList("show-login");
    }

    private void BeginAuthLoading(string message)
    {
        if (authLoadingOverlay != null)
        {
            authLoadingOverlay.style.display = DisplayStyle.Flex;
        }

        if (authLoadingText != null && !string.IsNullOrWhiteSpace(message))
        {
            authLoadingText.text = message;
        }

        if (authLoadingSpinner != null)
        {
            authLoadingSpinner.style.rotate = new Rotate(new Angle(0f, AngleUnit.Degree));
            if (authLoadingSpinnerCoroutine == null)
            {
                authLoadingSpinnerCoroutine = StartCoroutine(AnimateAuthLoadingSpinner());
            }
        }
    }

    private void EndAuthLoading()
    {
        if (authLoadingOverlay != null)
        {
            authLoadingOverlay.style.display = DisplayStyle.None;
        }

        if (authLoadingSpinnerCoroutine != null)
        {
            StopCoroutine(authLoadingSpinnerCoroutine);
            authLoadingSpinnerCoroutine = null;
        }
    }

    private IEnumerator AnimateAuthLoadingSpinner()
    {
        float angle = 0f;

        while (true)
        {
            if (authLoadingSpinner != null)
            {
                angle += Time.deltaTime * 360f;
                authLoadingSpinner.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            }

            yield return null;
        }
    }

    private void BeginAddPlayerLoading(string message)
    {
        if (addPlayerLoadingOverlay != null)
        {
            addPlayerLoadingOverlay.style.display = DisplayStyle.Flex;
        }

        if (addPlayerLoadingText != null && !string.IsNullOrWhiteSpace(message))
        {
            addPlayerLoadingText.text = message;
        }

        if (addPlayerLoadingSpinner != null)
        {
            addPlayerLoadingSpinner.style.rotate = new Rotate(new Angle(0f, AngleUnit.Degree));
            if (addPlayerLoadingSpinnerCoroutine == null)
            {
                addPlayerLoadingSpinnerCoroutine = StartCoroutine(AnimateAddPlayerLoadingSpinner());
            }
        }
    }

    private void EndAddPlayerLoading()
    {
        if (addPlayerLoadingOverlay != null)
        {
            addPlayerLoadingOverlay.style.display = DisplayStyle.None;
        }

        if (addPlayerLoadingSpinnerCoroutine != null)
        {
            StopCoroutine(addPlayerLoadingSpinnerCoroutine);
            addPlayerLoadingSpinnerCoroutine = null;
        }
    }

    private IEnumerator AnimateAddPlayerLoadingSpinner()
    {
        float angle = 0f;

        while (true)
        {
            if (addPlayerLoadingSpinner != null)
            {
                angle += Time.deltaTime * 360f;
                addPlayerLoadingSpinner.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            }

            yield return null;
        }
    }

    private void ShowAddPlayerMessage(string message, bool isSuccess)
    {
        if (addPlayerValidationText == null)
        {
            return;
        }

        addPlayerValidationText.text = message;
        addPlayerValidationText.style.color = isSuccess
            ? new Color(100f / 255f, 200f / 255f, 100f / 255f)
            : new Color(190f / 255f, 40f / 255f, 40f / 255f);
    }

    private string GetLoginFailureMessage()
    {
        return GetAuthFailureMessage(false);
    }

    private string GetRegisterFailureMessage()
    {
        return GetAuthFailureMessage(true);
    }

    private string GetAuthFailureMessage(bool isRegister)
    {
        string errorCode = LoginManager.Instance != null ? LoginManager.Instance.LastAuthErrorCode : string.Empty;
        string fallbackMessage = LoginManager.Instance != null ? LoginManager.Instance.LastAuthErrorMessage : string.Empty;

        if (errorCode == AuthErrorNetworkOffline)
        {
            return "Koneksi internet belum tersambung.";
        }

        if (errorCode == AuthErrorRequestTimeout)
        {
            return "Permintaan ke server terlalu lama. Coba lagi.";
        }

        if (errorCode == AuthErrorServerDown || errorCode == AuthErrorServerUnreachable)
        {
            return "Server sedang bermasalah atau tidak bisa dihubungi.";
        }

        if (errorCode == AuthErrorInvalidCredentials)
        {
            return "Username atau password salah.";
        }

        if (errorCode == AuthErrorUsernameTaken)
        {
            return "Username sudah digunakan.";
        }

        if (errorCode == AuthErrorRoleNotAllowed)
        {
            return "Akun ini bukan instructor. Silakan login menggunakan akun instructor.";
        }

        if (!string.IsNullOrWhiteSpace(fallbackMessage))
        {
            return fallbackMessage;
        }

        return isRegister
            ? "Registrasi gagal. Coba lagi nanti."
            : "Login gagal. Coba lagi nanti.";
    }
}
