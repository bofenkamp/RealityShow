using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

public class CharacterCreator : MonoBehaviour
{
    private enum CharacterCreationStage { Name, Details, Done }

    [Header("External")]
    [SerializeField] private UIController _uiCont;
    [SerializeField] private GameController _gameCont;

    [Header("Player Character Creator")]
    private CharacterCreationStage _currStage = CharacterCreationStage.Name;

    [Header("NPC Creation Stats")]
    [SerializeField] private bool _generateNPCs;
    public int CharacterCount;
    [SerializeField] private float _nbChance;
    [SerializeField] private TextAsset _regionsFile;
    [SerializeField] private TextAsset _majorsFile;
    [SerializeField] private TextAsset _traitsFile;
    [SerializeField] private int _inputTraits;
    private (string, int)[] _regionChances;
    private int _regionChancesTotal;
    private (string, int)[] _majorChances;
    private int _majorChancesTotal;
    private string[] _traits;

    [Header("AI Instructions")]
    [SerializeField] private PromptsContainer _prompts;

    void Start()
    {
        _regionChances = UnpackWeightedListFile(_regionsFile, out _regionChancesTotal);
        _majorChances = UnpackWeightedListFile(_majorsFile, out _majorChancesTotal);
        _traits = UnpackUnweightedListFile(_traitsFile);
        if (_generateNPCs)
            StartCoroutine(CreateCharacterProfiles());     
    }

    #region Player Character

    public void OnContinuePressed()
    {
        switch (_currStage)
        {
            case CharacterCreationStage.Name:
                RecordPlayerCharacterName();
                break;
            case CharacterCreationStage.Details:
                RecordPlayerCharacterDetails();
                break;
        }
    }

    private void RecordPlayerCharacterName()
    {
        var playerForename = _uiCont.GetPlayerCharacterForename();
        var playerSurname = _uiCont.GetPlayerCharacterSurname();
        _gameCont.SetPlayerName(playerForename, playerSurname);
        _uiCont.SetUpDetailsSection(playerForename, playerSurname);
        _currStage = CharacterCreationStage.Details;
    }

    private void RecordPlayerCharacterDetails()
    {
        var gender = _uiCont.GetGender();
        var year = _uiCont.GetYear();
        var hometown = _uiCont.GetHometown();
        var major = _uiCont.GetMajor();
        var appearance = _uiCont.GetPhysicalAppearance();
        var background = _uiCont.GetBackground();
        var personality = _uiCont.GetPersonality();
        var perceived = _uiCont.GetPerceivedPersonality();
        _gameCont.SetPlayerDetails(gender, year, hometown, major, appearance, background, personality, perceived);
        _currStage = CharacterCreationStage.Done;
        _uiCont.ShowMainGame();
    }

    #endregion

    #region Other Contestants

    private IEnumerator CreateCharacterProfiles()
    {
        var basicPromptText = _prompts.DecideNamesHometownsPrefix;
        for (int i = 1; i <= CharacterCount; i++)
        {
            basicPromptText += $"{i}. {CreateRandomCharacterSeed()}\n";
        }

        string basicResponseText = "";
        yield return AIChatController.Instance.SinglePrompt(basicPromptText, result => basicResponseText = result, 
            chunk => Debug.Log(chunk));

        var basicProfiles = JsonUtility.FromJson<BasicProfilesSummary>(basicResponseText);
        string advancedPromptText = _prompts.ExtendedProfilesPrefix;
        foreach (var profile in basicProfiles.students)
        {
            var major = GetMajor();
            int likability = Random.Range(0, 11);
            int energy = Random.Range(0, 11);
            int status = Random.Range(0, 11);
            var traits = GetTraits();
            advancedPromptText += $"{profile.first_name} {profile.last_name}:\n";
            advancedPromptText += $"Gender: {profile.gender}\n";
            advancedPromptText += $"Hometown: {profile.hometown}\n";
            advancedPromptText += $"Major: {major}\n";
            advancedPromptText += $"Likability: {likability}\n";
            advancedPromptText += $"Energy: {energy}\n";
            advancedPromptText += $"Status: {status}\n";
            advancedPromptText += $"Traits: {traits}\n\n";
        }
        string advancedResponseText = "";
        yield return AIChatController.Instance.SinglePrompt(advancedPromptText, result => advancedResponseText = result, 
            chunk => Debug.Log(chunk));

        Debug.Log(advancedResponseText);
        var characterProfiles = JsonUtility.FromJson<CharacterProfiles>(advancedResponseText);
        _gameCont.AddNPCs(characterProfiles);
    }

    private string CreateRandomCharacterSeed()
    {
        var gender = GetGender();
        var year = GetYear();
        var region = GetRegion();
        return $"{gender}, {region}, {year}";
    }

    private string GetGender()
    {
        var mChance = (1f - _nbChance) / 2f;
        var rand = Random.value;
        return rand < mChance ? "Male" :
            rand < mChance + _nbChance ? "Nonbinary" : "Female";
    }

    private string GetYear()
    {
        var rand = Random.value;
        return rand < 0.5 ? (rand < 0.25 ? "Freshman" : "Sophomore") :
            rand > 0.75 ? "Senior" : "Junior";
    }

    private string GetRegion()
    {
        return GetFromWeightedList(_regionChances, _regionChancesTotal, "Anywhere");
    }

    private string GetMajor()
    {
        return GetFromWeightedList(_majorChances, _majorChancesTotal, "Undecided");
    }

    private string GetTraits()
    {
        var traits = _traits.ToList();
        var chosenTraits = new string[_inputTraits];

        for (int i = 0; i < _inputTraits; i++)
        {
            int n = Random.Range(0, traits.Count);
            chosenTraits[i] = traits[n].Trim(new char[] { ' ', '\n', '\r' });
            traits.RemoveAt(n);
        }

        return string.Join(", ", chosenTraits);
    }

    private string GetFromWeightedList((string, int)[] values, int maxValue, string defaultResult)
    {
        int n = Random.Range(0, maxValue);
        for (int i = 0; i < values.Length; i++)
        {
            var curr = values[i];
            if (n < curr.Item2)
                return curr.Item1;
            n -= curr.Item2;
        }

        return defaultResult;
    }

    private (string, int)[] UnpackWeightedListFile(TextAsset file, out int total)
    {
        var fileText = file.text;
        var lines = fileText.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
        var result = new (string, int)[lines.Length];
        total = 0;

        for (int i = 0; i < result.Length; i++)
        {
            var line = lines[i].Split(',', System.StringSplitOptions.RemoveEmptyEntries);
            var key = line[0];
            var val = int.Parse(line[1]);
            result[i] = (key, val);
            total += val;
        }

        return result;
    }

    private string[] UnpackUnweightedListFile(TextAsset file)
    {
        return file.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
    }

    #endregion

    #region Challenge Givers

    public IEnumerator CreateChallenger(int playerCount, Action<ChallengerProfile> onComplete)
    {
        var gender = GetGender();
        var homeRegion = GetRegion();
        var department = GetMajor();
        var traits = GetTraits();

        var prompt = _prompts.ProfessorGenPrefix.Replace("#", playerCount.ToString());
        prompt += $"Gender: {gender}\nFrom: {homeRegion}\nTeaches: {department}\nTraits: {traits}";
        if (playerCount <= CharacterCount)
        {
            prompt += _prompts.ExistingProfs;
            for (int i = playerCount + 1; i < _gameCont.Challengers.Length; i++)
                prompt += $"{_gameCont.Challengers[i].full_name}\n{_gameCont.Challengers[i].challenge_announcement}\n\n";
        }
        string professorText = "";
        yield return AIChatController.Instance.SinglePrompt(prompt, result => professorText = result, chunk => Debug.Log(chunk));
        Debug.Log(professorText);
        var prof = JsonUtility.FromJson<ChallengerProfile>(professorText);
        onComplete.Invoke(prof);
    }

    #endregion
}
