using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class BanCommand : Command.Command
{
    public override string Usage => "ban <player>";
    public override string Description => "Bans a player";
    public override string[] Names => ["ban"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player");
        context.Source.Server.playerManager.banPlayer(target);
        context.Source.LogOp("Banning " + target);
        context.Source.Server.playerManager.getPlayer(target)?.NetworkHandler.disconnect("Banned by admin");
        return 1;
    }
}
