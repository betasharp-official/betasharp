using BetaSharp.Network.Packets.Play;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class SayCommand : Command.Command
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(SayCommand));

    public override string Usage => "say <message>";
    public override string Description => "Broadcasts a message";
    public override string[] Names => ["say"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentGreedy("message").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string message = context.GetArgument<string>("message");
        s_logger.LogInformation("[" + context.Source.SenderName + "] " + message);
        context.Source.Server.playerManager.sendToAll(ChatMessagePacket.Get("§d[Server] " + message));
        return 1;
    }
}
