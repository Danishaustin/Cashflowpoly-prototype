using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance;

    void Awake()
    {
        Instance = this;
    }
    public async Task InitializeServicesAsync()
    {
        await UnityServices.InitializeAsync();
    }
    public async Task<bool> SignUp(string username, string password)
    {

        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("Username is empty.");
            return false;
        }
        if (string.IsNullOrEmpty(password))
        {
            Debug.Log("Password is empty.");
            return false;
        }

        bool success =await SignUpWithUsernamePasswordAsync(username, password);
        return success;
    }
    public async Task<bool> SignIn(string username, string password)
    {

        if (string.IsNullOrEmpty(username))
        {
            Debug.Log("Username is empty.");
            return false;
        }

        if (string.IsNullOrEmpty(password))
        {
            Debug.Log("Password is empty.");
            return false;
        }

        bool success = await SignInWithUsernamePasswordAsync(username, password);
        return success;
    }

    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
        Debug.Log("Player signed out.");
    }

    public bool IsSignedIn()
    {
        return AuthenticationService.Instance.IsSignedIn;
    }

    async Task<bool> SignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("SignUp is successful.");
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
            AuthenticationService.Instance.SignOut();
            return true;
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            return false;
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            return false;
        }
    }

    async Task<bool> SignInWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            Debug.Log("Is Signed In: " + AuthenticationService.Instance.IsSignedIn);
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
            return true;
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            return false;
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
            return false;
        }
    }

    async Task AddUsernamePasswordAsync(string username, string password)
    {
        try
        {
            await AuthenticationService.Instance.AddUsernamePasswordAsync(username, password);
            Debug.Log("Username and password added.");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }
}
