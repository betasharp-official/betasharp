using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Server.Command;
using BetaSharp.Worlds.Core;

namespace BetaSharp.Server.Commands;

public class GameRuleCommand : ICommand
{
    public string Usage => "gamerule [rule name] [value]";
    public string Description => "Gets or sets a game rule";
    public string[] Names => ["gamerule"];

    public void Execute(ICommand.CommandContext c)
    {
        ServerPlayerEntity? player = c.Server.playerManager.getPlayer(c.SenderName);
        ServerWorld world = player != null ? c.Server.getWorld(player.dimensionId) : c.Server.worlds[0];
        RuleSet rules = world.Rules;
        RuleRegistry registry = RuleRegistry.Instance;

        if (c.Args.Length == 0)
        {
            c.Output.SendMessage("Available Game Rules:");
            foreach (IGameRule rule in registry.All)
            {
                IRuleValue val = rules.Get(rule.Key);
                c.Output.SendMessage($"  {rule.Key} = {rule.Serialize(val)}");
            }
            return;
        }

        if (c.Args.Length == 1)
        {
            string ruleName = c.Args[0];
            ResourceLocation key = ResourceLocation.Parse(ruleName);
            if (registry.TryGet(key, out IGameRule? rule))
            {
                IRuleValue val = rules.Get(key);
                c.Output.SendMessage($"{ruleName} = {rule.Serialize(val)}");
            }
            else
            {
                c.Output.SendMessage($"Unknown game rule: {ruleName}");
            }
            return;
        }

        if (c.Args.Length >= 2)
        {
            string ruleName = c.Args[0];
            string valueStr = c.Args[1];
            ResourceLocation key = ResourceLocation.Parse(ruleName);

            if (!registry.TryGet(key, out IGameRule? _))
            {
                c.Output.SendMessage($"Unknown game rule: {ruleName}");
                return;
            }

            try
            {
                if (rules.TrySet(key, valueStr))
                {
                    c.Output.SendMessage($"Game rule {ruleName} has been updated to {valueStr}");
                    c.LogOp($"Set game rule {ruleName} to {valueStr}");
                }
                else
                {
                    c.Output.SendMessage($"Failed to parse value '{valueStr}' for game rule {ruleName}");
                }
            }
            catch (Exception ex)
            {
                c.Output.SendMessage($"Error setting game rule: {ex.Message}");
            }
        }
    }
}
