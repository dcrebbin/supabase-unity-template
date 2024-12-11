using com.example;

using com.example;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TMPro;
using UnityEngine;
using static Supabase.Gotrue.Constants;

using System.Web;
using UnityEngine.UI;

public class SocialSignIn : MonoBehaviour
{
    [SerializeField] private string RedirectUrl = "characterquest://callback";

    // Public Unity References
    public TMP_Text ErrorText = null!;
    public SupabaseManager SupabaseManager = null!;
    public SupabaseActions SupabaseActions = null!;
    
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
    
    private void OnDeepLinkActivated(string url)
    {
        // Parse the URL to get the code parameter
        Uri uri = new Uri(url);
        string code = HttpUtility.ParseQueryString(uri.Query).Get("code");

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
    }

    private async void Update()
    {
        // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
        if (_doSignOut)
        {
            _doSignOut = false;
            SupabaseActions.SetSpinner(signOutButton,true);
            await SupabaseManager.Supabase()!.Auth.SignOut();
            SupabaseActions.SetSpinner(signOutButton,false);
            _doSignOut = false;
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

    private string GenerateNonce(){
        var random = new RNGCryptoServiceProvider();
        var bytes = new byte[16];
        random.GetBytes(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');  
    }

    private async Task PerformSignIn()
    {
        try
        {
            Debug.Log($"Starting sign-in with provider: {provider}");
            var nonce = GenerateNonce();
            var providerAuth = (await SupabaseManager.Supabase()!.Auth.SignIn(provider, new SignInOptions
            {
                RedirectTo = RedirectUrl,
                FlowType = OAuthFlowType.PKCE,
                QueryParams = new Dictionary<string, string> { { "nonce", nonce } }
            }))!;
            
            Debug.Log($"Provider Auth URI: {providerAuth.Uri}");
            _pkce = providerAuth.PKCEVerifier;
            
            // Save PKCE to PlayerPrefs
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
            debugText.text = "PKCE: " + _pkce + "\nToken: " + _token;
            
            Session session = (await SupabaseManager.Supabase()!.Auth.ExchangeCodeForSession(_pkce, _token)!);
            
            // Clear saved PKCE after successful exchange
            PlayerPrefs.DeleteKey("PKCE");
            PlayerPrefs.Save();
            
            ErrorText.text = $"Success! Signed in as {session.User?.UserMetadata["name"].ToString().Split(" ")[0]}";
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

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string HashString(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}