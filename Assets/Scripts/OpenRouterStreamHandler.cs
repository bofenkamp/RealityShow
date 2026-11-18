using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OpenRouterStreamHandler : DownloadHandlerScript
{
    private readonly Action<string> _onTextReceived;
    private readonly StringBuilder _lineBuffer;
    private readonly Decoder _decoder;

    public OpenRouterStreamHandler(Action<string> callback)
        : base(new byte[8192])
    {
        _onTextReceived = callback;
        _lineBuffer = new StringBuilder();
        _decoder = Encoding.UTF8.GetDecoder();
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0)
            return false;

        // Decode incrementally so UTF-8 characters split across
        // network packets are handled correctly.
        var chars = new char[Encoding.UTF8.GetMaxCharCount(dataLength)];

        int charCount = _decoder.GetChars(
            data, 0, dataLength, chars, 0, false
        );

        for (int i = 0; i < charCount; i++)
        {
            var c = chars[i];

            if (c == '\n')
            {
                ProcessLine(_lineBuffer.ToString());
                _lineBuffer.Clear();
            }
            else
            {
                _lineBuffer.Append(c);
            }
        }

        return true;
    }

    private void ProcessLine(string line)
    {
        line = line.Trim();

        // Ignore empty lines, SSE comments and other metadata.
        if (string.IsNullOrEmpty(line) ||
            line.StartsWith(":") ||
            !line.StartsWith("data:"))
            return;

        var json = line[5..].Trim();

        if (json == "[DONE]")
            return;

        try
        {
            var chunk = JsonUtility.FromJson<ChatStreamChunk>(json);

            if (chunk?.choices == null || chunk.choices.Length == 0)
                return;

            var content = chunk.choices[0].delta?.content;

            if (!string.IsNullOrEmpty(content))
                _onTextReceived?.Invoke(content);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not parse stream chunk: " + e.Message);
        }
    }
}