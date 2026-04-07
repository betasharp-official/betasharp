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
            .Executes(ctx => KillAll(ctx, (byte)TypeFilter.all, ""))
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
                    (byte)TypeFilter.all => true,
                    (byte)TypeFilter.mob => entity is EntityLiving,
                    (byte)TypeFilter.hostile => entity is EntityMonster,
                    (byte)TypeFilter.friendly => entity is EntityAnimal,
                    (byte)TypeFilter.item => entity is EntityItem,
                    (byte)TypeFilter.tnt => entity is EntityTNTPrimed,
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
        else if (type == (byte)TypeFilter.all)
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
        all = 0,
        a = 0,
        mob = 1,
        m = 1,
        living = 1,
        hostile = 2,
        h = 2,
        monster = 2,
        friendly = 3,
        f = 3,
        animal = 3,
        item = 4,
        i = 4,
        tnt = 5,
        t = 5
    }
}
