using BetaSharp.Server.Command;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class TickCommand : Command.Command
{
    public override string Usage => "tick [schedule|pause|unpause|rate|<ticks to run>]";
    public override string Description => "Change tick properties";
    public override string[] Names => ["tick"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Then(Literal("schedule")
                .Then(Literal("clear").Executes(ScheduleClearDimension)
                    .Then(ArgumentInt("dimensionId").Executes(ScheduleClearDimension))
                    .Then(Literal("all").Executes(ScheduleClear)))
                .Then(Literal("tick").Executes(TickScheduler).Then(ArgumentBool("forceFlush").Executes(TickScheduler))))
            .Then(Literal("pause").Executes(Pause).Then(ArgumentBool("pause").Executes(Pause)))
            .Then(Literal("unpause").Executes(Unpause))
            .Then(Literal("rate").Executes(GetTickRate).Then(ArgumentInt("tickRate").Executes(SetTickRate)))
            .Then(ArgumentInt("ticks").Executes(Tick));

    private static int ScheduleClear(CommandContext<CommandSource> context)
    {
        long count = 0;
        long newCount = 0;
        foreach (var world in context.Source.Server.worlds)
        {
            count += world.TickScheduler.Count;
            world.TickScheduler.ClearFullQueue();
            newCount += world.TickScheduler.Count;
        }

        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        sender?.SendMessage($"Cleared {count - newCount} ticks from the scheduler.");

        return 1;
    }

    private static int ScheduleClearDimension(CommandContext<CommandSource> context)
    {
        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        int dimDefault = 0;
        if (sender != null) dimDefault = sender.DimensionId;

        int dimId = context.GetArgumentOrDefault("dimensionId", dimDefault);
        var tickScheduler = context.Source.Server.getWorld(dimId).TickScheduler;
        long count = tickScheduler.Count;
        tickScheduler.ClearFullQueue();

        sender?.SendMessage($"Cleared {count - tickScheduler.Count} ticks from the scheduler.");

        return 1;
    }

    private static int TickScheduler(CommandContext<CommandSource> context)
    {
        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (sender == null) throw new Exception("Could not find sender.");
        var tickScheduler = context.Source.Server.getWorld(sender.DimensionId).TickScheduler;
        long count = tickScheduler.Count;
        tickScheduler.Tick(context.GetArgumentOrDefault("forceFlush", false));

        sender.SendMessage($"Processed {count - tickScheduler.Count} ticks from the scheduler.");
        return 1;
    }

    private static int Tick(CommandContext<CommandSource> context)
    {
        int ticks = context.GetArgumentOrDefault("ticks", 1);
        for (int i = 0; i < ticks; i++)
        {
            context.Source.Server.Tick();
        }

        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        sender?.SendMessage($"Processed {ticks} ticks.");

        return 1;
    }

    private static int Pause(CommandContext<CommandSource> context)
    {
        bool oldState = context.Source.Server.Paused;
        context.Source.Server.Paused = context.GetArgumentOrDefault("pause", true);
        return GetPause(context, oldState);
    }

    private static int Unpause(CommandContext<CommandSource> context)
    {
        bool oldState = context.Source.Server.Paused;
        context.Source.Server.Paused = false;
        return GetPause(context, oldState);
    }

    private static int GetPause(CommandContext<CommandSource> context, bool oldState)
    {
        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (sender == null) return 1;
        bool paused = context.Source.Server.Paused;
        sender.SendMessage(oldState == paused ? $"Game already {(paused ? "paused" : "unpaused")}" : $"Game {(paused ? "paused" : "unpaused")}");
        return 1;
    }

    private static int SetTickRate(CommandContext<CommandSource> context)
    {
        context.Source.Server.TickRate = context.GetArgument<int>("tickRate");
        return GetTickRate(context);
    }

    private static int GetTickRate(CommandContext<CommandSource> context)
    {
        var sender = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (sender == null) throw new Exception("Could not find sender.");
        sender.SendMessage($"Current tick rate: {context.Source.Server.TickRate}");
        return 1;
    }
}
