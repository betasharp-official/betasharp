using BetaSharp.Entities;
using BetaSharp.Items;
using BetaSharp.Server.Command;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class GiveCommand : Command.Command
{
    public override string Usage => "give <player> <item> <count>";
    public override string Description => "Gives yourself an item";
    public override string[] Names => ["give"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(ArgumentString("item")
                .Executes(GiveItem)
                .Then(ArgumentInt("count")
                    .Executes(GiveItemCount)))
            .Then(ArgumentPlayer("player")
                .Then(ArgumentString("item")
                    .Executes(GivePlayerItem)
                    .Then(ArgumentInt("count")
                        .Executes(GivePlayerItemCount))));

    // give <item>  →  give 1 of item to self
    private static int GiveItem(CommandContext<CommandSource> context)
    {
        string item = context.GetArgument<string>("item");

        GiveTo(context.Source, item, 1);
        return 1;
    }

    private static int GiveItemCount(CommandContext<CommandSource> context)
    {
        string item = context.GetArgument<string>("item");
        int count = context.GetArgument<int>("count");

        GiveTo(context.Source, item, count);
        return 1;
    }


    private static int GivePlayerItem(CommandContext<CommandSource> context)
    {
        string item = context.GetArgument<string>("item");
        ServerPlayerEntity player = context.GetArgument<ServerPlayerEntity>("player");

        GiveTo(context.Source, player, item, 1);
        return 1;
    }

    private static int GivePlayerItemCount(CommandContext<CommandSource> context)
    {
        string item = context.GetArgument<string>("item");
        ServerPlayerEntity player = context.GetArgument<ServerPlayerEntity>("player");
        int count = context.GetArgument<int>("count");

        GiveTo(context.Source, player, item, count);
        return 1;
    }

    private static void GiveTo(CommandSource source, string item, int count)
    {
        ServerPlayerEntity? sender = source.Server.playerManager.getPlayer(source.SenderName);
        if (sender == null)
        {
            source.Output.SendMessage("Could not find your player.");
            return;
        }

        GiveTo(source, sender, item, count);
    }

    private static void GiveTo(CommandSource source, ServerPlayerEntity target, string item, int count)
    {
        if (!ItemLookup.TryResolveItemId(item, out int itemId))
        {
            source.Output.SendMessage("Unknown item: " + item);
            return;
        }

        ItemStack stack = new(itemId, count, 0);
        target.inventory.AddItemStackToInventoryOrDrop(stack);
        string msg = $"Gave {count} [{ItemLookup.ResolveItemName(stack)}] to {target.name}";
        source.LogOp($"{target.name} {msg}");
        source.Output.SendMessage(msg);
    }
}
