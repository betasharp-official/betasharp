using BetaSharp.Entities;
using BetaSharp.Worlds.Core;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class SummonCommand : Command.Command
{
    public override string Usage => "summon <entity> <count>";
    public override string Description => "Spawns an entity at your location";
    public override string[] Names => ["summon", "spawn"];

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(ArgumentString("entity")
                .Executes(ctx => Summon(ctx, 1))
                .Then(ArgumentInt("count").Executes(ctx => Summon(ctx, ctx.GetArgument<int>("count")))));

    private static int Summon(CommandContext<CommandSource> context, int count)
    {
        ServerPlayerEntity? player = context.Source.Server.playerManager.getPlayer(context.Source.SenderName);
        if (player == null)
        {
            context.Source.Output.SendMessage("Could not find your player.");
            return 0;
        }

        string entityName = context.GetArgument<string>("entity");
        ServerWorld world = context.Source.Server.getWorld(player.DimensionId);
        int summoned = 0;

        for (int i = 0; i < count; i++)
        {
            Entity? entity = EntityRegistry.CreateEntityAt(entityName, world, (float)player.X, (float)player.Y, (float)player.Z);
            if (entity != null)
            {
                summoned++;
            }
        }

        if (summoned > 0)
        {
            context.Source.Output.SendMessage($"Summoned {summoned}x {entityName}");
        }
        else
        {
            context.Source.Output.SendMessage($"Unknown entity: {entityName}");
        }

        return 1;
    }
}
