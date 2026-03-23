using BetaSharp.Server.Command;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Commands;

public class WeatherCommand : ICommand
{
    public string Usage => "weather <clear|rain|storm>";
    public string Description => "Sets the weather";
    public string[] Names => ["weather"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: weather <clear|rain|storm>");
            return;
        }

        string weather = c.Args[0].ToLower();
        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            ServerWorld world = c.Server.worlds[i];
            switch (weather)
            {
                case "clear":
                    world.globalEntities.Clear();
                    world.getProperties().IsRaining = false;
                    world.getProperties().IsThundering = false;
                    break;
                case "rain":
                    world.getProperties().IsRaining = true;
                    world.getProperties().IsThundering = false;
                    break;
                case "storm":
                    world.getProperties().IsRaining = true;
                    world.getProperties().IsThundering = true;
                    break;
                default:
                    c.Output.SendMessage("Unknown weather type. Use: clear, rain, or storm");
                    return;
            }
        }

        c.Output.SendMessage($"Weather set to {weather}.");
    }
}
