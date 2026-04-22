using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class BanIpCommand : Command.Command
{
    public override string Usage => "ban-ip <ip>";
    public override string Description => "Bans an IP address";
    public override string[] Names => ["ban-ip"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentString("ip").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string ip = context.GetArgument<string>("ip");
        context.Source.Server.playerManager.banIp(ip);
        context.Source.LogOp("Banning ip " + ip);
        return 1;
    }
}
