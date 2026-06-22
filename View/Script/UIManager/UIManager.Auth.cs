using UnityEngine;
using UnityEngine.UIElements;

public partial class UIManager
{
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

        if (usernameInput.value.Length < 3)
        {
            errorMessage = "Username minimal 3 karakter";
            return false;
        }

        // Password validation according to backend requirements
        string password = passwordInput.value;

        if (password.Length < 8)
        {
            errorMessage = "Password minimal 8 karakter";
            return false;
        }

        if (password.Length > 30)
        {
            errorMessage = "Password maksimal 30 karakter";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]"))
        {
            errorMessage = "Password harus mengandung minimal 1 huruf besar";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]"))
        {
            errorMessage = "Password harus mengandung minimal 1 huruf kecil";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]"))
        {
            errorMessage = "Password harus mengandung minimal 1 angka";
            return false;
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\",./<>?]"))
        {
            errorMessage = "Password harus mengandung minimal 1 simbol (!@#$%^&*)";
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
            authValidationText.text = errorMessage;
            return;
        }

        // Clear warning message
        authValidationText.text = string.Empty;

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
            authValidationText.text = string.Empty;
        }
        else
        {
            authValidationText.text = "Login gagal. Periksa username dan password Anda.";
            Debug.Log("Login failed. Please check your credentials.");
        }
    }

    private async void OnRegisterClicked(ClickEvent evt)
    {
        Debug.Log("Register button clicked!");

        // Validate input first
        if (!ValidateAuthInput(out string errorMessage))
        {
            authValidationText.text = errorMessage;
            return;
        }

        // Clear warning message
        authValidationText.text = string.Empty;

        RegisterButton.SetEnabled(false);
        loginButton.SetEnabled(false);
        bool success = await LoginManager.Instance.SignUp(usernameInput.value, passwordInput.value);
        RegisterButton.SetEnabled(true);
        loginButton.SetEnabled(true);

        if (!success)
        {
            authValidationText.text = "Registrasi gagal. Username mungkin sudah digunakan.";
            Debug.Log("Registration failed. Please check your input.");
            return;
        }

        usernameInput.value = string.Empty;
        passwordInput.value = string.Empty;
        authValidationText.text = string.Empty;
    }

    private void OnSignOutClicked(ClickEvent evt)
    {
        Debug.Log("Sign Out button clicked!");
        LoginManager.Instance.SignOut();
        profilePage.RemoveFromClassList("show-profile");
        scrim.RemoveFromClassList("show-scrim");
        loginContainer.AddToClassList("show-login");
        profileContainer.style.display = DisplayStyle.None;
        authValidationText.text = string.Empty;
    }
}
