using BetaSharp.Server.Command;
using BetaSharp.Worlds.Core;

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
            if (!TryParseTimeValue(c.Args[1], out long timeValue))
            {
                c.Output.SendMessage("Invalid time value: " + c.Args[1]);
                return;
            }

            for (int i = 0; i < c.Server.worlds.Length; i++)
            {
                ServerWorld world = c.Server.worlds[i];
                if (mode == "add")
                {
                    world.SetTime(world.GetTime() + timeValue);
                }
                else
                {
                    world.SetTime(timeValue);
                }
            }

            string message = mode == "add" ? $"Added {timeValue} to time" : $"Set time to {timeValue}";
            c.Output.SendMessage(message);
            c.LogOp( message);
            return;
        }

        if (c.Args.Length == 1 && TryParseTimeValue(c.Args[0], out long namedTime))
        {
            for (int i = 0; i < c.Server.worlds.Length; i++)
            {
                c.Server.worlds[i].SetTime(namedTime);
            }

            c.Output.SendMessage($"Time set to {c.Args[0]} ({namedTime})");
            c.LogOp( $"Set time to {namedTime}");
            return;
        }

        c.Output.SendMessage("Usage: time <set|add> <value>  or  time <named_time>");
        c.Output.SendMessage("Named values: sunrise, morning, noon, sunset, night, midnight");
    }

    internal static bool TryParseTimeValue(string input, out long time)
    {
        time = input.ToLower() switch
        {
            "sunrise" or "dawn" => 0,
            "morning" => 1000,
            "noon" or "day" => 6000,
            "sunset" or "dusk" => 12000,
            "night" => 13000,
            "midnight" => 18000,
            _ => -1
        };

        if (time >= 0) return true;

        if (long.TryParse(input, out time))
        {
            return true;
        }

        time = 0;
        return false;
    }
}
