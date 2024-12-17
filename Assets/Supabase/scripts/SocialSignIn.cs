using com.example;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TMPro;
using UnityEngine;
using static Supabase.Gotrue.Constants;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Newtonsoft.Json;

using System.Web;
using UnityEngine.UI;

public class SocialSignIn : MonoBehaviour
{
    [SerializeField] private string RedirectUrl = "supabaseunity://callback";

    public TMP_Text ErrorText = null!;
    public SupabaseManager SupabaseManager = null!;
    public SupabaseActions SupabaseActions = null!;
    IAppleAuthManager m_AppleAuthManager;
    public string Token { get; private set; }
    public string Error { get; private set; }

    // Private implementation
    private bool _doSignIn;
    private bool _doSignOut;
    private bool _doExchangeCode;

    public Button signInButton;
    public Button signOutButton;
    public Provider provider;
    public GameObject signInSpinner;

    public TMP_Text debugText;

    // Transactional data
    private string _pkce;
    private string _token;

    private string _nonce;
    private void Awake()
    {
        // Register for deep link activation
        Application.deepLinkActivated += OnDeepLinkActivated;

        SupabaseActions = FindObjectOfType<SupabaseActions>();
        SupabaseManager = FindObjectOfType<SupabaseManager>();

        // Check if we were launched with a deep link
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }

        // Load saved PKCE from PlayerPrefs if it exists
        _pkce = PlayerPrefs.GetString("PKCE", "");
    }

    private async void Start()
    {
        if (AppleAuthManager.IsCurrentPlatformSupported)
        {
            PayloadDeserializer deserializer = new PayloadDeserializer();
            m_AppleAuthManager = new AppleAuthManager(deserializer);
            ErrorText.text += "Apple auth started";
        }
        else
        {
            ErrorText.text += "Apple auth not supported";
        }

    }

    private void OnDeepLinkActivated(string url)
    {
        // Parse the URL to get the code parameter
        Uri uri = new Uri(url);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        // Check for error parameters
        string error = queryParams.Get("error");
        string errorDescription = queryParams.Get("error_description");

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"Auth Error: {error} - {errorDescription}");
            ErrorText.text = $"Authentication Error: {errorDescription}";
            return;
        }

        string code = queryParams.Get("code");
        Debug.Log("URL: " + url);
        Debug.Log("Code: " + code);

        if (!string.IsNullOrEmpty(code))
        {
            _token = code;
            _doExchangeCode = true;
        }
    }

    // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
    public void SignIn()
    {
        _doSignIn = true;
    }

    // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
    public void SignOut()
    {
        _doSignOut = true;
        PlayerPrefs.DeleteKey("accessToken");
        PlayerPrefs.DeleteKey("refreshToken");
        PlayerPrefs.DeleteKey("PKCE");
        PlayerPrefs.Save();
    }

    public async void Update()
    {
        // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
        if (_doSignOut)
        {
            _doSignOut = false;
            SupabaseActions.SetSpinner(signOutButton, true);
            await SupabaseManager.Supabase()!.Auth.SignOut();
            SupabaseActions.SetSpinner(signOutButton, false);
            _doSignOut = false;
        }

        if (m_AppleAuthManager != null)
        {
            m_AppleAuthManager.Update();
        }

        if (_doExchangeCode)
        {
            _doExchangeCode = false;
            try
            {
                await PerformExchangeCode();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e);
            }
            _doExchangeCode = false;
        }

        // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
        if (_doSignIn)
        {
            _doSignIn = false;
            signInButton.interactable = false;
            signInSpinner.SetActive(true);
            try
            {
                await PerformSignIn();
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                Debug.Log(e);
            }
            _doSignIn = false;
            signInButton.interactable = true;
            signInSpinner.SetActive(false);
        }
    }

    private string GenerateNonce()
    {
        var random = new RNGCryptoServiceProvider();
        var bytes = new byte[16];
        random.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public void Initialize()
    {
        var deserializer = new PayloadDeserializer();
        m_AppleAuthManager = new AppleAuthManager(deserializer);
    }

    private void RetrieveAppleTokenAsync()
    {
        try
        {
            if (m_AppleAuthManager == null)
            {
                Initialize();
            }

            _nonce = Helpers.GenerateNonce();
            var _nonceVerify = Helpers.GenerateSHA256NonceFromRawNonce(_nonce);

            ErrorText.text += $"\nNonce sent to Apple:\n {_nonce}";
            ErrorText.text += $"\nNonce Hash: {_nonceVerify}\n";

            AppleAuthLoginArgs loginArgs =
                new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName, _nonceVerify);

            m_AppleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    Debug.Log("LoginWithAppleId callback");
                    var appleIDCredential = credential as IAppleIDCredential;
                    if (appleIDCredential != null)
                    {
                        var idToken = Encoding.UTF8.GetString(
                            appleIDCredential.IdentityToken,
                            0,
                            appleIDCredential.IdentityToken.Length);
                        Debug.Log("Sign-in with Apple successfully done. IDToken: " + idToken);
                        Token = idToken;
                        Debug.Log("Token: " + Token);
                        AppleSignIn();
                    }
                    else
                    {
                        Debug.Log("Sign-in with Apple error. Message: appleIDCredential is null");
                        Error = "Retrieving Apple Id Token failed.";
                        ErrorText.text = Error;
                    }
                },
                error =>
                {
                    Debug.Log("Sign-in with Apple error. Message: " + error);
                    Error = "Retrieving Apple Id Token failed.";
                    ErrorText.text = Error;
                }
            );
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            Debug.Log(e);
            throw;
        }
    }

    public void SignInWithApple()
    {
        RetrieveAppleTokenAsync();
    }

    private async void AppleSignIn()
    {
        Debug.Log("Attempting to sign in with Apple via IDToken");
        if (Token == null)
        {
            debugText.text += "Null identity token\n";
            return;
        }
        debugText.text += "Starting supabase auth attempt\n";
        Task<Session> t = null;
        try
        {
            debugText.text += $"signing in with nonce {_nonce}";
            t = SupabaseManager.Supabase().Auth.SignInWithIdToken(Constants.Provider.Apple, Token, _nonce);
            Debug.Log("SignInWithIdToken completed");
            await t;
        }
        catch (Exception e)
        {
            Debug.Log("Exception with SignInWithIdToken");
            debugText.text = "Unknown Exception with SignInWithIdToken";
            debugText.text += $"\n Exception {e.Message}";
            debugText.text += $"\n {e.StackTrace}";
        }
        if (t?.IsCompletedSuccessfully == true)
        {
            Debug.Log("SignInWithIdToken completed successfully");
            debugText.text += $"\nsupabase login success\n {t.Result?.User?.Id}";
        }
        else
        {
            Debug.Log("SignInWithIdToken failed");
            debugText.text += $"\nsupabase failure\n {t?.Exception}";
        }
    }

    private async Task PerformSignIn()
    {
        try
        {
            Debug.Log($"Starting sign-in with provider: {provider}");
            var nonce = GenerateNonce();

            var providerAuth = (await SupabaseManager.Supabase()!.Auth.SignIn(provider, new SignInOptions
            {
#if UNITY_EDITOR
                RedirectTo = "http://localhost:8080/callback",
#else
                RedirectTo = RedirectUrl,
#endif
                FlowType = OAuthFlowType.PKCE,
                QueryParams = new Dictionary<string, string> { { "nonce", nonce }, { "token", Token } }
            }))!;

            Debug.Log($"Provider Auth URI: {providerAuth.Uri}");
            _pkce = providerAuth.PKCEVerifier;

            PlayerPrefs.SetString("PKCE", _pkce);
            PlayerPrefs.Save();

            Debug.Log("Opening URL in browser...");
            Application.OpenURL(providerAuth.Uri.ToString());
        }
        catch (GotrueException goTrueException)
        {
            Debug.LogError($"Provider: {provider}, Error: {goTrueException.Message}");
            ErrorText.text = $"{goTrueException.Reason} {goTrueException.Message}";
            Debug.LogException(goTrueException, gameObject);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message, gameObject);
            Debug.Log(e, gameObject);
        }
    }

    private async Task PerformExchangeCode()
    {
        try
        {
            Debug.Log($"PKCE: {_pkce}");
            Debug.Log($"Token: {_token}");
            Session session = (await SupabaseManager.Supabase()!.Auth.ExchangeCodeForSession(_pkce, _token)!);
            PlayerPrefs.DeleteKey("PKCE");
            PlayerPrefs.Save();
        }
        catch (GotrueException goTrueException)
        {
            Debug.Log("GotrueException");
            ErrorText.text = $"{goTrueException.Reason} {goTrueException.Message}";
            Debug.Log(goTrueException.Message, gameObject);
            Debug.LogException(goTrueException, gameObject);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message, gameObject);
            Debug.Log(e, gameObject);
        }
    }
}