using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class KillAllCommand : Command.Command
{
    public override string Usage => "killall <all|mob|hostile|friendly|item|tnt> <filter>";
    public override string Description => "Kills entities by type";
    public override string[] Names => ["killall"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Executes(ctx => KillAll(ctx, (byte)TypeFilter.All, ""))
            .Then(ArgumentEnum<TypeFilter>("type")
                .Executes(ctx => KillAll(ctx, (byte)ctx.GetArgument<TypeFilter>("type"), ""))
                .Then(ArgumentString("filter")).Executes(ctx => KillAll(ctx, (byte)ctx.GetArgument<TypeFilter>("type"), ctx.GetArgument<string>("filter")))
            );

    private static int KillAll(CommandContext<CommandSource> context, byte type, string filter)
    {
        filter = filter.ToLower();
        int count = 0;

        for (int w = 0; w < context.Source.Server.worlds.Length; w++)
        {
            ServerWorld world = context.Source.Server.worlds[w];
            List<Entity> entities = new(world.Entities.Entities);

            foreach (Entity entity in entities)
            {
                if (entity is EntityPlayer)
                {
                    continue;
                }

                bool shouldKill = type switch
                {
                    (byte)TypeFilter.All => true,
                    (byte)TypeFilter.Mob => entity is EntityLiving,
                    (byte)TypeFilter.Hostile => entity is EntityMonster,
                    (byte)TypeFilter.Friendly => entity is EntityAnimal,
                    (byte)TypeFilter.Item => entity is EntityItem,
                    (byte)TypeFilter.Tnt => entity is EntityTntPrimed,
                    _ => EntityRegistry.GetId(entity)?.Equals(filter, StringComparison.OrdinalIgnoreCase) ?? false
                };

                if (shouldKill)
                {
                    world.Entities.Remove(entity);
                    count++;
                }
            }
        }

        if (type == 255)
        {
            context.Source.Output.SendMessage($"Killed {count} entities (filter: {filter}).");
        }
        else if (type == (byte)TypeFilter.All)
        {
            context.Source.Output.SendMessage($"Killed {count} entities.");
        }
        else
        {
            context.Source.Output.SendMessage($"Killed {count} {(TypeFilter)type}s.");
        }

        return 1;
    }

    private enum TypeFilter : byte
    {
        All = 0,
        A = 0,
        Mob = 1,
        M = 1,
        Living = 1,
        Hostile = 2,
        H = 2,
        Monster = 2,
        Friendly = 3,
        F = 3,
        Animal = 3,
        Item = 4,
        I = 4,
        Tnt = 5,
        T = 5
    }
}
