using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class OpCommand : Command.Command
{
    public override string Usage => "op <player>";
    public override string Description => "Makes a player operator";
    public override string[] Names => ["op"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player");
        context.Source.Server.playerManager.addToOperators(target);
        context.Source.LogOp("Opping " + target);
        context.Source.Server.playerManager.messagePlayer(target, "§eYou are now op!");
        return 1;
    }
}
