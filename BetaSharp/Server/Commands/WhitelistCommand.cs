using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class WhitelistCommand : Command.Command
{
    public override string Usage => "whitelist <on|off|list|add|remove|reload> [player]";
    public override string Description => "Manages the whitelist";
    public override string[] Names => ["whitelist"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(Literal("on").Executes(WhitelistOn))
            .Then(Literal("off").Executes(WhitelistOff))
            .Then(Literal("list").Executes(WhitelistList))
            .Then(Literal("reload").Executes(WhitelistReload))
            .Then(Literal("add").Then(ArgumentString("player").Executes(WhitelistAdd)))
            .Then(Literal("remove").Then(ArgumentString("player").Executes(WhitelistRemove)));

    private static int WhitelistOn(CommandContext<CommandSource> context)
    {
        context.Source.LogOp("Turned on white-listing");
        context.Source.Server.config.SetProperty("white-list", true);
        return 1;
    }

    private static int WhitelistOff(CommandContext<CommandSource> context)
    {
        context.Source.LogOp("Turned off white-listing");
        context.Source.Server.config.SetProperty("white-list", false);
        return 1;
    }

    private static int WhitelistList(CommandContext<CommandSource> context)
    {
        HashSet<string> whitelist = context.Source.Server.playerManager.getWhitelist();
        string names = string.Join(" ", whitelist);
        context.Source.Output.SendMessage("White-listed players: " + names);
        return 1;
    }

    private static int WhitelistReload(CommandContext<CommandSource> context)
    {
        context.Source.Server.playerManager.reloadWhitelist();
        context.Source.LogOp("Reloaded white-list from file");
        return 1;
    }

    private static int WhitelistAdd(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player").ToLower();
        context.Source.Server.playerManager.addToWhitelist(target);
        context.Source.LogOp("Added " + target + " to white-list");
        return 1;
    }

    private static int WhitelistRemove(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player").ToLower();
        context.Source.Server.playerManager.removeFromWhitelist(target);
        context.Source.LogOp("Removed " + target + " from white-list");
        return 1;
    }
}
