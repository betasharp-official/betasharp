using BetaSharp.Entities;
using BetaSharp.Network.Packets.Play;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Commands;

public class TellCommand : Command.Command
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(TellCommand));

    public override string Usage => "tell <player> <message>";
    public override string Description => "Whispers to a player";
    public override string[] Names => ["tell"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentPlayer("player").Then(ArgumentGreedy("message").Executes(Execute)));

    private static int Execute(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity target = context.GetArgument<ServerPlayerEntity>("player");
        string message = context.GetArgument<string>("message");
        s_logger.LogInformation("[" + context.Source.SenderName + "->" + target.Name + "] " + message);

        string whisper = "§7" + context.Source.SenderName + " whispers " + message;
        s_logger.LogInformation(whisper);

        if (!PlayerManager.sendPacket(target, ChatMessagePacket.Get(whisper)))
        {
            context.Source.Output.SendMessage("There's no player by that name online.");
        }

        return 1;
    }
}
