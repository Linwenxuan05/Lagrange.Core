using Lagrange.Core.Common.Entity;
using Lagrange.Core.Message.Entity;

namespace Lagrange.Core.Message;

/// <summary>
/// MessageBuilder is used to build the <see cref="MessageChain"/>, every MessageChain should be created with this class
/// </summary>
public sealed class MessageBuilder
{
    private readonly MessageChain _chain;

    private MessageBuilder(MessageChain chain) => _chain = chain;

    public bool IsGroup => _chain.IsGroup;

    public static MessageBuilder Friend(uint friendUin) => new(new MessageChain(friendUin, "", "")); // automatically set selfUid and friendUid by MessagingLogic

    public static MessageBuilder Group(uint groupUin) => new(new MessageChain(groupUin));

    [Obsolete("Use `MessageBuilder.Friend(uint)`")]
    public static MessageBuilder FakeGroup(uint groupUin, uint memberUin) =>
            new(new MessageChain(groupUin, memberUin, (uint)Random.Shared.Next(1000000, 9999999)));

    /// <summary>
    /// Set the sending time for this message
    /// Used for time display in mulitmsg
    /// </summary>
    /// <param name="time">The sending time of this message</param>
    public MessageBuilder Time(DateTime? time)
    {
        _chain.Time = time ?? default;

        return this;
    }

    /// <summary>
    /// Set the friend name for this message
    /// Used for name display in mulitmsg
    /// </summary>
    /// <param name="time">The sending time of this message</param>
    public MessageBuilder FriendName(string? name)
    {
        (_chain.FriendInfo ??= new BotFriend(
            _chain.FriendUin,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty
        )).Nickname = name ?? "";

        return this;
    }

    /// <summary>
    /// Set the friend avator for this message
    /// Used for avatar display in mulitmsg
    /// </summary>
    /// <param name="time">The sending time of this message</param>
    public MessageBuilder FriendAvatar(string? avatar)
    {
        (_chain.FriendInfo ??= new BotFriend(
            _chain.FriendUin,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty
        )).Avatar = avatar ?? "";

        return this;
    }

    /// <summary>
    /// Add a text entity to the message chain
    /// </summary>
    /// <param name="text">The text to be added</param>
    public MessageBuilder Text(string text)
    {
        var textEntity = new TextEntity(text);
        _chain.Add(textEntity);

        return this;
    }

    /// <summary>
    /// Add a mention entity to the message chain (@someone)
    /// </summary>
    /// <param name="target">the Uin of target</param>
    /// <param name="display">the string to be displayed</param>
    public MessageBuilder Mention(uint target, string? display = null)
    {
        var mentionEntity = new MentionEntity(display, target);
        _chain.Add(mentionEntity);

        return this;
    }

    /// <summary>
    /// Add a face entity to the message chain
    /// </summary>
    /// <param name="id">The id of emoji</param>
    /// <param name="isLarge">Is the emoji large</param>
    public MessageBuilder Face(ushort id, bool isLarge = false)
    {
        var faceEntity = new FaceEntity(id, isLarge);
        _chain.Add(faceEntity);

        return this;
    }

    /// <summary>
    /// Add a forward entity to the message chain (reply)
    /// </summary>
    /// <param name="target">The message chain to be forwarded</param>
    public MessageBuilder Forward(MessageChain target)
    {
        var forwardEntity = new ForwardEntity(target);
        _chain.Add(forwardEntity);

        return this;
    }
    
    /// <summary>
    /// Add a forward entity to the specified message sequence number
    /// </summary>
    /// <param name="sequence">The sequence number of the message to be forwarded</param>
    public MessageBuilder Forward(uint sequence)
    {
        var forwardEntity = new ForwardEntity(sequence);
        _chain.Add(forwardEntity);

        return this;
    }

    /// <summary>
    /// Add a multimsg entity to the message chain (multi message)
    /// </summary>
    /// <param name="groupUin">The group to be sent, if null, the message will be sent as private message</param>
    /// <param name="msg">The messages to be sent</param>
    [Obsolete("No more need for group uin")]
    public MessageBuilder MultiMsg(uint? groupUin = null, params MessageBuilder[] msg)
    {
        return MultiMsg(msg);
    }

    public MessageBuilder MultiMsg(params MessageBuilder[] msg)
    {
        var multiMsgEntity = new MultiMsgEntity(msg.Select(x => x.Build()).ToList());
        _chain.Add(multiMsgEntity);

        return this;
    }

    [Obsolete("No more need for group uin")]
    public MessageBuilder MultiMsg(uint? groupUin = null, params MessageChain[] chains)
    {
        return MultiMsg(chains);
    }

    public MessageBuilder MultiMsg(params MessageChain[] chains)
    {
        var multiMsgEntity = new MultiMsgEntity(chains.ToList());
        _chain.Add(multiMsgEntity);

        return this;
    }

    /// <summary>
    /// Add a xml entity to the message chain (card message)
    /// </summary>
    /// <param name="xml">The xml to be sent</param>
    public MessageBuilder Xml(string xml)
    {
        var xmlEntity = new XmlEntity(xml);
        _chain.Add(xmlEntity);

        return this;
    }
    
    /// <summary>
    /// Add a xml entity (with custom serviceId) to the message chain (card message)
    /// </summary>
    /// <param name="xml">The xml to be sent</param>
    /// <param name="serviceId">The service id of the xml</param>
    public MessageBuilder Xml(string xml, int serviceId)
    {
        var xmlEntity = new XmlEntity(xml, serviceId);
        _chain.Add(xmlEntity);

        return this;
    }

    /// <summary>
    /// Add a image entity to the message chain
    /// </summary>
    /// <remarks>The file would not be closed until the message is sent</remarks>
    /// <param name="filePath">The path of image file</param>
    public MessageBuilder Image(string filePath)
    {
        var imageEntity = new ImageEntity(filePath);
        _chain.Add(imageEntity);

        return this;
    }

    /// <summary>
    /// Add a image entity to the message chain
    /// </summary>
    /// <param name="file">The image file</param>
    public MessageBuilder Image(byte[] file)
    {
        var imageEntity = new ImageEntity(file);
        _chain.Add(imageEntity);

        return this;
    }

    public MessageBuilder Video(byte[] file, int videoLength = 0)
    {
        var videoEntity = new VideoEntity(file, videoLength);
        _chain.Add(videoEntity);

        return this;
    }

    public MessageBuilder Video(string filePath, int videoLength = 0)
    {
        var videoEntity = new VideoEntity(filePath, videoLength);
        _chain.Add(videoEntity);

        return this;
    }

    /// <summary>
    /// Add a audio entity to the message chain
    /// </summary>
    /// <param name="file">The audio file that has already been converted to SilkCodec</param>
    /// <param name="audioLength">The length of the audio file that directly shown</param>
    public MessageBuilder Record(byte[] file, int audioLength = 0)
    {
        var recordEntity = new RecordEntity(file, audioLength);
        _chain.Add(recordEntity);

        return this;
    }

    /// <summary>
    /// Add a audio entity to the message chain
    /// </summary>
    /// <param name="filePath">The audio file that has already been converted to SilkCodec</param>
    /// <param name="audioLength">The length of the audio file that directly shown</param>
    public MessageBuilder Record(string filePath, int audioLength = 0)
    {
        var recordEntity = new RecordEntity(filePath, audioLength);
        _chain.Add(recordEntity);

        return this;
    }

    /// <summary>
    /// Add a dedicated poke entity to message chain
    /// </summary>
    /// <param name="type">Poke ID, default value is the preset of NTQQ</param>
    public MessageBuilder Poke(uint type = 1)
    {
        var pokeEntity = new PokeEntity(type, 0);
        _chain.Add(pokeEntity);

        return this;
    }

    /// <summary>
    /// Add a dedicated poke entity to message chain
    /// </summary>
    /// <param name="type">Poke ID, default value is the preset of NTQQ</param>
    /// <param name="strength">Poke strength</param>
    public MessageBuilder Poke(uint type = 1, uint strength = 0)
    {
        var pokeEntity = new PokeEntity(type, strength);
        _chain.Add(pokeEntity);

        return this;
    }

    /// <summary>
    /// Add a dedicated poke(window shake) entity to message chain
    /// </summary>
    /// <param name="type">face type</param>
    /// <param name="strength">How big the face will be displayed ([0,3] is valid)</param>
    public MessageBuilder Poke(PokeFaceType type, uint strength = 0)
    {
        var friendShakeEntity = new PokeEntity((uint)type, strength);
        _chain.Add(friendShakeEntity);

        return this;
    }

    /// <summary>
    /// Add a dedicated special window shake entity to message chain
    /// </summary>
    /// <param name="type">face type</param>
    /// <param name="count">count of face</param>
    public MessageBuilder SpecialPoke(SpecialPokeFaceType type, uint count = 1)
    {
        var friendSpecialShakeEntity = new SpecialPokeEntity((uint)type, count, type.ToName());
        _chain.Add(friendSpecialShakeEntity);

        return this;
    }

    /// <summary>
    /// Add a bounce face entity to message chain
    /// </summary>
    /// <param name="face">The face entry of the face to be added to the message chain</param>
    /// <param name="count">The number of face entities to generate</param>
    /// <param name="shouldAddPreviewText">Should the entity add a preview text</param>
    public MessageBuilder BounceFace(SysFaceEntry face, uint count, bool shouldAddPreviewText = true)
    {
        var bounceFaceEntity = new BounceFaceEntity(face, count, shouldAddPreviewText);
        _chain.Add(bounceFaceEntity);

        return this;
    }

    /// <summary>
    /// Add a bounce face entity to message chain
    /// </summary>
    /// <param name="faceId">The face ID of the face to be added to the message chain</param>
    /// <param name="count">The number of face entities to generate, default is 1</param>
    /// <param name="name">The name of the face</param>
    /// <param name="shouldAddPreviewText">Should the entity add a preview text</param>
    public MessageBuilder BounceFace(uint faceId, uint count, string? name = null, bool shouldAddPreviewText = true)
    {
        var bounceFaceEntity = new BounceFaceEntity(faceId, count, name, shouldAddPreviewText);
        _chain.Add(bounceFaceEntity);

        return this;
    }

    /// <summary>
    /// Add a dedicated LightApp entity to message chain
    /// </summary>
    /// <param name="payload">Json Payload</param>
    public MessageBuilder LightApp(string payload)
    {
        var pokeEntity = new LightAppEntity(payload);
        _chain.Add(pokeEntity);

        return this;
    }

    public MessageBuilder LongMsg(string resId)
    {
        var longMsgEntity = new LongMsgEntity(resId);
        _chain.Add(longMsgEntity);

        return this;
    }

    public MessageBuilder LongMsg(MessageChain chain)
    {
        var longMsgEntity = new LongMsgEntity(chain);
        _chain.Add(longMsgEntity);

        return this;
    }

    public MessageBuilder Markdown(string json)
    {
        var markdownEntity = new MarkdownEntity(json);
        _chain.Add(markdownEntity);

        return this;
    }

    public MessageBuilder Markdown(MarkdownData data)
    {
        var markdownEntity = new MarkdownEntity(data);
        _chain.Add(markdownEntity);

        return this;
    }

    public MessageBuilder Keyboard(string json)
    {
        var keyboardEntity = new KeyboardEntity(json);
        _chain.Add(keyboardEntity);

        return this;
    }

    public MessageBuilder Keyboard(KeyboardData data)
    {
        var keyboardEntity = new KeyboardEntity(data);
        _chain.Add(keyboardEntity);

        return this;
    }

    public MessageBuilder MarketFace(string faceId, int tabId, string key, string summary)
    {
        var marketFaceEntity = new MarketfaceEntity(faceId, tabId, key, summary);
        _chain.Add(marketFaceEntity);

        return this;
    }

    [Obsolete("This is an embarrassing typo... sorry; see also: GreyTip")]
    public MessageBuilder GeryTip(string greyTip)
    {
        return GreyTip(greyTip);
    }

    public MessageBuilder GreyTip(string greyTip)
    {
        var greyTipEntity = new GreyTipEntity(greyTip);
        _chain.Add(greyTipEntity);

        return this;
    }

    public MessageBuilder Add(IMessageEntity entity)
    {
        _chain.Add(entity);
        return this;
    }

    public MessageChain Build() => _chain;
}