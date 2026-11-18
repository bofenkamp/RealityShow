using UnityEngine;

[CreateAssetMenu(fileName = "InternalMemoryHelper", menuName = "Scriptable Objects/InternalMemoryHelper")]
public class InternalMemoryHelper : ScriptableObject
{
    [Header("Challenges")]
    [TextArea(1, 10)] public string ChallengeResultSummary;

    [Header("Eliminations")]
    [TextArea(1, 10)] public string EliminationHeader;
}
