using BetaSharp.Entities;
using BetaSharp.Items;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class ClearCommand : Command.Command
{
    public override string Usage => "clear";
    public override string Description => "Clears your inventory";
    public override string[] Names => ["clear"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (player == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 1;
        }

        ItemStack[] inventory = player.Inventory.Main;
        for (int i = 0; i < inventory.Length; i++)
        {
            inventory[i] = null;
        }

        context.Source.Output.SendMessage("Inventory cleared.");
        return 1;
    }
}
