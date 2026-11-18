using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIChatController : MonoBehaviour
{
    public static AIChatController Instance;

    [Header("AI Settings")]
    [SerializeField] private string _apiURL;
    [SerializeField] private TextAsset _keyFile;
    public string Model;
    private string _apiKey;

    [Header("Dialogue")]
    [SerializeField] private PromptsContainer _prompts;
    [SerializeField] private DialogueContainer _dialogue;

    private void Start()
    {
        Instance = this;
        Debug.Log(Instance.gameObject.name);
        _apiKey = _keyFile.text.Trim();
    }

    public IEnumerator CharacterPrompt(string prompt, CharacterProfile character, Action<string> onComplete)
    {
        character.RebuildContext();
        character.chatHistory.AddMessage("user", prompt);
        var response = "";
        yield return SendPrompt(character.chatHistory, r => response = r, chunk => Debug.Log(chunk));
        character.chatHistory.AddMessage("assistant", response);
        onComplete?.Invoke(response);
    }

    public IEnumerator SideConversation(string dialogue, CharacterProfile speakingCharacter, 
        CharacterProfile listeningCharacter, Action<DialogueMessage> onComplete, bool isFirstMessage)
    {
        speakingCharacter.RebuildContext();
        var speakerMessageHistory = new List<ChatMessage>(speakingCharacter.chatHistory.messages);
        if (speakingCharacter.sideConversations.ContainsKey(listeningCharacter.first_name))
            speakerMessageHistory.AddRange(speakingCharacter.sideConversations[listeningCharacter.first_name]);
        else
            speakingCharacter.sideConversations[listeningCharacter.first_name] = new();

        if (!listeningCharacter.sideConversations.ContainsKey(speakingCharacter.first_name))
            listeningCharacter.sideConversations[speakingCharacter.first_name] = new();

        var messageForSpeaker = new ChatMessage(dialogue);
        var messageForListener = new ChatMessage("assistant", dialogue);
        speakerMessageHistory.Add(messageForSpeaker);
        speakingCharacter.sideConversations[listeningCharacter.first_name].Add(messageForSpeaker);
        listeningCharacter.sideConversations[speakingCharacter.first_name].Add(messageForListener);

        var request = new ChatRequest(speakerMessageHistory.ToArray());
        var prompt = ((isFirstMessage ? _prompts.ConversationInstructionsInitiate : "") + 
            _prompts.ConversationInstructions)
            .Replace("$nl", listeningCharacter.first_name)
            .Replace("$ns", speakingCharacter.first_name)
            .Replace("$d", dialogue);
        request.messages[^1].content = prompt;
        var response = "";
        yield return SendPrompt(request, r => response = r, null);
        var responseObj = JsonUtility.FromJson<DialogueMessage>(response);
        if (responseObj.conversation_status == "continue")
        {
            if (listeningCharacter.isPlayer)
                onComplete?.Invoke(responseObj);
            else
                yield return SideConversation(responseObj.dialogue, listeningCharacter, speakingCharacter, null, false);
        }
        else
        {
            var text = $"{responseObj.dialogue}\n{_dialogue.EndOfConversation.Replace("$n", speakingCharacter.first_name)}";
            speakingCharacter.sideConversations[listeningCharacter.first_name]
                .Add(new ChatMessage("assistant", text));
            listeningCharacter.sideConversations[speakingCharacter.first_name]
                .Add(new ChatMessage("user", text));
        }
        onComplete?.Invoke(responseObj);
    }

    public IEnumerator SinglePrompt(string prompt, Action<string> onComplete, Action<string> onChunk)
    {
        var request = new ChatRequest(new ChatMessage[] { new ChatMessage(prompt) });
        yield return SendPrompt(request, onComplete, onChunk);
    }

    public IEnumerator SendPrompt(ChatRequest request, Action<string> onComplete, Action<string> onChunk)
    {
        var json = JsonUtility.ToJson(request);
        var fullResponse = new StringBuilder();
        using var webRequest = CreateRequest(json);
        Debug.Log(json);

        webRequest.downloadHandler = new OpenRouterStreamHandler(
            chunk =>
            {
                fullResponse.Append(chunk);
                onChunk?.Invoke(chunk);
            }
        );

        yield return webRequest.SendWebRequest();

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(webRequest.error);
            onComplete.Invoke("");
            yield break;
        }

        var responseJson = fullResponse.ToString();// webRequest.downloadHandler.text.Trim();
        Debug.Log(responseJson);
        onComplete.Invoke(responseJson);

        //var responseObject = JsonUtility.FromJson<AIResponse>(responseJson);
        //Debug.Log(responseObject.choices[0].message.content);
        //onComplete.Invoke(responseObject.choices[0].message.content);
    }

    private UnityWebRequest CreateRequest(string json)
    {
        var request = new UnityWebRequest(_apiURL, "POST");
        var bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {_apiKey}");
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}
