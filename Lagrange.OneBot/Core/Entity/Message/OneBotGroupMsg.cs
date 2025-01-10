using System.Text.Json.Serialization;
using Lagrange.Core.Common.Entity;

namespace Lagrange.OneBot.Core.Entity.Message;

[Serializable]
public class OneBotGroupMsg(uint selfId, uint groupUin, List<OneBotSegment> message, string rawMessage, BotGroupMember member, int messageId, long time) : OneBotEntityBase(selfId, "message", time)
{
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "group";

    [JsonPropertyName("sub_type")] public string SubType { get; set; } = "normal";

    [JsonPropertyName("message_id")] public int MessageId { get; set; } = messageId;

    [JsonPropertyName("group_id")] public uint GroupId { get; set; } = groupUin;
    
    [JsonPropertyName("user_id")] public uint UserId { get; set; } = member.Uin;
    
    [JsonPropertyName("anonymous")] public object? Anonymous { get; set; } = null;

    [JsonPropertyName("message")] public List<OneBotSegment> Message { get; set; } = message;

    [JsonPropertyName("raw_message")] public string RawMessage { get; set; } = rawMessage;

    [JsonPropertyName("font")] public int Font { get; set; } = 0;

    [JsonPropertyName("sender")] public OneBotGroupSender GroupSender { get; set; } = new(member.Uin, member.MemberName, member.MemberCard ?? string.Empty, (int)member.GroupLevel, member.Permission);
}

[Serializable]
public class OneBotGroupStringMsg(uint selfId, uint groupUin, string message, BotGroupMember member, int messageId, long time) : OneBotEntityBase(selfId, "message", time)
{
    [JsonPropertyName("message_type")] public string MessageType { get; set; } = "group";

    [JsonPropertyName("sub_type")] public string SubType { get; set; } = "normal";

    [JsonPropertyName("message_id")] public int MessageId { get; set; } = messageId;

    [JsonPropertyName("group_id")] public uint GroupId { get; set; } = groupUin;
    
    [JsonPropertyName("user_id")] public uint UserId { get; set; } = member.Uin;
    
    [JsonPropertyName("anonymous")] public object? Anonymous { get; set; } = null;
    
    [JsonPropertyName("message")] public string Message { get; set; } = message;

    [JsonPropertyName("raw_message")] public string RawMessage { get; set; } = message;

    [JsonPropertyName("font")] public int Font { get; set; } = 0;

    [JsonPropertyName("sender")] public OneBotGroupSender GroupSender { get; set; } = new(member.Uin, member.MemberName, member.MemberCard ?? string.Empty, (int)member.GroupLevel, member.Permission);
}
