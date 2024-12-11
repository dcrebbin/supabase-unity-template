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
    public TMP_InputField characterInput;

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
                var retrievedCharacters = string.Join("\n", characters.Models.Select(c => $"- {((Character)c).text}"));
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
            string characterId = characterInput.text;
            Debug.Log("Updating known character");
            var character = new Character { id = characterId, text = characterInput.text,user_id = SupabaseManager.Supabase().Auth.CurrentUser.Id.ToString(), language_code="zh_CN" };
            var response = await SupabaseManager.Supabase()?.From<Character>().Update(character);
            Debug.Log($"Updated character with ID: {characterId}");
            this.characterText.text = $"Updated character: {character.text}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to update character: {e.Message}");
            errorText.text = "Failed to update character";
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
