using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CharacterProfile
{
    public string first_name;
    public string last_name;
    public string gender;
    public string year;
    public string hometown;
    public string major;
    public int likability;
    public int status;
    public int energy;
    public string physical_description;
    public string background;
    public string personality;
    public string perceived_personality;
    public bool isPlayer;
    public string SubjectPronoun => GetSubjectPronoun();
    public string ObjectPronoun => GetObjectPronoun();
    public string PossessivePronoun => GetPossessivePronoun();
    public ChatRequest chatHistory;
    private string _challengeHistory;
    public Dictionary<string, List<ChatMessage>> sideConversations;

    public CharacterProfile()
    {
        chatHistory = new ChatRequest(new ChatMessage[] { new ChatMessage("system", "") });
        sideConversations = new();
    }

    public void RebuildContext()
    {
        var text = $"You are {first_name} {last_name}. You are a college {year} from {hometown} " +
            $"whose major is {major} at a large public college in Los Angeles. {physical_description} " +
            $"{background} {personality} On a scale of 0 to 10 (0 being the least and 10 being " +
            $"the most, your likability is a {likability}, your status (or at least what you think " +
            $"your status is) is a {status}, and your energy level is a {energy}. You are competing " +
            $"in an elimination-style reality competition at your college. The competition consists of " +
            $"a series of challenges, created and judged by professors at the college. After each challenge, " +
            $"the professor will pick one winner, who receives immunity for this round, then each competitor " +
            $"publicly votes for one of the other competitors who didn't win to get eliminated. Whichever " +
            $"competitor received the most votes is eliminated from the competition. Here is a summary of the " +
            $"other competitors:\n";

        var allChars = GameController.Instance.GetAllCharacters();
        allChars.Remove(this);

        foreach (var character in allChars.OrderBy(x => Random.value))
            text += $"{character.first_name} {character.last_name}: {character.first_name} is a " +
                $"{character.year} from {character.hometown} studying {character.major}. " +
                $"{character.physical_description} {character.perceived_personality}\n";

        if (!string.IsNullOrEmpty(_challengeHistory))
            text += $"\nHistory of the competition so far:\n{_challengeHistory}";

        chatHistory.messages[0].content = text;
    }

    public void AddToHistory(string addition)
    {
        if (!string.IsNullOrEmpty(_challengeHistory))
            _challengeHistory += "\n\n";

        _challengeHistory += addition;
    }

    public void StoreSideConversationsInMemory()
    {
        var memory = chatHistory.messages.ToList();
        foreach (var pair in sideConversations)
        {
            memory.AddRange(pair.Value);
            pair.Value.Clear();
        }
    }

    public override string ToString()
    {
        return GetBio();
    }

    public string GetBio(bool includeLastName = true, bool includeGender = true,
        bool includeYear = true, bool includeHometown = true, bool includeMajor = true,
        bool includeStats = true, bool includePhysical = true, bool includeBackground = true,
        bool includePersonality = true, bool includePerceived = true)
    {
        var text = first_name;
        text += includeLastName ? $" {last_name}\n" : "\n";
        text += includeGender ? $"Gender: {gender}\n" : "";
        text += includeYear ? $"Year: {year}\n" : "";
        text += includeHometown ? $"Hometown: {hometown}\n" : "";
        text += includeMajor ? $"Major: {major}\n" : "";
        text += includeStats && !isPlayer ? $"Likability: {likability}/10\n" +
            $"How high status they think they are: {status}/10\n" +
            $"Energy: {energy}\n" : "";
        text += includePhysical ? physical_description + "\n" : "";
        text += includeBackground ? background + "\n" : "";
        text += includePersonality ? personality + "\n" : "";
        text += includePerceived ? perceived_personality + "\n" : "";
        return text;
    }

    private string GetSubjectPronoun()
    {
        return gender.ToLower() switch
        {
            "male" => "he",
            "female" => "she",
            _ => "they"
        };
    }

    private string GetObjectPronoun()
    {
        return gender.ToLower() switch
        {
            "male" => "him",
            "female" => "her",
            _ => "them"
        };
    }

    private string GetPossessivePronoun()
    {
        return gender.ToLower() switch
        {
            "male" => "his",
            "female" => "her",
            _ => "their"
        };
    }
}
