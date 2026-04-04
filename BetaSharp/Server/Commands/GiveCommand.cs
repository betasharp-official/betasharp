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
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage($"Usage: {Usage}");
            return;
        }

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

            ItemStack stack = new(selfItemId, count, 0);
            sender.inventory.AddItemStackToInventoryOrDrop(stack);
            string msg = $"Gave {count} [{ItemLookup.ResolveItemName(stack)}] to {sender.name}";
            c.LogOp($"{sender.name} {msg}");
            c.Output.SendMessage(msg);
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

            ItemStack stack = new(itemId, count, 0);
            targetPlayer.inventory.AddItemStackToInventoryOrDrop(stack);
            string msg = $"Gave {count} [{ItemLookup.ResolveItemName(stack)}] to {sender.name}";
            c.LogOp($"{targetPlayer.name} {msg}");
            c.Output.SendMessage(msg);
        }
    }
}
