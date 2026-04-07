using BetaSharp.Worlds.Core;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class WeatherCommand : Command.Command
{
    public override string Usage => "weather <clear|rain|storm>";
    public override string Description => "Sets the weather";
    public override string[] Names => ["weather"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(Literal("clear").Executes(SetClear))
            .Then(Literal("rain").Executes(SetRain))
            .Then(Literal("storm").Executes(SetStorm));

    private static int SetClear(CommandContext<CommandSource> context)
    {
        foreach (ServerWorld world in context.Source.Server.worlds)
        {
            world.Entities.GlobalEntities.Clear();
            world.Properties.IsRaining = false;
            world.Properties.IsThundering = false;
        }

        context.Source.Output.SendMessage("Weather set to clear.");
        return 1;
    }

    private static int SetRain(CommandContext<CommandSource> context)
    {
        foreach (ServerWorld world in context.Source.Server.worlds)
        {
            world.Properties.IsRaining = true;
            world.Properties.IsThundering = false;
        }

        context.Source.Output.SendMessage("Weather set to rain.");
        return 1;
    }

    private static int SetStorm(CommandContext<CommandSource> context)
    {
        foreach (ServerWorld world in context.Source.Server.worlds)
        {
            world.Properties.IsRaining = true;
            world.Properties.IsThundering = true;
        }

        context.Source.Output.SendMessage("Weather set to storm.");
        return 1;
    }
}
