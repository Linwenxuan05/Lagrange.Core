using System.Text.Json.Serialization;
using Lagrange.Core.Message;
using Lagrange.Core.Message.Entity;

namespace Lagrange.OneBot.Message.Entity;

[Serializable]
public partial class GreyTipSegment(string content, uint objectType, uint subType, uint type)
{
    public GreyTipSegment() : this("", 3, 2, 4) { }

    [JsonPropertyName("content")][CQProperty] public string Content { get; set; } = content;
    [JsonPropertyName("object_type")][CQProperty] public uint ObjectType { get; set; } = objectType;
    [JsonPropertyName("sub_type")][CQProperty] public uint SubType { get; set; } = subType;
    [JsonPropertyName("type")][CQProperty] public uint Type { get; set; } = type;

}

[SegmentSubscriber(typeof(GreyTipEntity), "greytip")]
public partial class GreyTipSegment : SegmentBase
{
    public override void Build(MessageBuilder builder, SegmentBase segment)
    {
        if (segment is GreyTipSegment greyTip) builder.greytip(greyTip.Content, greyTip.ObjectType, greyTip.SubType, greyTip.Type);
    }

    public override SegmentBase? FromEntity(MessageChain chain, IMessageEntity entity)
    {
        return null;
    }
}