using BetaSharp.Entities;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class KickCommand : Command.Command
{
    public override string Usage => "kick <player>";
    public override string Description => "Kicks a player";
    public override string[] Names => ["kick"];
    public override bool DisallowInternalServer => true;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(ArgumentPlayer("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity targetPlayer = context.GetArgument<ServerPlayerEntity>("player");

        targetPlayer.NetworkHandler.disconnect("Kicked by admin");
        context.Source.LogOp("Kicking " + targetPlayer.name);

        return 1;
    }
}
