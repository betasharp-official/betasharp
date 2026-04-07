using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class DeopCommand : Command.Command
{
    public override string Usage => "deop <player>";
    public override string Description => "Removes operator status";
    public override string[] Names => ["deop"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player");
        context.Source.Server.playerManager.removeFromOperators(target);
        context.Source.Server.playerManager.messagePlayer(target, "§eYou are no longer op!");
        context.Source.LogOp("De-opping " + target);
        return 1;
    }
}
