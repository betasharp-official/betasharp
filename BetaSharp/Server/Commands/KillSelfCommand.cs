using BetaSharp.Entities;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class KillSelfCommand : Command.Command
{
    public override string Usage => "kill <player>";
    public override string Description => "Kills player";
    public override string[] Names => ["kill"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute).Then(ArgumentPlayer("player").Executes(ExecuteOther));

    private static int Execute(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (player == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 1;
        }

        player.damage(null, 1000);
        return 1;
    }

    private static int ExecuteOther(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity player = context.GetArgument<ServerPlayerEntity>("player");
        player.damage(null, 1000);
        return 1;
    }
}
