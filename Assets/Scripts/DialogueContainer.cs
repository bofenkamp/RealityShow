using UnityEngine;

[CreateAssetMenu(fileName = "DialogueContainer", menuName = "Scriptable Objects/DialogueContainer")]
public class DialogueContainer : ScriptableObject
{
    [Header("Pre-Competition")]
    [TextArea(3, 10)]
    public string Intro;
    public string IntroducingYou;

    [Header("Challenges")]
    [TextArea(1, 10)]
    public string NewChallenge;
    [TextArea(1, 10)]
    public string ChallengeIntroduction;
    [TextArea(1, 10)]
    public string ChallengeOutcomesIntro;
    [TextArea(1, 10)]
    public string JudgeStarting;
    [TextArea(1, 10)]
    public string ChallengeResults;

    [Header("Pre-Elimination Chat")]
    [TextArea(1, 10)]
    public string PlayerChooseWhoToTalkTo;
    [TextArea(1, 10)]
    public string OpeningLineRequest;
    [TextArea(1, 10)]
    public string NpcInitiateConvoWithPlayer;
    [TextArea(1, 10)]
    public string ConversationLine;
    [TextArea(1, 10)]
    public string EndOfConversation;
    [TextArea(1, 10)]
    public string WaitForNpcToFinishTalking;

    [Header("Elimination")]
    [TextArea(1, 10)]
    public string ChooseEliminationVote;
    [TextArea(1, 10)]
    public string LoadingVotes;
    [TextArea(1, 10)]
    public string VoteRevealIntro;
    [TextArea(1, 10)]
    public string IndividualVote;
    [TextArea(1, 10)]
    public string VotingConclusion;
    [TextArea(1, 10)]
    public string PlayerEliminated;

    [Header("Final")]
    [TextArea(1, 10)]
    public string PlayerWonFinalChallenge;
    [TextArea(1, 10)]
    public string NpcChooseSecondFinalist;
    [TextArea(1, 10)]
    public string FinalistChoiceAnnouncement;
    [TextArea(1, 10)]
    public string FinalsIntro;
    [TextArea(1, 10)]
    public string WaitingForNpcFinalistSpeech;
    [TextArea(1, 10)]
    public string FinalistSpeech;
    [TextArea(1, 10)]
    public string EliminatedNpcAnnounceWinnerVote;
    [TextArea(1, 10)]
    public string EliminatedAddressesFinalist;
    [TextArea(1, 10)]
    public string WinnerAnnouncement;
}
