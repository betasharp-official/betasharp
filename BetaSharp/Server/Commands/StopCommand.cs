using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class StopCommand : Command.Command
{
    public override string Usage => "stop";
    public override string Description => "Stops the server";
    public override string[] Names => ["stop"];
    public override byte PermissionLevel => 4;
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        context.Source.LogOp("Stopping the server..");
        context.Source.Server.Stop();
        return 1;
    }
}
