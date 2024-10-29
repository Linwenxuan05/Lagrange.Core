﻿using System.Text.Json.Serialization;

namespace Lagrange.OneBot.Core.Entity.Action;

[Serializable]
public class OneBotGetAiRecord
{
    [JsonPropertyName("character")] public string Character { get; set; }

    [JsonPropertyName("group_id")] public uint GroupId { get; set; }

    [JsonPropertyName("text")] public string Text { get; set; }
}