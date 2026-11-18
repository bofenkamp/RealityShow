using System;
using System.Linq;
using UnityEngine;
using Object = System.Object;

#region AI Input

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;

    public ChatMessage(string content)
    {
        role = "user";
        this.content = content;
    }

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public Reasoning reasoning;
    public bool stream;

    public ChatRequest(ChatMessage[] messages)
    {
        model = AIChatController.Instance == null ? "" : AIChatController.Instance.Model;
        this.messages = messages;
        reasoning = new Reasoning();
        stream = true;
    }

    public void AddMessage(string role, string content)
    {
        var newMessage = new ChatMessage(role, content);
        var messageList = messages.ToList();
        messageList.Add(newMessage);
        messages = messageList.ToArray();
    }
}

[Serializable]
public class Reasoning
{
    public bool enabled = true;
}

#endregion

#region Streaming

[Serializable]
public class ChatStreamChunk
{
    public ChatStreamChoice[] choices;
}

[Serializable]
public class ChatStreamChoice
{
    public ChatStreamDelta delta;
    public string finish_reason;
}

[Serializable]
public class ChatStreamDelta
{
    public string content;
}

#endregion

#region AI Output

[Serializable]
public class AIResponse
{
    public string id;
    public string @object;
    public long created;
    public string model;
    public string provider;
    public Object system_fingerprint;
    public Object service_tier;
    public Choice[] choices;
    public Usage usage;
}

[Serializable]
public class Choice
{
    public int index;
    public Object longprobs;
    public string finish_reason;
    public string native_finish_reason;
    public Message message;
}

[Serializable]
public class Message
{
    public string role;
    public string content;
    public Object refusal;
    public string reasoning;
    public ReasoningDetails reasoning_details;
}

[Serializable]
public class ReasoningDetails
{
    public string type;
    public string text;
    public string format;
    public int index;
}

[Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
    public float cost;
    public bool is_byok;
    public TokenDetails prompt_tokens_details;
    public CostDetails cost_details;
    public CompletionDetails completion_tokens_details;
}

[Serializable]
public class TokenDetails
{
    public int cached_tokens;
    public int cache_write_tokens;
    public int audio_tokens;
    public int video_tokens;
}

[Serializable]
public class CostDetails
{
    public float upstream_inference_cost;
    public float upstream_inference_prompt_cost;
    public float upstream_inference_completions_cost;
}

[Serializable]
public class CompletionDetails
{
    public int reasoning_tokens;
    public int image_tokens;
    public int audio_tokens;
}

#endregion

#region Character Creation

[Serializable]
public class BasicProfilesSummary
{
    public BasicProfile[] students;
}

[Serializable]
public class BasicProfile
{
    public string gender;
    public string hometown;
    public string year;
    public string first_name;
    public string last_name;
}

[Serializable]
public class CharacterProfiles
{
    public CharacterProfile[] students;
}

[Serializable]
public class ChallengerProfile
{
    public string full_name;
    public string referred_name;
    public string gender;
    public string hometown;
    public string department;
    public string physical_description;
    public string background;
    public string personality;
    public string perceived_personality;
    public string challenge_announcement;
    public string challenge_summary;

    public string GetBio(bool includeFullName = true, bool includeGender = true,
        bool includeHometown = true, bool includeDepartment = true,
        bool includePhysical = true, bool includeBackground = true, 
        bool includePersonality = true)
    {
        var result = includeFullName ? full_name : referred_name;
        result += $"\n{(includeGender ? $"Gender: {gender}\n" : "")}";
        result += includeDepartment ? $"Teaches: {department}\n" : "";
        result += includeHometown ? $"Hometown: {hometown}\n" : "";
        result += includePhysical ? $"{physical_description}\n" : "";
        result += includeBackground ? $"{background}\n" : "";
        result += includePersonality ? $"{personality}\n" : "";
        return result;
    }
}

#endregion

#region Challenges

[Serializable]
public class ChallengeResult
{
    public ChallengeFeedback[] feedback;
    public string winner_announcement;
    public string winner_name;
}

[Serializable]
public class ChallengeFeedback
{
    public string contestant_name;
    public string review;
}

#endregion

#region Conversation

[Serializable]
public class ConversationInitiation
{
    public string name;
    public string opening_line;
}

[Serializable]
public class DialogueMessage
{
    public string dialogue;
    public string conversation_status;
}

#endregion

#region

[Serializable]
public class FinalsVote
{
    public string name;
    public FinalistMessage[] messages;
    public string vote_announcement;
    public string vote;
}

[Serializable]
public class FinalistMessage
{
    public string name;
    public string message;
}

#endregion