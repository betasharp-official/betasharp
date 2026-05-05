using BetaSharp.Entities;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class TeleportDimensionCommand : Command.Command
{
    public override string Usage => "tpdim <id> <player>";
    public override string Description => "Teleports to a dimension";
    public override string[] Names => ["tpdim"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(ArgumentInt("dim")
                .Executes(TpdimSelf)
                .Then(ArgumentPlayer("player").Executes(TpdimPlayer)));

    private static int TpdimSelf(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (player == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 0;
        }

        return TeleportToDimension(context, player);
    }

    private static int TpdimPlayer(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity player = context.GetArgument<ServerPlayerEntity>("player");
        return TeleportToDimension(context, player);
    }

    private static int TeleportToDimension(CommandContext<CommandSource> context, ServerPlayerEntity player)
    {
        int dim = context.GetArgument<int>("dim");

        if (dim != 0 && dim != -1)
        {
            context.Source.Output.SendMessage("Dimension " + dim + " does not exist.");
            return 0;
        }

        if (player.DimensionId == dim)
        {
            context.Source.Output.SendMessage("Player is already in dimension " + dim);
            return 0;
        }

        context.Source.Server.playerManager.sendPlayerToDimension(player, dim);
        context.Source.Output.SendMessage("Teleported " + player.Name + " to dimension " + dim);
        return 1;
    }
}
