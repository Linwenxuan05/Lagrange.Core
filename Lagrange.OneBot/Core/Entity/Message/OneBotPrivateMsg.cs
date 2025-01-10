using System.Text.Json.Serialization;

namespace Lagrange.OneBot.Core.Entity.Message;

[Serializable]
public class OneBotPrivateMsg(uint selfId, OneBotSender groupSender, string subType, long time) : OneBotEntityBase(selfId, "message", time)
{
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "private";

    [JsonPropertyName("sub_type")] public string SubType { get; set; } = subType;

    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    
    [JsonPropertyName("user_id")] public uint UserId { get; set; }
    
    [JsonPropertyName("message")] public List<OneBotSegment> Message { get; set; } = new();

    [JsonPropertyName("raw_message")] public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("font")] public int Font { get; set; } = 0;

    [JsonPropertyName("sender")] public OneBotSender GroupSender { get; set; } = groupSender;

    [JsonPropertyName("target_id")] public uint TargetId { get; set; }
}

[Serializable]
public class OneBotPrivateStringMsg(uint selfId, OneBotSender groupSender, string subType, long time) : OneBotEntityBase(selfId, "message", time)
{
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "private";

    [JsonPropertyName("sub_type")] public string SubType { get; set; } = subType;

    [JsonPropertyName("message_id")] public int MessageId { get; set; }
    
    [JsonPropertyName("user_id")] public uint UserId { get; set; }
    
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;

    [JsonPropertyName("raw_message")] public string RawMessage { get; set; } = string.Empty;

    [JsonPropertyName("font")] public int Font { get; set; } = 0;

    [JsonPropertyName("sender")] public OneBotSender GroupSender { get; set; } = groupSender;

    [JsonPropertyName("target_id")] public uint TargetId { get; set; }
}
