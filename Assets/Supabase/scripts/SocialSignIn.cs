using com.example;

using com.example;
using System;
using System.Linq;
using System.Threading.Tasks;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using TMPro;
using UnityEngine;
using static Supabase.Gotrue.Constants;

using System.Web;
using UnityEngine.UI;


//From https://github.com/wiverson/supabase-unity-template
public class SocialSignIn : MonoBehaviour
{
    [SerializeField] private string RedirectUrl = "characterquest://callback";

    // Public Unity References
    public TMP_Text ErrorText = null!;
    public SupabaseManager SupabaseManager = null!;
    
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
            signOutButton.interactable = false;
            signOutButton.GetComponentInChildren<Spinner>().gameObject.SetActive(true);
            await SupabaseManager.Supabase()!.Auth.SignOut();
            signOutButton.GetComponentInChildren<Spinner>().gameObject.SetActive(false);
            signOutButton.interactable = true;
            _doSignOut = false;
        }

        if (_doExchangeCode)
        {
            _doExchangeCode = false;
            await PerformExchangeCode();
            _doExchangeCode = false;
        }

        // Unity does not allow async UI events, so we set a flag and use Update() to do the async work
        if (_doSignIn)
        {
            _doSignIn = false;
            signInButton.interactable = false;
            signInSpinner.SetActive(true);
            await PerformSignIn();
            _doSignIn = false;
            signInButton.interactable = true;
            signInSpinner.SetActive(false);
        }
    }

    private async Task PerformSignIn()
    {
        try
        {
            var providerAuth = (await SupabaseManager.Supabase()!.Auth.SignIn(provider, new SignInOptions
            {
                RedirectTo = RedirectUrl,
                FlowType = OAuthFlowType.PKCE,
            }))!;
            _pkce = providerAuth.PKCEVerifier;
            
            // Save PKCE to PlayerPrefs
            PlayerPrefs.SetString("PKCE", _pkce);
            PlayerPrefs.Save();

            Application.OpenURL(providerAuth.Uri.ToString());
        }
        catch (GotrueException goTrueException)
        {
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
}