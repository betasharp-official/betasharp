using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class PardonIpCommand : Command.Command
{
    public override string Usage => "pardon-ip <ip>";
    public override string Description => "Pardons an IP address";
    public override string[] Names => ["pardon-ip"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("ip").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string ip = context.GetArgument<string>("ip");
        context.Source.Server.playerManager.unbanIp(ip);
        context.Source.LogOp("Pardoning ip " + ip);
        return 1;
    }
}
