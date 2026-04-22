using BetaSharp.Worlds.Core;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class TimeCommand : Command.Command
{
    public override string Usage => "time <set|add> <value>";
    public override string Description => "Sets the world time";
    public override string[] Names => ["time", "settime"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(Literal("set")
                .Then(ArgumentInt("value").Executes(c => TimeSet(c, c.GetArgument<int>("value"))))
                .Then(ArgumentEnum<Time>("time of day").Executes(c => TimeSet(c, (int)c.GetArgument<Time>("time of day"))))
            )
            .Then(Literal("add")
                .Then(ArgumentInt("value").Executes(c => TimeAdd(c, c.GetArgument<int>("value"))))
                .Then(ArgumentEnum<Time>("time of day").Executes(c => TimeAdd(c, (int)c.GetArgument<Time>("time of day"))))
            )
            .Then(ArgumentInt("value").Executes(c => TimeSet(c, c.GetArgument<int>("value"))))
            .Then(ArgumentEnum<Time>("time of day").Executes(c => TimeSet(c, (int)c.GetArgument<Time>("time of day"))));


    private static int TimeSet(CommandContext<CommandSource> context, int time)
    {
        foreach (ServerWorld world in context.Source.Server.worlds)
        {
            world.SetTime(time);
        }

        string msg = $"Set time to {time}";
        context.Source.Output.SendMessage(msg);
        context.Source.LogOp(msg);
        return 1;
    }


    private static int TimeAdd(CommandContext<CommandSource> context, int time)
    {
        foreach (ServerWorld world in context.Source.Server.worlds)
        {
            world.SetTime(world.GetTime() + time);
        }

        string msg = $"Added {time} to time";
        context.Source.Output.SendMessage(msg);
        context.Source.LogOp(msg);
        return 1;
    }

    private enum Time
    {
        Dawn = 0,
        Sunrise = 0,
        Morning = 1000,
        Day = 6000,
        Noon = 6000,
        Sunset = 12000,
        Dusk = 12000,
        Night = 13000,
        Midnight = 18000
    }
}
