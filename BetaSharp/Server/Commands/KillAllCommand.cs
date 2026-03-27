using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class KillAllCommand : ICommand
{
    public string Usage => "killall [filter]";
    public string Description => "Kills entities by type";
    public string[] Names => ["killall"];

    public void Execute(ICommand.CommandContext c)
    {
        string filter = c.Args.Length > 0 ? c.Args[0].ToLower() : "all";
        int count = 0;

        for (int w = 0; w < c.Server.worlds.Length; w++)
        {
            var world = c.Server.worlds[w];
            var entities = new List<Entity>(world.Entities.Entities);

            foreach (Entity entity in entities)
            {
                if (entity is EntityPlayer) continue;

                bool shouldKill = filter switch
                {
                    "all" => true,
                    "living" or "mob" => entity is EntityLiving,
                    "monster" => entity is EntityMonster,
                    "animal" => entity is EntityAnimal,
                    "item" => entity is EntityItem,
                    "tnt" => entity is EntityTNTPrimed,
                    _ => EntityRegistry.GetId(entity)?.Equals(filter, System.StringComparison.OrdinalIgnoreCase) ?? false
                };

                if (shouldKill)
                {
                    world.Entities.Remove(entity);
                    count++;
                }
            }
        }

        c.Output.SendMessage($"Killed {count} entities (filter: {filter}).");
    }
}
