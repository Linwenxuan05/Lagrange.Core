using System.Text.Json.Serialization;

namespace Lagrange.OneBot.Core.Entity;

[Serializable]
public abstract class OneBotEntityBase(uint selfId, string postType, long time)
{
    public OneBotEntityBase(uint selfId, string postType)
        : this(selfId, postType, DateTimeOffset.Now.ToUnixTimeSeconds())
    {
    }

    [JsonPropertyName("time")] public long Time { get; set; } = time;

    [JsonPropertyName("self_id")] public uint SelfId { get; set; } = selfId;

    [JsonPropertyName("post_type")] public string PostType { get; set; } = postType;
}