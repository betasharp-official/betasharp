using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class GiveCommand : ICommand
{
    public string Usage => "give [player] <item> [count]";
    public string Description => "Gives yourself an item";
    public string[] Names => ["give"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length <= 1)
        {
            c.Output.SendMessage($"Usage: {Usage}");
            return;
        }
        else if (c.Args.Length >= 2)
        {
            ServerPlayerEntity? sender = c.Server.playerManager.getPlayer(c.SenderName);
            if (sender == null)
            {
                c.Output.SendMessage("Could not find your player.");
                return;
            }

            // give <item> [count] --> self
            if (ItemLookup.TryResolveItemId(c.Args[0], out int selfItemId))
            {

                int count = 1;
                if (c.Args.Length > 1 && int.TryParse(c.Args[1], out int parsedCount))
                {
                    count = Math.Clamp(parsedCount, 1, 64);
                }

                sender.inventory.AddItemStackToInventoryOrDrop(new ItemStack(selfItemId, count, 0));
                c.LogOp($"{sender.name} Gave {count} [{selfItemId}] to {sender.name}");
                c.Output.SendMessage($"Gave {count} [{selfItemId}] to {sender.name}");
                return;
            }
            else // give [player] <item> [count] --> to player
            {
                string targetName = c.Args[0];
                ServerPlayerEntity? targetPlayer = c.Server.playerManager.getPlayer(targetName);

                if (targetPlayer == null)
                {
                    c.Output.SendMessage("Can't find user " + targetName);
                    return;
                }

                if (!ItemLookup.TryResolveItemId(c.Args[1], out int itemId))
                {
                    c.Output.SendMessage("Unknown item: " + c.Args[1]);
                    return;
                }

                if (Item.ITEMS[itemId] == null)
                {
                    c.Output.SendMessage("There's no item with id " + itemId);
                    return;
                }

                int count = 1;
                if (c.Args.Length > 2 && int.TryParse(c.Args[2], out int parsedCount))
                {
                    count = Math.Clamp(parsedCount, 1, 64);
                }

                c.LogOp($"Giving {targetPlayer.name} {count}x{itemId}");
                targetPlayer.inventory.AddItemStackToInventoryOrDrop(new ItemStack(itemId, count, 0));
                c.LogOp($"{sender} Gave {count} [{itemId}] to {targetPlayer.name}");
                c.Output.SendMessage($"Gave {count} [{itemId}] to {targetPlayer.name}");
                return;
            }
        }
        return;
    }
}
