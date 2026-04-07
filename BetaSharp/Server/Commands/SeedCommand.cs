using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class SeedCommand : Command.Command
{
    public override string Usage => "seed";
    public override string Description => "Prints the world seed";
    public override string[] Names => ["seed"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        long seed = context.Source.Server.worlds[0].Seed;
        context.Source.Output.SendMessage($"Seed: {seed}");
        return 1;
    }
}
