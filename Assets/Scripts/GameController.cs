using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    [Header("External")]
    [SerializeField] private UIController _uiCont;
    [SerializeField] private CharacterCreator _charCreator;

    [Header("Data")]
    private CharacterProfile _player;
    private List<CharacterProfile> _activeCharacters;
    private List<CharacterProfile> _eliminatedCharacters;
    [HideInInspector] public ChallengerProfile[] Challengers;
    private Dictionary<string, string>[] _challengeResults;
    private string _currChallengeWinner;
    private Dictionary<string, string>[] _eliminationChoices;

    [Header("Progression")]
    private bool _showedIntroText;
    private bool _playerSet;
    private bool _npcsSet;
    private Dictionary<string, bool> _isConversationOngoing;
    private Queue<ConversationInitiation> _waitingToTalkToPlayer;
    private Dictionary<string, string> _finalistSpeeches;
    private Dictionary<string, int> _finalsVotes;

    [Header("Dialogue")]
    [SerializeField] private DialogueContainer _dialogue;
    [SerializeField] private PromptsContainer _prompts;
    [SerializeField] private InternalMemoryHelper _memoryTexts;

    [Header("Testing")]
    [SerializeField] private bool _generateChallenges;

    #region Setup

    private void Start()
    {
        Instance = this;
        _activeCharacters = new();
        _eliminatedCharacters = new();
        _challengeResults = new Dictionary<string, string>[_charCreator.CharacterCount + 2];
        _eliminationChoices = new Dictionary<string, string>[_charCreator.CharacterCount + 2];
        _isConversationOngoing = new();
        _waitingToTalkToPlayer = new();
        for (int i = 3; i < _challengeResults.Length; i++)
            _challengeResults[i] = new();
        if (_generateChallenges)
            StartCoroutine(GenerateChallenges());
    }

    public void SetPlayerName(string forename, string surname)
    {
        _player = new();
        _player.isPlayer = true;
        _player.first_name = forename;
        _player.last_name = surname;
    }

    public void SetPlayerDetails(string gender, string year, string hometown, string major,
        string physicalAppearance, string background, string personality, string perceivedPersonality)
    {
        _player.gender = gender;
        _player.year = year;
        _player.hometown = hometown;
        _player.major = major;
        _player.physical_description = physicalAppearance;
        _player.background = background;
        _player.personality = personality;
        _activeCharacters.Add(_player);
        _playerSet = true;
        if (_npcsSet)
            StartCompetition();
        StartCoroutine(GetPlayerFirstImpressions(perceivedPersonality));
    }

    public void AddNPCs(CharacterProfiles npcs)
    {
        foreach (var npc in npcs.students)
        {
            _activeCharacters.Add(npc);
            Debug.Log(npc);
        }
        _npcsSet = true;
        if (_playerSet)
            StartCompetition();
    }

    private IEnumerator GetPlayerFirstImpressions(string attemptedBehavior)
    {
        var bio = _player.GetBio(includeYear:false, includeStats: false, includePerceived: false);
        var prompt = _prompts.FirstImpressions
            .Replace("$f", _player.first_name)
            .Replace("$y", _player.year)
            .Replace("$b", bio)
            .Replace("$p", attemptedBehavior);
        string perceptionText = "";
        yield return AIChatController.Instance.SinglePrompt(prompt, t => perceptionText = t, c => Debug.Log(c));
        _player.perceived_personality = perceptionText;
    }

    private IEnumerator GenerateChallenges()
    {
        yield return null;
        int playerCount = _charCreator.CharacterCount + 1;
        Challengers = new ChallengerProfile[playerCount + 1];

        for (int i = playerCount; i > 2; i--)
        {
            var prof = new ChallengerProfile();
            yield return _charCreator.CreateChallenger(i, p => prof = p);
            Challengers[i] = prof;
        }
    }

    #endregion

    #region Intro

    public void ShowIntroText()
    {
        if (_showedIntroText)
            return;

        var introText = _dialogue.Intro
            .Replace("$", _player.first_name)
            .Replace("%", _player.PossessivePronoun);
        _uiCont.AddText(introText);
        _showedIntroText = true;
    }

    private void StartCompetition()
    {
        if (!_showedIntroText)
            ShowIntroText();

        foreach (var character in _activeCharacters)
        {
            if (character.isPlayer)
                continue;

            var charText = character.GetBio(includeStats: false, includeBackground: false, includePersonality: false);

            _uiCont.AddText($"{charText}\n");
        }

        StartCoroutine(IntroducePlayer());
    }

    private IEnumerator IntroducePlayer()
    {
        _uiCont.AddText("\n");
        _uiCont.AddText(_dialogue.IntroducingYou);
        _uiCont.AddText("\n\n");
        _uiCont.AddText(_player.GetBio(includeStats: false, includePerceived: false));
        _uiCont.AddText("\n\n");

        yield return new WaitUntil(() => !string.IsNullOrEmpty(_player.perceived_personality));

        _uiCont.AddText(_player.perceived_personality);

        StartCoroutine(StartNextChallenge());
    }

    #endregion

    #region Main Game Loop

    private IEnumerator StartNextChallenge()
    {
        var text = $"\n\n{_dialogue.NewChallenge}".Replace("$", (_eliminatedCharacters.Count + 1).ToString());

        _uiCont.AddText(text);

        yield return new WaitUntil(() => Challengers[_activeCharacters.Count] != null &&
            !string.IsNullOrEmpty(GetCurrentChallengeDescription()));

        var currChallenge = Challengers[_activeCharacters.Count];

        text = _dialogue.ChallengeIntroduction
            .Replace("$f", currChallenge.full_name)
            .Replace("$d", currChallenge.department)
            .Replace("$pd", currChallenge.physical_description)
            .Replace("$pp", currChallenge.perceived_personality)
            .Replace("$n", currChallenge.referred_name)
            .Replace("$a", currChallenge.challenge_announcement)
            .Replace("$y", _player.first_name);

        _uiCont.AddText(text);

        _uiCont.AllowTextInput(t => ProcessChallengeSubmission(t));
        GiveNPCsChallenge();
    }

    private void GiveNPCsChallenge()
    {
        var challengeGiver = Challengers[_activeCharacters.Count];
        var promptTemplate = _prompts.GiveNpcChallenge
            .Replace("$tf", challengeGiver.full_name)
            .Replace("$d", challengeGiver.department)
            .Replace("$pd", challengeGiver.physical_description)
            .Replace("$pp", challengeGiver.perceived_personality)
            .Replace("$n", challengeGiver.referred_name)
            .Replace("$a", challengeGiver.challenge_announcement);

        foreach (var character in _activeCharacters)
        {
            if (character.isPlayer)
                continue;

            var prompt = promptTemplate.Replace("$f", character.first_name);
            StartCoroutine(AIChatController.Instance.CharacterPrompt(prompt, character,
                result => _challengeResults[_activeCharacters.Count][character.first_name] = result));
        }
    }

    private void ProcessChallengeSubmission(string attemptSummary)
    {
        var challengeDescription = GetCurrentChallengeDescription();
        var bio = _player.GetBio(includeGender: false, includeYear: false,
            includeStats: false, includePerceived: false);

        var prompt = _prompts.PlayerChallengeAttempt
            .Replace("$f", _player.first_name)
            .Replace("$y", _player.year)
            .Replace("$po", _player.ObjectPronoun)
            .Replace("$ps", _player.SubjectPronoun)
            .Replace("$pp", _player.PossessivePronoun)
            .Replace("$c", challengeDescription)
            .Replace("$b", bio)
            .Replace("$a", attemptSummary);

        StartCoroutine(AIChatController.Instance.SinglePrompt(prompt, 
            t => _challengeResults[_activeCharacters.Count][_player.first_name] = t, null));
        StartCoroutine(DisplayChallengeResults());
    }

    private IEnumerator DisplayChallengeResults()
    {
        var fullRecap = "";

        var outcomesIntro = _dialogue.ChallengeOutcomesIntro;
        _uiCont.AddText(outcomesIntro);
        fullRecap += outcomesIntro;

        var undisplayedCharacters = new HashSet<string>();
        foreach (var character in _activeCharacters)
            undisplayedCharacters.Add(character.first_name);

        var challengeDict = _challengeResults[_activeCharacters.Count];
        var challenger = Challengers[_activeCharacters.Count];

        while (undisplayedCharacters.Count > 0)
        {
            foreach (var character in undisplayedCharacters)
            {
                if (challengeDict.ContainsKey(character) && !string.IsNullOrEmpty(challengeDict[character]))
                {
                    var text = $"{character}:\n{challengeDict[character]}\n\n";
                    _uiCont.AddText(text);
                    fullRecap += text;
                    undisplayedCharacters.Remove(character);
                }
            }
            yield return null;
        }

        var judgeStartingText = _dialogue.JudgeStarting.Replace("$n", challenger.referred_name);
        _uiCont.AddText(judgeStartingText);
        fullRecap += judgeStartingText;

        var resultsSummary = "";
        foreach (var pair in challengeDict.OrderBy(x => Random.value).ToArray())
            resultsSummary += $"{pair.Key}:\n{pair.Value}\n\n";
        resultsSummary = resultsSummary.Trim('\n');

        var bio = challenger.GetBio(includeDepartment: false);

        var prompt = _prompts.GetChallengeJudgment
            .Replace("$fn", challenger.full_name)
            .Replace("$d", challenger.department)
            .Replace("$n", challenger.referred_name)
            .Replace("$a", challenger.challenge_announcement)
            .Replace("$s", resultsSummary)
            .Replace("$b", bio);
        var judgeJson = "";
        yield return AIChatController.Instance.SinglePrompt(prompt, json => judgeJson = json, null);

        var resultsObj = JsonUtility.FromJson<ChallengeResult>(judgeJson);

        var feedback = "";
        foreach (var competitor in resultsObj.feedback)
            feedback += $"{competitor.contestant_name}:\n{competitor.review}\n\n";
        feedback = feedback.Trim('\n');

        var resultText = _dialogue.ChallengeResults
            .Replace("$n", challenger.referred_name)
            .Replace("$f", feedback)
            .Replace("$a", resultsObj.winner_announcement)
            .Replace("$w", resultsObj.winner_name);
        _currChallengeWinner = resultsObj.winner_name;

        _uiCont.AddText(resultText);
        fullRecap += resultText;

        var memoryUpdate = _memoryTexts.ChallengeResultSummary
            .Replace("$#", (_eliminatedCharacters.Count + 1).ToString())
            .Replace("$s", challenger.challenge_summary)
            .Replace("$w", resultsObj.winner_name);
        foreach (var character in GetAllCharacters())
            character.AddToHistory(memoryUpdate);

        if (_activeCharacters.Count > 3)
            InitiateConversations(fullRecap);
        else
            ConductEliminations();
    }

    private void InitiateConversations(string results)
    {
        if (_activeCharacters.Contains(_player))
            _isConversationOngoing[_player.first_name] = true;

        foreach (var character in _activeCharacters)
        {
            if (character.isPlayer)
            {
                PlayerChooseConversation();
            }
            else
            {
                StartCoroutine(NpcInitiateConversation(character, results));
            }
        }
    }

    private IEnumerator NpcInitiateConversation(CharacterProfile initiator, string resultsText)
    {
        _isConversationOngoing[initiator.first_name] = true;
        var characterOptionsList = string.Join(", ", _activeCharacters.Where(c => c != initiator).Select(c => c.first_name));

        var choosePersonPrompt = _prompts.InitiateConversation
            .Replace("$r", resultsText)
            .Replace("$n", initiator.first_name)
            .Replace("$pp", initiator.PossessivePronoun)
            .Replace("$c", characterOptionsList);

        var conversationOpenerResponse = "";
        yield return AIChatController.Instance.CharacterPrompt(choosePersonPrompt, initiator, 
            r => conversationOpenerResponse = r);
        var opener = JsonUtility.FromJson<ConversationInitiation>(conversationOpenerResponse);

        if (opener.name == "Nobody")
        {
            _isConversationOngoing[initiator.first_name] = false;
            yield break;
        }
        else if (opener.name == _player.first_name)
        {
            _waitingToTalkToPlayer.Enqueue(opener);
            yield break;
        }

        var other = GetCharacterWithName(opener.name);
        yield return AIChatController.Instance.SideConversation(opener.opening_line, 
            initiator, other, _ => _isConversationOngoing[initiator.first_name] = false, true);
    }

    private void PlayerChooseConversation()
    {
        _uiCont.AddText(_dialogue.PlayerChooseWhoToTalkTo
            .Replace("$n", _player.first_name)
            .Replace("$pp", _player.PossessivePronoun));

        var conversationOptions = _activeCharacters.Where(c => !c.isPlayer)
            .Select(c => c.first_name).ToList();
        conversationOptions.Add("Nobody");
        _uiCont.ShowButtonOptions(conversationOptions.ToArray(), 
            s => StartCoroutine(PlayerStartConversation(s)));
    }

    private IEnumerator PlayerStartConversation(string partner)
    {
        if (partner == "Nobody")
        {
            OnPlayerFinishesConversation();
            yield break;
        }

        var openingLineRequest = _dialogue.OpeningLineRequest
            .Replace("$n", _player.first_name)
            .Replace("$o", partner);

        _uiCont.AddText(openingLineRequest);

        var character = GetCharacterWithName(partner);
        _uiCont.AllowTextInput(line => StartCoroutine(PlayerConversation(character, line, true)));
    }

    private IEnumerator PlayerConversation(CharacterProfile other, string playerLine, bool isFirstLine)
    {
        if (string.IsNullOrEmpty(playerLine))
        {
            _uiCont.AddText(_dialogue.EndOfConversation.Replace("$n", _player.first_name));
            other.sideConversations[_player.first_name][^1].content +=
                "\n" + _dialogue.EndOfConversation.Replace("$n", _player.first_name);
            OnPlayerFinishesConversation();
            yield break;
        }

        _uiCont.AddText(_dialogue.ConversationLine
            .Replace("$n", _player.first_name)
            .Replace("$l", playerLine));

        DialogueMessage responseJson = null;
        yield return AIChatController.Instance.SideConversation(playerLine, other, _player, r => responseJson = r, isFirstLine);

        _uiCont.AddText(_dialogue.ConversationLine
            .Replace("$n", other.first_name)
            .Replace("$l", responseJson.dialogue));

        if (responseJson.conversation_status == "continue")
        {
            _uiCont.AllowChatInput(s => StartCoroutine(PlayerConversation(other, s, false)));
        }
        else
        {
            _uiCont.AddText(_dialogue.EndOfConversation.Replace("$n", other.first_name));
            OnPlayerFinishesConversation();
            yield break;
        }
    }

    private void OnPlayerFinishesConversation()
    {
        if (_waitingToTalkToPlayer.Count == 0)
        {
            _isConversationOngoing[_player.first_name] = false;
            if (!AllConversationsFinished())
                StartCoroutine(WaitForNpcConvosToFinish());
            else
                ConductEliminations();
        }
        else
        {
            var newConvo = _waitingToTalkToPlayer.Dequeue();
            _uiCont.AddText(_dialogue.NpcInitiateConvoWithPlayer
                .Replace("$o", newConvo.name));
            var newPartner = GetCharacterWithName(newConvo.name);
            _uiCont.AllowChatInput(s => StartCoroutine(PlayerConversation(newPartner, s, false)));
        }
    }

    private IEnumerator WaitForNpcConvosToFinish()
    {
        _uiCont.AddText(_dialogue.WaitForNpcToFinishTalking);

        yield return new WaitUntil(() => AllConversationsFinished());

        ConductEliminations();
    }

    private void ConductEliminations()
    {
        foreach (var character in GetAllCharacters())
            character.StoreSideConversationsInMemory();

        if (_activeCharacters.Count == 3)
        {
            StartCoroutine(WinnerChooseFinalist());
            return;
        }

        var upForElimination = new List<CharacterProfile>();
        foreach (var character in _activeCharacters.OrderBy(x => Random.value))
            if (character.first_name != _currChallengeWinner)
                upForElimination.Add(character);

        foreach (var character in _activeCharacters)
        {
            var options = upForElimination.ToList();
            options.Remove(character);

            if (character.isPlayer)
            {
                _uiCont.AddText(_dialogue.ChooseEliminationVote);
                _uiCont.ShowButtonOptions(options.Select(o => o.first_name).ToArray(),
                    v => _eliminationChoices[_activeCharacters.Count][character.first_name] = v);
            }
            else
            {
                var prompt = _prompts.ChooseWhoToEliminate
                    .Replace("$n", character.first_name)
                    .Replace("$e", string.Join(", ", upForElimination))
                    .Replace("$ps", character.SubjectPronoun)
                    .Replace("$w", _currChallengeWinner)
                    .Replace("$o", string.Join(", ", options));

                StartCoroutine(AIChatController.Instance.CharacterPrompt(prompt, character, r =>
                    _eliminationChoices[_activeCharacters.Count][character.first_name] = r));
            }
        }

        StartCoroutine(ProcessEliminationVotes());
    }

    private IEnumerator ProcessEliminationVotes()
    {
        var votingHistory = _memoryTexts.EliminationHeader;

        if (_eliminationChoices[_activeCharacters.Count].Count < _activeCharacters.Count)
        {
            _uiCont.AddText(_dialogue.LoadingVotes);
            yield return new WaitUntil(() => _eliminationChoices[_activeCharacters.Count].Count == _activeCharacters.Count);
        }

        _uiCont.AddText(_dialogue.VoteRevealIntro);
        var votes = new Dictionary<string, int>();

        foreach (var pair in _eliminationChoices[_activeCharacters.Count].OrderBy(x => Random.value))
        {
            var voteRecipient = pair.Value;
            var voteReport = _dialogue.IndividualVote
                .Replace("$n", pair.Key)
                .Replace("$v", voteRecipient);
            _uiCont.AddText(voteReport);
            votingHistory += voteReport;
            if (votes.ContainsKey(voteRecipient))
                votes[voteRecipient]++;
            else
                votes[voteRecipient] = 1;
        }

        var maxVotes = 0;
        var mostVoted = string.Empty;

        //TO DO: Handle ties
        foreach (var pair in votes)
        {
            if (pair.Value > maxVotes)
            {
                mostVoted = pair.Key;
                maxVotes = pair.Value;
            }
        }

        votingHistory += _dialogue.VotingConclusion.Replace("$l", mostVoted);
        foreach (var character in GetAllCharacters())
            character.AddToHistory(votingHistory.Trim('\n'));

        EliminatePlayer(mostVoted);
    }

    private void EliminatePlayer(string loser)
    {
        _uiCont.AddText(_dialogue.VotingConclusion.Replace("$l", loser));

        var player = GetCharacterWithName(loser);
        _eliminatedCharacters.Add(player);
        _activeCharacters.Remove(player);

        if (loser == _player.first_name)
        {
            _uiCont.AddText(_dialogue.PlayerEliminated
                .Replace("$n", _player.first_name));
            return;
        }

        if (_activeCharacters.Count > 2)
            StartCoroutine(StartNextChallenge());
        else
            StartFinal();
    }

    #endregion

    #region Finals

    private IEnumerator WinnerChooseFinalist()
    {
        if (_currChallengeWinner == _player.first_name)
        {
            var text = _dialogue.PlayerWonFinalChallenge
                .Replace("$n", _player.first_name)
                .Replace("$po", _player.ObjectPronoun);
            _uiCont.AddText(text);
            _uiCont.ShowButtonOptions(_activeCharacters.Where(c => !c.isPlayer).Select(c => c.first_name).ToArray(),
                s => ProcessFinalistChoice(s));
        }
        else
        {
            var autoqualifier = GetCharacterWithName(_currChallengeWinner);
            var options = _activeCharacters.Where(c => c.first_name != _currChallengeWinner)
                .Select(c => c.first_name).OrderBy(x => Random.value).ToArray();
            var optionText = $"{options[0]} or {options[1]}";
            var prompt = _prompts.ChooseOtherFinalist
                .Replace("$w", autoqualifier.first_name)
                .Replace("$o", optionText);
            var result = "";
            _uiCont.AddText(_dialogue.NpcChooseSecondFinalist.Replace("$w", autoqualifier.first_name));
            yield return AIChatController.Instance.CharacterPrompt(prompt, autoqualifier, r => result = r);
            ProcessFinalistChoice(result);
        }
        yield return null;
    }

    private void ProcessFinalistChoice(string otherFinalist)
    {
        _uiCont.AddText(_dialogue.FinalistChoiceAnnouncement
            .Replace("$w", _currChallengeWinner)
            .Replace("$f", otherFinalist)
            .Replace("$po", GetCharacterWithName(_currChallengeWinner).ObjectPronoun));

        foreach (var character in _activeCharacters)
        {
            if (character.first_name != _currChallengeWinner &&
                character.first_name != otherFinalist)
                EliminatePlayer(character.first_name);
        }
    }

    private void StartFinal()
    {
        _finalistSpeeches = new();
        foreach (var character in _activeCharacters)
            _finalistSpeeches[character.first_name] = "";

        GetNpcSpeech();

        var finalists = string.Join(" and ", _activeCharacters.Select(c => c.first_name));

        _uiCont.AddText(_dialogue.FinalsIntro
            .Replace("$f", finalists)
            .Replace("$p", _player.first_name));

        _uiCont.AllowTextInput(s => ProcessPlayerSpeech(s));
    }

    private void GetNpcSpeech()
    {
        foreach (var finalist in _activeCharacters)
        {
            if (finalist.isPlayer)
                continue;

            var other = string.Join(" or ", _activeCharacters
                .Where(c => c.first_name != finalist.first_name).Select(c => c.first_name));
            var prompt = _prompts.NpcFinalistSpeech
                .Replace("$n", finalist.first_name)
                .Replace("$o", other);

            StartCoroutine(AIChatController.Instance.CharacterPrompt(prompt, finalist, r =>
                _finalistSpeeches[finalist.first_name] = r));
        }
    }

    private void ProcessPlayerSpeech(string playerSpeech)
    {
        _finalistSpeeches[_player.first_name] = playerSpeech;
        StartCoroutine(Final());
    }

    private void DisplaySpeech(string finalist)
    {
        var text = _dialogue.FinalistSpeech
            .Replace("$n", finalist)
            .Replace("$s", _finalistSpeeches[finalist]);
        _uiCont.AddText(text);
    }

    private IEnumerator Final()
    {
        DisplaySpeech(_player.first_name);
        var npcFinalist = _activeCharacters.Where(c => !c.isPlayer).ToArray()[0];
        _finalsVotes = new();
        foreach (var finalist in _activeCharacters)
            _finalsVotes[finalist.first_name] = 0;

        if (!AllFinalistSpeechesSubmitted())
        {
            _uiCont.AddText(_dialogue.WaitingForNpcFinalistSpeech.Replace("$n", npcFinalist.first_name));
            yield return new WaitUntil(() => AllFinalistSpeechesSubmitted());
        }

        DisplaySpeech(npcFinalist.first_name);

        foreach (var eliminated in _eliminatedCharacters)
        {
            var speeches = string.Join("\n\n", _finalistSpeeches
               .Select(pair => _dialogue.FinalistSpeech.Replace("$n", pair.Key)
               .Replace("$s", pair.Value).OrderBy(x => Random.value)));
            var options = string.Join(" or ", _activeCharacters.OrderBy(x => Random.value).Select(c => c.first_name));
            string prompt = _prompts.ChooseWinnerVote
               .Replace("$o", options)
               .Replace("$n", eliminated.first_name)
               .Replace("$s", speeches);
            StartCoroutine(AIChatController.Instance.CharacterPrompt(prompt, eliminated, v => ReceiveVote(v)));
        }

        yield return new WaitUntil(() => _finalsVotes.Select(p => p.Value).Sum() == _eliminatedCharacters.Count);

        DeclareWinner();
    }

    private void ReceiveVote(string voteJson)
    {
        var voteObj = JsonUtility.FromJson<FinalsVote>(voteJson);

        var feedback = string.Join("\n\n", voteObj.messages.Select(m => _dialogue.EliminatedAddressesFinalist
            .Replace("$n", voteObj.name).Replace("$w", m.name).Replace("$f", m.message)));

        var text = _dialogue.EliminatedNpcAnnounceWinnerVote
            .Replace("$n", voteObj.name)
            .Replace("$f", feedback)
            .Replace("$a", voteObj.vote_announcement)
            .Replace("$w", voteObj.vote);

        _finalsVotes[voteObj.vote]++;
    }

    private void DeclareWinner()
    {
        var mostVotes = 0;
        var currWinner = "";

        foreach (var pair in _finalsVotes)
        {
            if (pair.Value > mostVotes)
            {
                currWinner = pair.Key;
                mostVotes = pair.Value;
            }
        }

        _currChallengeWinner = currWinner;

        var winText = _dialogue.WinnerAnnouncement
            .Replace("$#", mostVotes.ToString())
            .Replace("$t", _eliminatedCharacters.Count.ToString())
            .Replace("$w", currWinner);

        _uiCont.AddText(winText);
    }

    #endregion

    #region Helper Functions

    public List<CharacterProfile> GetAllCharacters()
    {
        var all = new List<CharacterProfile>(_activeCharacters);
        all.AddRange(_eliminatedCharacters);
        return all.OrderBy(x => Random.value).ToList();
    }

    private string GetCurrentChallengeDescription()
    {
        return Challengers[_activeCharacters.Count].challenge_announcement;
    }

    private CharacterProfile GetCharacterWithName(string firstName)
    {
        foreach (var character in _activeCharacters)
            if (character.first_name == firstName)
                return character;
        foreach (var character in _eliminatedCharacters)
            if (character.first_name == firstName)
                return character;
        return null;
    }

    private bool AllConversationsFinished()
    {
        foreach (var pair in _isConversationOngoing)
            if (pair.Value)
                return false;

        return true;
    }

    private bool AllFinalistSpeechesSubmitted()
    {
        foreach (var pair in _finalistSpeeches)
            if (string.IsNullOrEmpty(pair.Value))
                return false;

        return true;
    }

    #endregion
}
