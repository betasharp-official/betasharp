using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class SummonCommand : ICommand
{
    public string Usage => "summon <entity>";
    public string Description => "Spawns an entity at your location";
    public string[] Names => ["summon", "spawn"];

    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 1)
        {
            c.Output.SendMessage("Usage: summon <entity_name> [count]");
            return;
        }

        ServerPlayerEntity? player = c.Server.playerManager.getPlayer(c.SenderName);
        if (player == null)
        {
            c.Output.SendMessage("Could not find your player.");
            return;
        }

        string entityName = c.Args[0];

        int count = 1;
        if (c.Args.Length >= 2)
        {
            if (!int.TryParse(c.Args[1], out count) || count < 1)
            {
                c.Output.SendMessage("Invalid count. Must be a positive number.");
                return;
            }
        }

        var world = c.Server.getWorld(player.dimensionId);
        int summoned = 0;

        for (int i = 0; i < count; i++)
        {
            Entity? entity = EntityRegistry.CreateEntityAt(entityName, world, (float)player.x, (float)player.y, (float)player.z);
            if (entity != null)
            {
                summoned++;
            }
        }

        if (summoned > 0)
        {
            c.Output.SendMessage($"Summoned {summoned}x {entityName}");
        }
        else
        {
            c.Output.SendMessage($"Unknown entity: {entityName}");
        }
    }
}
