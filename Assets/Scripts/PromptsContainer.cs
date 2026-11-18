using UnityEngine;

[CreateAssetMenu(fileName = "PromptsContainer", menuName = "Scriptable Objects/PromptsContainer")]
public class PromptsContainer : ScriptableObject
{
    [Header("Character Creation")]
    [TextArea(1, 10)] public string DecideNamesHometownsPrefix;
    [TextArea(1, 10)] public string ExtendedProfilesPrefix;
    [TextArea(1, 10)] public string ProfessorGenPrefix;
    [TextArea(1, 10)] public string ExistingProfs;

    [Header("Pre-Challenge")]
    [TextArea(1, 10)] public string FirstImpressions;

    [Header("Challenges")]
    [TextArea(1, 10)] public string PlayerChallengeAttempt;
    [TextArea(1, 10)] public string GiveNpcChallenge;
    [TextArea(1, 10)] public string GetChallengeJudgment;

    [Header("Conversation")]
    [TextArea(1, 10)] public string InitiateConversation;
    [TextArea(1, 10)] public string ConversationInstructionsInitiate;
    [TextArea(1, 10)] public string ConversationInstructions;

    [Header("Elimination")]
    [TextArea(1, 10)] public string ChooseWhoToEliminate;

    [Header("Finals")]
    [TextArea(1, 10)] public string ChooseOtherFinalist;
    [TextArea(1, 10)] public string NpcFinalistSpeech;
    [TextArea(1, 10)] public string ChooseWinnerVote;
}
