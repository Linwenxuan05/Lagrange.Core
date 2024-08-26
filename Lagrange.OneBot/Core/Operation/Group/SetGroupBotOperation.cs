using System.Text.Json;
using System.Text.Json.Nodes;
using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.OneBot.Core.Entity.Action;
using Lagrange.OneBot.Core.Operation.Converters;

namespace Lagrange.OneBot.Core.Operation.Group;

[Operation("set_group_bot_status")]
public class SetGroupBotOperation : IOperation
{
    public async Task<OneBotResult> HandleOperation(BotContext context, JsonNode? payload)
    {
        var message = payload.Deserialize<OneBotSetGroupBot>(SerializerOptions.DefaultOptions);

        if (message != null)
        {
            bool _ = await context.SetGroupBot(message.BotId, message.Enable, message.GroupId);

            return new OneBotResult(message.BotId, 0, "ok");
        }

        throw new Exception();
    }
}
