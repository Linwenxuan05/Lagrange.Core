using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Lagrange.Core;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using Lagrange.Core.Utility.Extension;
using Lagrange.OneBot.Core.Entity.Message;
using Lagrange.OneBot.Core.Network;
using Lagrange.OneBot.Database;
using Lagrange.OneBot.Message.Entity;
using LiteDB;
using Microsoft.Extensions.Configuration;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Lagrange.OneBot.Message;

/// <summary>
/// The class that converts the OneBot message to/from MessageEntity of Lagrange.Core
/// </summary>
public sealed class MessageService
{
    private readonly LagrangeWebSvcCollection _service;
    private readonly LiteDatabase _context;
    private readonly IConfiguration _config;
    private readonly Dictionary<Type, List<(string Type, SegmentBase Factory)>> _entityToFactory;
    private readonly bool _stringPost;

    private static readonly JsonSerializerOptions Options;

    static MessageService()
    {
        Options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { ModifyTypeInfo } } };
    }

    public MessageService(BotContext bot, LagrangeWebSvcCollection service, LiteDatabase context, IConfiguration config)
    {
        _service = service;
        _context = context;
        _config = config;
        _stringPost = config.GetValue<bool>("Message:StringPost");

        var invoker = bot.Invoker;

        invoker.OnFriendMessageReceived += OnFriendMessageReceived;
        invoker.OnGroupMessageReceived += OnGroupMessageReceived;
        invoker.OnTempMessageReceived += OnTempMessageReceived;

        _entityToFactory = new Dictionary<Type, List<(string, SegmentBase)>>();
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attribute = type.GetCustomAttribute<SegmentSubscriberAttribute>();
            if (attribute != null)
            {
                var instance = (SegmentBase)type.CreateInstance(false);
                instance.Database = _context;

                if (_entityToFactory.TryGetValue(attribute.Entity, out var factories)) factories.Add((attribute.Type, instance));
                else _entityToFactory[attribute.Entity] = [(attribute.Type, instance)];
            }
        }
    }

    private void OnFriendMessageReceived(BotContext bot, FriendMessageEvent e)
    {
        var record = (MessageRecord)e.Chain;
        _context.GetCollection<MessageRecord>().Insert(new BsonValue(record.MessageHash), record);

        if (_config.GetValue<bool>("Message:IgnoreSelf") && e.Chain.FriendUin == bot.BotUin) return; // ignore self message

        var request = ConvertToPrivateMsg(bot.BotUin, e.Chain);

        _ = _service.SendJsonAsync(request);
    }

    public object ConvertToPrivateMsg(uint uin, MessageChain chain)
    {
        var segments = Convert(chain);
        int hash = MessageRecord.CalcMessageHash(chain.MessageId, chain.Sequence);
        string raw = ToRawMessage(segments);
        object request = _stringPost ? new OneBotPrivateStringMsg(uin, new OneBotSender(chain.FriendUin, chain.FriendInfo?.Nickname ?? string.Empty), "friend", ((DateTimeOffset)chain.Time).ToUnixTimeSeconds())
        {
            MessageId = hash,
            UserId = chain.FriendUin,
            Message = raw,
            RawMessage = raw,
            TargetId = chain.TargetUin,
        } : new OneBotPrivateMsg(uin, new OneBotSender(chain.FriendUin, chain.FriendInfo?.Nickname ?? string.Empty), "friend", ((DateTimeOffset)chain.Time).ToUnixTimeSeconds())
        {
            MessageId = hash,
            UserId = chain.FriendUin,
            Message = segments,
            RawMessage = raw,
            TargetId = chain.TargetUin
        };
        return request;
    }

    private void OnGroupMessageReceived(BotContext bot, GroupMessageEvent e)
    {
        var record = (MessageRecord)e.Chain;
        _context.GetCollection<MessageRecord>().Insert(new BsonValue(record.MessageHash), record);
        if (_config.GetValue<bool>("Message:IgnoreSelf") && e.Chain.FriendUin == bot.BotUin) return; // ignore self message

        var request = ConvertToGroupMsg(bot.BotUin, e.Chain);

        _ = _service.SendJsonAsync(request);
    }

    public object ConvertToGroupMsg(uint uin, MessageChain chain)
    {
        var segments = Convert(chain);
        int hash = MessageRecord.CalcMessageHash(chain.MessageId, chain.Sequence);
        object request = _stringPost
            ? new OneBotGroupStringMsg(uin, chain.GroupUin ?? 0, ToRawMessage(segments), chain.GroupMemberInfo ?? throw new Exception("Group member not found"), hash, ((DateTimeOffset)chain.Time).ToUnixTimeSeconds())
            : new OneBotGroupMsg(uin, chain.GroupUin ?? 0, segments, ToRawMessage(segments), chain.GroupMemberInfo ?? throw new Exception("Group member not found"), hash, ((DateTimeOffset)chain.Time).ToUnixTimeSeconds());
        return request;
    }

    private void OnTempMessageReceived(BotContext bot, TempMessageEvent e)
    {
        var record = (MessageRecord)e.Chain;
        _context.GetCollection<MessageRecord>().Insert(new BsonValue(record.MessageHash), record);

        var segments = Convert(e.Chain);
        var request = new OneBotPrivateMsg(bot.BotUin, new OneBotSender(e.Chain.FriendUin, e.Chain.FriendInfo?.Nickname ?? string.Empty), "group", ((DateTimeOffset)e.Chain.Time).ToUnixTimeSeconds())
        {
            MessageId = record.MessageHash,
            UserId = e.Chain.FriendUin,
            Message = segments,
            RawMessage = ToRawMessage(segments)
        };

        _ = _service.SendJsonAsync(request);
    }

    public List<OneBotSegment> Convert(MessageChain chain)
    {
        var result = new List<OneBotSegment>();

        foreach (var entity in chain)
        {
            if (_entityToFactory.TryGetValue(entity.GetType(), out var instances))
            {
                foreach (var instance in instances)
                {
                    if (instance.Factory.FromEntity(chain, entity) is { } segment) result.Add(new OneBotSegment(instance.Type, segment));
                }
            }
        }

        return result;
    }

    private static string EscapeText(string str) => str
        .Replace("&", "&amp;")
        .Replace("[", "&#91;")
        .Replace("]", "&#93;");

    private static string EscapeCQ(string str) => EscapeText(str).Replace(",", "&#44;");

    private static string ToRawMessage(List<OneBotSegment> segments)
    {
        var rawMessageBuilder = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.Data is TextSegment textSeg)
            {
                rawMessageBuilder.Append(EscapeText(textSeg.Text));
            }
            else
            {
                rawMessageBuilder.Append("[CQ:");
                rawMessageBuilder.Append(segment.Type);
                foreach (var property in JsonSerializer.SerializeToElement(segment.Data, Options).EnumerateObject())
                {
                    rawMessageBuilder.Append(',');
                    rawMessageBuilder.Append(property.Name);
                    rawMessageBuilder.Append('=');
                    rawMessageBuilder.Append(EscapeCQ(property.Value.ToString()));
                }
                rawMessageBuilder.Append(']');
            }
        }
        return rawMessageBuilder.ToString();
    }

    private static void ModifyTypeInfo(JsonTypeInfo ti)
    {
        if (ti.Kind != JsonTypeInfoKind.Object) return;
        foreach (var info in ti.Properties.Where(x => x.AttributeProvider?.IsDefined(typeof(CQPropertyAttribute), false) == false).ToArray())
        {
            ti.Properties.Remove(info);
        }
    }
}
