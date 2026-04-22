using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class PardonCommand : Command.Command
{
    public override string Usage => "pardon <player>";
    public override string Description => "Pardons a player";
    public override string[] Names => ["pardon"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player");
        context.Source.Server.playerManager.unbanPlayer(target);
        context.Source.LogOp("Pardoning " + target);
        return 1;
    }
}
