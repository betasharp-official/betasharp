using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class ReloadCommand : Command.Command
{
    public override string Usage => "reload";
    public override string Description => "Reloads all datapacks";
    public override string[] Names => ["reload"];
    public override byte PermissionLevel => 4;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        context.Source.Server.ReloadDatapacks();
        return 1;
    }
}
