using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class SaveAllCommand : Command.Command
{
    public override string Usage => "save-all";
    public override string Description => "Forces a world save";
    public override string[] Names => ["save-all"];
    public override byte PermissionLevel => 4;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        CommandSource c = context.Source;
        c.LogOp("Forcing save..");
        c.Server.playerManager?.savePlayers();

        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].SaveWithLoadingDisplay(true, null);
        }

        c.LogOp("Save complete.");
        return 1;
    }
}
