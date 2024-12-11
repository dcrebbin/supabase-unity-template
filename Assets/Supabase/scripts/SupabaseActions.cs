using System;
using System.Linq;
using System.Threading.Tasks;
using com.example;
using TMPro;
using UnityEngine;
using Postgrest.Models;
using Postgrest.Attributes;
using UnityEngine.UI;

public class SupabaseActions : MonoBehaviour
{
    public TMP_Text errorText;
    public SupabaseManager SupabaseManager = null!;

    public Text characterText;
    public InputField knownCharacterInput;

    public InputField characterRemovalInput;

    public void GetCharacters()
    {
        GetKnownCharacters();
    }

    private async Task GetKnownCharacters()
        {
            try
            {
                Debug.Log("Retrieving known characters");
                var characters = await SupabaseManager.Supabase()?.From<Character>().Filter("user_id", Postgrest.Constants.Operator.Equals, SupabaseManager.Supabase().Auth.CurrentUser.Id).Get();
                var retrievedCharacters = string.Join(",", characters.Models.Select(c => $"{((Character)c).text}"));
                this.characterText.text = retrievedCharacters;
                Debug.Log($"Retrieved {characters.Models.Count} characters");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to retrieve characters: {e.Message}");
                errorText.text = "Failed to retrieve characters";
            }
        }

    public void UpdateCharacter()
    {
        UpdateKnownCharacter();
    }

    private async Task UpdateKnownCharacter()
    {
        try
        {
            string characterText = knownCharacterInput.text;
            Debug.Log("Adding known character");

            var isCharacterKnown = (await SupabaseManager.Supabase()?.From<Character>().Filter(
                "user_id", Postgrest.Constants.Operator.Equals, SupabaseManager.Supabase().Auth.CurrentUser.Id)
                .Filter("text", Postgrest.Constants.Operator.Equals, characterText).Get()).Models.Count > 0;
            if (isCharacterKnown)
            {
                knownCharacterInput.text = "ALREADY KNOWN";
                return;
            }
            
            var character = new Character { 
                text = characterText,
                user_id = SupabaseManager.Supabase().Auth.CurrentUser.Id,
                language_code = "zh_CN",
                learnt_at = DateTime.UtcNow
            };
            
            var response = await SupabaseManager.Supabase()?.From<Character>().Insert(character);
            Debug.Log("Added " + response.Models.First().text);
            knownCharacterInput.text = characterText + " ADDED";
        }
        catch (Exception e)
        {
            knownCharacterInput.text = "FAILED";
            Debug.LogError($"Failed to update character: {e.Message}");
            errorText.text = "Failed to update character";
        }
    }

    public void RemoveCharacter()
    {
        RemoveKnownCharacter();
    }

    private async Task RemoveKnownCharacter()
    {
        try
        {
            string characterText = characterRemovalInput.text;
            Debug.Log("Removing known character");

            // First get the character record
            var characters = await SupabaseManager.Supabase()?.From<Character>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, SupabaseManager.Supabase().Auth.CurrentUser.Id)
                .Filter("text", Postgrest.Constants.Operator.Equals, characterText)
                .Get();

            if (characters.Models.Count == 0)
            {
                characterRemovalInput.text = "NOT KNOWN";
                return;
            }

            // Get the existing character with its proper ID
            var existingCharacter = (Character)characters.Models.First();
            
            // Delete using the proper character object
            var response = await SupabaseManager.Supabase()?.From<Character>().Delete(existingCharacter);
            characterRemovalInput.text = characterText + " REMOVED";
            Debug.Log($"Removed character: {characterText}");
        }
        catch (Exception e)
        {
            characterRemovalInput.text = "FAILED";
            Debug.LogError($"Failed to remove character: {e.Message}");
            errorText.text = "Failed to remove character";
        }
    }
}


[Table("known_characters")]
public class Character : BaseModel
{
    [PrimaryKey("id")]
    public string id { get; set; }
    
    [Column("learnt_at")]
    public DateTime learnt_at { get; set; }

    [Column("text")]
    public string text { get; set; }

    [Column("language_code")]
    public string language_code { get; set; }
    
    [Column("user_id")]
    public string user_id { get; set; }
}
