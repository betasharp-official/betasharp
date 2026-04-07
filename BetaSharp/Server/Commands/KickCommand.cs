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
        argBuilder.Then(ArgumentString("player").Executes(Execute));

    private static int Execute(CommandContext<CommandSource> context)
    {
        string target = context.GetArgument<string>("player");
        ServerPlayerEntity? targetPlayer = context.Source.Server.playerManager.getPlayer(target);

        if (targetPlayer != null)
        {
            targetPlayer.NetworkHandler.disconnect("Kicked by admin");
            context.Source.LogOp("Kicking " + targetPlayer.name);
        }
        else
        {
            context.Source.Output.SendMessage("Can't find user " + target + ". No kick.");
        }

        return 1;
    }
}
