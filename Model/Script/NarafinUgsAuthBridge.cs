using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public sealed class NarafinUgsAuthBridge
{
    private const string UgsAuthFailedCode = "UGS_AUTH_FAILED";
    private const string UgsAuthModeUsernamePassword = "username_password_testing";
    private const string UgsProfilePrefix = "narafin_";
    private const string UgsUsernamePrefix = "nf_";
    private const string UgsPasswordPrefix = "Nf1!";
    private bool isInitializing;
    private Task initializeTask;

    public string LastErrorCode { get; private set; } = string.Empty;
    public string LastErrorMessage { get; private set; } = string.Empty;

    public bool IsSignedIn
    {
        get
        {
            return NarafinRuntimeConfig.UseUnityGameServicesAuth &&
                   UnityServices.State == ServicesInitializationState.Initialized &&
                   AuthenticationService.Instance.IsSignedIn;
        }
    }

    public string PlayerId
    {
        get
        {
            if (!IsSignedIn)
            {
                return string.Empty;
            }

            return AuthenticationService.Instance.PlayerId ?? string.Empty;
        }
    }

    public async Task<NarafinSessionOperationResult> EnsureSignedInAsync(NarafinAuthSession narafinSession)
    {
        ClearLastError();

        if (!NarafinRuntimeConfig.UseUnityGameServicesAuth)
        {
            Debug.Log("UGS auth dimatikan lewat NarafinRuntimeConfig.UseUnityGameServicesAuth.");
            return CreateSuccessResult();
        }

        if (narafinSession == null || string.IsNullOrWhiteSpace(narafinSession.user_id))
        {
            return CreateFailureResult(UgsAuthFailedCode, "Session Narafin belum valid untuk sinkronisasi UGS.");
        }

        try
        {
            await InitializeAsync();

            string profile = BuildProfileName(narafinSession.user_id);
            string accountHash = ComputeStableHash(narafinSession.user_id);
            string username = BuildUsername(accountHash);
            string password = BuildPassword(accountHash);
            string linkedNarafinUserId = NarafinPlayerPrefs.UgsLinkedNarafinUserId;
            Debug.Log("UGS auth menyiapkan profile: " + profile);

            if (AuthenticationService.Instance.IsSignedIn &&
                string.Equals(linkedNarafinUserId, narafinSession.user_id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(NarafinPlayerPrefs.UgsAuthMode, UgsAuthModeUsernamePassword, StringComparison.OrdinalIgnoreCase))
            {
                StoreMapping(narafinSession.user_id, profile);
                Debug.Log("UGS auth sudah aktif. Unity PlayerId: " + AuthenticationService.Instance.PlayerId);
                return CreateSuccessResult();
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }

            AuthenticationService.Instance.SwitchProfile(profile);
            Debug.LogWarning("UGS username/password testing aktif. Jangan gunakan mode ini untuk production tanpa validasi backend.");
            await SignInOrSignUpWithUsernamePasswordAsync(username, password);

            StoreMapping(narafinSession.user_id, profile);
            Debug.Log("UGS auth berhasil dengan username/password testing. Narafin user_id: " + narafinSession.user_id + ", Unity PlayerId: " + AuthenticationService.Instance.PlayerId);
            return CreateSuccessResult();
        }
        catch (Exception ex)
        {
            return CreateFailureResult(UgsAuthFailedCode, "Gagal login ke Unity Game Services: " + ex.Message);
        }
    }

    public async Task<NarafinSessionOperationResult> InitializeAsync()
    {
        ClearLastError();

        if (!NarafinRuntimeConfig.UseUnityGameServicesAuth)
        {
            Debug.Log("UGS initialize dilewati karena UseUnityGameServicesAuth = false.");
            return CreateSuccessResult();
        }

        try
        {
            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                Debug.Log("UGS sudah initialized.");
                return CreateSuccessResult();
            }

            if (isInitializing && initializeTask != null)
            {
                Debug.Log("UGS initialize sedang berjalan, menunggu proses yang sama.");
                await initializeTask;
                return CreateSuccessResult();
            }

            isInitializing = true;
            Debug.Log("UGS initialize dimulai. State: " + UnityServices.State);
            initializeTask = UnityServices.InitializeAsync();
            await initializeTask;
            Debug.Log("UGS initialize berhasil. State: " + UnityServices.State);
            return CreateSuccessResult();
        }
        catch (Exception ex)
        {
            return CreateFailureResult(UgsAuthFailedCode, "Gagal initialize Unity Game Services: " + ex.Message);
        }
        finally
        {
            isInitializing = false;
        }
    }

    public void SignOut()
    {
        if (!NarafinRuntimeConfig.UseUnityGameServicesAuth)
        {
            return;
        }

        try
        {
            if (UnityServices.State == ServicesInitializationState.Initialized &&
                AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Gagal sign out UGS: " + ex.Message);
        }
    }

    private void StoreMapping(string narafinUserId, string profile)
    {
        NarafinPlayerPrefs.StoreUgsAuthMapping(
            narafinUserId,
            AuthenticationService.Instance.PlayerId ?? string.Empty,
            profile,
            UgsAuthModeUsernamePassword);
    }

    private static string BuildProfileName(string narafinUserId)
    {
        return UgsProfilePrefix + ComputeStableHash(narafinUserId);
    }

    private static string BuildUsername(string accountHash)
    {
        return UgsUsernamePrefix + accountHash;
    }

    private static string BuildPassword(string accountHash)
    {
        return UgsPasswordPrefix + accountHash;
    }

    private static async Task SignInOrSignUpWithUsernamePasswordAsync(string username, string password)
    {
        try
        {
            Debug.Log("UGS username/password sign-in dimulai. Username: " + username);
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
            return;
        }
        catch (Exception signInEx)
        {
            Debug.Log("UGS username/password sign-in gagal, mencoba sign-up. Detail: " + signInEx.Message);
        }

        try
        {
            await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
            Debug.Log("UGS username/password sign-up berhasil. Username: " + username);
        }
        catch (Exception signUpEx)
        {
            Debug.Log("UGS username/password sign-up gagal, mencoba sign-in ulang. Detail: " + signUpEx.Message);
            await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
        }
    }

    private static string ComputeStableHash(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        ulong hash = 14695981039346656037UL;

        for (int i = 0; i < bytes.Length; i++)
        {
            hash ^= bytes[i];
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private NarafinSessionOperationResult CreateFailureResult(string errorCode, string errorMessage)
    {
        LastErrorCode = errorCode ?? string.Empty;
        LastErrorMessage = errorMessage ?? string.Empty;
        Debug.LogWarning(LastErrorMessage);

        return new NarafinSessionOperationResult
        {
            Success = false,
            ErrorCode = LastErrorCode,
            ErrorMessage = LastErrorMessage
        };
    }

    private static NarafinSessionOperationResult CreateSuccessResult()
    {
        return new NarafinSessionOperationResult
        {
            Success = true
        };
    }

    private void ClearLastError()
    {
        LastErrorCode = string.Empty;
        LastErrorMessage = string.Empty;
    }
}
