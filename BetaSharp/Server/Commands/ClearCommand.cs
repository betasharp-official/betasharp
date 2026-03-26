using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class ClearCommand : ICommand
{
    public string Usage => "clear";
    public string Description => "Clears your inventory";
    public string[] Names => ["clear"];

    public void Execute(ICommand.CommandContext c)
    {
        ServerPlayerEntity? player = c.Server.playerManager.getPlayer(c.SenderName);
        if (player == null)
        {
            c.Output.SendMessage("Could not find your player.");
            return;
        }

        ItemStack[] inventory = player.inventory.main;
        for (int i = 0; i < inventory.Length; i++)
        {
            inventory[i] = null;
        }

        c.Output.SendMessage("Inventory cleared.");
    }
}
