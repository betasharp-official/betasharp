using BetaSharp.Entities;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class HealCommand : Command.Command
{
    public override string Usage => "heal <player> <amount>";
    public override string Description => "Heals yourself";
    public override string[] Names => ["heal"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Executes(HealFull)
            .Then(ArgumentInt("amount").Executes(HealAmount))
            .Then(ArgumentPlayer("player")
                .Executes(HealFullOther)
                .Then(ArgumentInt("amount").Executes(HealAmountOther)));

    private static int HealFull(CommandContext<CommandSource> context) => Heal(context, 255);

    private static int HealAmount(CommandContext<CommandSource> context) => Heal(context, context.GetArgument<int>("amount"));

    private static int HealFullOther(CommandContext<CommandSource> context) => Heal(context, context.GetArgument<ServerPlayerEntity>("player"), 255);
    private static int HealAmountOther(CommandContext<CommandSource> context) => Heal(context, context.GetArgument<ServerPlayerEntity>("player"), context.GetArgument<int>("amount"));

    private static int Heal(CommandContext<CommandSource> context, int amount)
    {
        if (amount <= 0)
        {
            context.Source.Output.SendMessage("Heal must be a positive number.");
            return 1;
        }

        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (player == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 1;
        }

        int old = player.health;
        player.heal(amount);
        context.Source.Output.SendMessage($"Healed for {player.health - old} health.");
        return 1;
    }

    private static int Heal(CommandContext<CommandSource> context, ServerPlayerEntity player, int amount)
    {
        if (amount <= 0)
        {
            context.Source.Output.SendMessage("Heal must be a positive number.");
            return 1;
        }

        int old = player.health;
        player.heal(amount);
        context.Source.Output.SendMessage($"Healed {player.name} for {player.health - old} health.");
        return 1;
    }
}
