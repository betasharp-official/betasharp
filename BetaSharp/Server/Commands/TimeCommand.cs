using BetaSharp.Server.Command;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Commands;

public class TimeCommand : ICommand
{
    public string Usage => "time <set|add> <value>";
    public string Description => "Sets the world time";
    public string[] Names => ["time", "settime"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: time <set|add> <value>  or  time <named_time>");
            return;
        }

        if (c.Args.Length >= 2 && (c.Args[0].Equals("set", StringComparison.OrdinalIgnoreCase) ||
                                 c.Args[0].Equals("add", StringComparison.OrdinalIgnoreCase)))
        {
            string mode = c.Args[0].ToLower();
            if (!WorldCommands.TryParseTimeValue(c.Args[1], out long timeValue))
            {
                c.Output.SendMessage("Invalid time value: " + c.Args[1]);
                return;
            }

            for (int i = 0; i < c.Server.worlds.Length; i++)
            {
                ServerWorld world = c.Server.worlds[i];
                if (mode == "add")
                {
                    world.synchronizeTimeAndUpdates(world.getTime() + timeValue);
                }
                else
                {
                    world.synchronizeTimeAndUpdates(timeValue);
                }
            }

            string message = mode == "add" ? $"Added {timeValue} to time" : $"Set time to {timeValue}";
            c.Output.SendMessage(message);
            AdminCommands.LogCommand(c.Server, c.SenderName, message);
            return;
        }

        if (c.Args.Length == 1 && WorldCommands.TryParseTimeValue(c.Args[0], out long namedTime))
        {
            for (int i = 0; i < c.Server.worlds.Length; i++)
            {
                c.Server.worlds[i].synchronizeTimeAndUpdates(namedTime);
            }

            c.Output.SendMessage($"Time set to {c.Args[0]} ({namedTime})");
            AdminCommands.LogCommand(c.Server, c.SenderName, $"Set time to {namedTime}");
            return;
        }

        c.Output.SendMessage("Usage: time <set|add> <value>  or  time <named_time>");
        c.Output.SendMessage("Named values: sunrise, morning, noon, sunset, night, midnight");
    }
}
