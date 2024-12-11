using System.Collections.Generic;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using TMPro;
using UnityEngine;
namespace com.example
//From https://github.com/wiverson/supabase-unity-template
{
	public class SessionListener : MonoBehaviour
	{
		// Public Unity References
		public SupabaseManager SupabaseManager = null!;
		public TMP_Text LoggedInEmailAddress = null!;
		public TMP_Text name;
		public TMP_Text email;
		public GameObject signInActionsContainer;
		public GameObject signOutButton;
		public GameObject signInButtonsContainer;

		public void UnityAuthListener(IGotrueClient<User, Session> sender, Constants.AuthState newState)
		{
			bool hasSignedIn = sender.CurrentUser?.Email == null;
			Debug.Log("hasSignedIn: " + hasSignedIn);
			
			signInButtonsContainer.SetActive(hasSignedIn);
			signInActionsContainer.SetActive(!hasSignedIn);
			signOutButton.SetActive(!hasSignedIn);

			if (hasSignedIn)
			{
				name.text = "";
				email.text = "";
			}
			else
			{
				name.text = "Hey " + sender.CurrentUser?.UserMetadata?.GetValueOrDefault("full_name", "N/A").ToString().Split(" ")[0] + "!";
				email.text = sender.CurrentUser?.Email.Substring(0,3) + "...@"+ sender.CurrentUser?.Email.Split("@")[1];
			}

			switch (newState)
			{
				case Constants.AuthState.SignedIn:
					Debug.Log("Signed In");
					break;
				case Constants.AuthState.SignedOut:
					Debug.Log("Signed Out");
					break;
				case Constants.AuthState.UserUpdated:
					Debug.Log("Signed In");
					break;
				case Constants.AuthState.PasswordRecovery:
					Debug.Log("Password Recovery");
					break;
				case Constants.AuthState.TokenRefreshed:
					Debug.Log("Token Refreshed");
					break;
				case Constants.AuthState.Shutdown:
					Debug.Log("Shutdown");
					break;
				default:
					Debug.Log("Unknown Auth State Update");
					break;
			}
		}
	}
}
