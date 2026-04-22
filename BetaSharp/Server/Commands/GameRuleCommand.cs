using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Worlds.Core;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class GameRuleCommand : Command.Command
{
    public override string Usage => "gamerule [rule name] [value]";
    public override string Description => "Gets or sets a game rule";
    public override string[] Names => ["gamerule"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Executes(ListRules)
            .Then(ArgumentString("rule")
                .Executes(GetRule)
                .Then(ArgumentString("value").Executes(SetRule)));

    private static int ListRules(CommandContext<CommandSource> context)
    {
        (RuleSet rules, RuleRegistry registry) = GetContext(context);
        context.Source.Output.SendMessage("Available Game Rules:");
        foreach (IGameRule rule in registry.All)
        {
            IRuleValue val = rules.Get(rule.Key);
            context.Source.Output.SendMessage($"  {rule.Key} = {rule.Serialize(val)}");
        }

        return 1;
    }

    private static int GetRule(CommandContext<CommandSource> context)
    {
        (RuleSet rules, RuleRegistry registry) = GetContext(context);
        string ruleName = context.GetArgument<string>("rule");
        ResourceLocation key = ResourceLocation.Parse(ruleName);

        if (registry.TryGet(key, out IGameRule? rule))
        {
            IRuleValue val = rules.Get(key);
            context.Source.Output.SendMessage($"{ruleName} = {rule.Serialize(val)}");
        }
        else
        {
            context.Source.Output.SendMessage($"Unknown game rule: {ruleName}");
        }

        return 1;
    }

    private static int SetRule(CommandContext<CommandSource> context)
    {
        (RuleSet rules, RuleRegistry registry) = GetContext(context);
        string ruleName = context.GetArgument<string>("rule");
        string valueStr = context.GetArgument<string>("value");
        ResourceLocation key = ResourceLocation.Parse(ruleName);

        if (!registry.TryGet(key, out IGameRule? _))
        {
            context.Source.Output.SendMessage($"Unknown game rule: {ruleName}");
            return 1;
        }

        try
        {
            if (rules.TrySet(key, valueStr))
            {
                context.Source.Output.SendMessage($"Game rule {ruleName} has been updated to {valueStr}");
                context.Source.LogOp($"Set game rule {ruleName} to {valueStr}");
            }
            else
            {
                context.Source.Output.SendMessage($"Failed to parse value '{valueStr}' for game rule {ruleName}");
            }
        }
        catch (Exception ex)
        {
            context.Source.Output.SendMessage($"Error setting game rule: {ex.Message}");
        }

        return 1;
    }

    private static (RuleSet rules, RuleRegistry registry) GetContext(CommandContext<CommandSource> context)
    {
        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        ServerWorld world = player != null ? context.Source.Server.getWorld(player.dimensionId) : context.Source.Server.worlds[0];
        return (world.Rules, RuleRegistry.Instance);
    }
}
