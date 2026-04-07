using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class SeedCommand : Command.Command
{
    public string Usage => "seed";
    public string Description => "Prints the world seed";
    public string[] Names => ["seed"];

    public void Execute(Command.Command.CommandSource c)
    {
        long seed = c.Server.worlds[0].Seed;
        c.Output.SendMessage($"Seed: {seed}");
    }
}
