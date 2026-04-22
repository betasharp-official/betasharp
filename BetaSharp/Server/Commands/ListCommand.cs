using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class ListCommand : Command.Command
{
    public override string Usage => "list";
    public override string Description => "Lists connected players";
    public override string[] Names => ["list"];
    public override byte PermissionLevel => 0;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        context.Source.Output.SendMessage("Connected players: " + context.Source.Server.playerManager.getPlayerList());
        return 1;
    }
}
