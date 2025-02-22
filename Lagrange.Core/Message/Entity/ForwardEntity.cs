using Lagrange.Core.Internal.Packets.Message.Element;
using Lagrange.Core.Internal.Packets.Message.Element.Implementation;
using Lagrange.Core.Internal.Packets.Message.Element.Implementation.Extra;
using ProtoBuf;

namespace Lagrange.Core.Message.Entity;

[MessageElement(typeof(SrcMsg))]
public class ForwardEntity : IMessageEntity
{
    public DateTime Time { get; set; }

    public ulong MessageId { get; set; }

    public uint Sequence { get; set; }

    public uint ClientSequence { get; set; }

    public string? Uid { get; set; }

    public uint TargetUin { get; set; }

    internal List<Elem> Elements { get; }

    private string? SelfUid { get; set; }

    public ForwardEntity()
    {
        Sequence = 0;
        Uid = null;
        Elements = new List<Elem>();
    }

    public ForwardEntity(uint sequence)
    {
        Sequence = sequence;
        Uid = null;
        Elements = new List<Elem>();
    }

    public ForwardEntity(MessageChain chain)
    {
        Time = chain.Time;
        Sequence = chain.Sequence;
        ClientSequence = chain.ClientSequence;
        Uid = chain.Uid;
        Elements = chain.Elements;
        TargetUin = chain.FriendUin;
        MessageId = chain.MessageId;
    }

    IEnumerable<Elem> IMessageEntity.PackElement()
    {
        var forwardReserve = new SrcMsg.Preserve
        {
            MessageId = MessageId,
        };
        using var forwardStream = new MemoryStream();
        Serializer.Serialize(forwardStream, forwardReserve);

        var mentionReserve = new MentionExtra
        {
            Type = TargetUin == 0 ? 1 : 2,
            Uin = 0,
            Field5 = 0,
            Uid = Uid,
        };
        using var mentionStream = new MemoryStream();
        Serializer.Serialize(mentionStream, mentionReserve);

        return new Elem[]
        {
            new()
            {
                SrcMsg = new SrcMsg
                {
                    OrigSeqs = new List<uint> { ClientSequence != 0 ? ClientSequence : Sequence },
                    SenderUin = TargetUin, // Can't get self uin
                    Time = (int)new DateTimeOffset(Time).ToUnixTimeSeconds(),
                    Elems = Elements,
                    PbReserve = forwardStream.ToArray(),
                    ToUin = 0
                }
            },
            new()
            {
                Text = ClientSequence == 0 ? new Text
                { 
                    Str = "not null",
                    PbReserve = mentionStream.ToArray()
                } : null
            }
        };
    }

    IMessageEntity? IMessageEntity.UnpackElement(Elem elems)
    {
        if (elems.SrcMsg is { } srcMsg)
        {
            var reserve = Serializer.Deserialize<SrcMsg.Preserve>(srcMsg.PbReserve.AsSpan());
            return new ForwardEntity
            {
                Time = DateTimeOffset.FromUnixTimeSeconds(srcMsg.Time ?? 0).LocalDateTime,
                Sequence = reserve.FriendSequence ?? srcMsg.OrigSeqs?[0] ?? 0,
                TargetUin = (uint)srcMsg.SenderUin,
                MessageId = reserve.MessageId
            };
        }

        return null;
    }

    public void SetSelfUid(string selfUid) => SelfUid = selfUid;

    public string ToPreviewString() => $"[Forward] Time: {Time} Sequence: {Sequence} ";

    public string ToPreviewText() => string.Empty;
}
