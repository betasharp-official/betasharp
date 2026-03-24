using BetaSharp.Entities;
using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class DataCommands : ICommand
{
    public string Usage => "data get ...";
    public string Description => "Get debug info from target(s)";
    public string[] Names => ["data"];


    public void Execute(ICommand.CommandContext c)
    {
        if (c.Args.Length < 2)
        {
            c.Output.SendMessage("Usage: data [get] ...");
            return;
        }

        ServerPlayerEntity player = c.Server.playerManager.getPlayer(c.SenderName);
        if (player == null)
        {
            c.Output.SendMessage("Could not find your player.");
            return;
        }


        if (c.Args[0].ToLower() == "get")
        {
            if (c.Args.Length < 2)
            {
                c.Output.SendMessage("Usage: data get [entity|player|global] <target?>");
                return;
            }

            DataGet(c, player);
        }
        else
        {
            // TODO: Add a set here
            c.Output.SendMessage(c.Args[0] + " is not supported.");
        }
    }

    private static void DataGet(ICommand.CommandContext c, ServerPlayerEntity player)
    {
        // convert short to the full string.
        switch (c.Args[1] = c.Args[1].ToLower())
        {
            case "e":
                c.Args[1] = "entity";
                break;
            case "p":
                c.Args[1] = "player";
                break;
            case "g":
                c.Args[1] = "global";
                break;
        }

        switch (c.Args[1])
        {
            case "entity":
                LogEntity(c.Server.getWorld(player.dimensionId).Entities.Entities, player, c);
                return;
            case "player":
                LogEntity(c.Server.getWorld(player.dimensionId).Entities.Players, player, c);
                return;
            case "global":
                LogEntity(c.Server.getWorld(player.dimensionId).Entities.GlobalEntities, player, c);
                return;
            default:
                c.Output.SendMessage("Usage: data get [entity|player|global] <target?>");
                return;
        }
    }

    private static void LogEntity<T>(List<T> items, ServerPlayerEntity player, ICommand.CommandContext c) where T : Entity
    {
        var sel = GetSelector(c.Args);

        if (c.Args.Length < 3)
        {
            if (items.Count != 1) FormatPlural(ref c.Args[1]);
            c.Output.SendMessage("Found " + items.Count + " " + c.Args[1]);
            return;
        }

        if (c.Args.Length < 4 && sel != Selector.None)
        {
            LogEntitySub(sel, items.ToArray(), player, c);
            return;
        }

        if (!int.TryParse(c.Args[2], out int id))
        {
            // Type comparision is faster than string comparison.
            // So if we can, compare using type.
            T[] filtered;
            if (EntityRegistry.TryGetTypeFromName(c.Args[2], out Type? type))
            {
                c.Args[1] = '_' + type.Name;
                filtered = items.Where(entity => entity.GetType() == type).ToArray();
            }
            else
            {
                c.Args[1] = '_' + c.Args[2];
                filtered = items.Where(entity => entity.GetType().Name == c.Args[2]).ToArray();
            }

            LogEntitySub(sel, filtered, player, c, true);
            return;
        }

        T? e = items.Find(entity => entity.id == id);

        if (e == null)
        {
            c.Output.SendMessage(id + " not found.");
            return;
        }

        LogEntity(e, c.Output);
    }

    private static Selector GetSelector(string[] args, int start = 2)
    {
        for (int i = start, l = args.Length; i < l; i++)
        {
            switch (args[i].ToLower())
            {
                case "first":
                    return Selector.First;
                case "close":
                    return Selector.Close;
            }
        }

        return Selector.None;
    }

    private enum Selector : byte
    {
        None = 0,
        First = 1,
        Close = 2
    }

    private static void LogEntitySub<T>(Selector sel, IEnumerable<T> items, ServerPlayerEntity player, ICommand.CommandContext c, bool listHits = false) where T : Entity
    {
        if (sel == Selector.First)
        {
            T? item = items.FirstOrDefault();
            if (item == null)
            {
                if (c.Args[1][0] == '_') c.Args[1] = c.Args[1].Substring(1);
                c.Output.SendMessage("Found 0 instances of " + c.Args[1]);
                return;
            }

            LogEntity(item, c.Output);
        }
        else if (sel == Selector.Close)
        {
            T? item = null;
            double distance = double.MaxValue;
            double distanceFast = double.MaxValue;

            foreach (T entity in items)
            {
                // Tiered distance check for faster comparison
                double d = Math.Abs(entity.x - player.x) + Math.Abs(entity.z - player.z);
                if (d * d * 1.15 > distanceFast) continue;
                var pPos = player.Position;
                var ePos = entity.Position;
                d = pPos.squareDistance2DTo(ePos);
                if (d > distanceFast) continue;
                double slowD = pPos.squareDistanceTo(ePos);
                if (slowD > distance) continue;
                if (entity.id == player.id) continue; // dont get self

                distanceFast = d;
                distance = slowD;
                item = entity;
            }

            if (item == null)
            {
                c.Output.SendMessage("0 matches");
                return;
            }

            LogEntity(item, c.Output);
        }
        else
        {
            var list = items.ToList();
            int co = list.Count();

            // output list of all items
            if (listHits && co > 0)
            {
                string listStr = "";
                foreach (T item in list)
                {
                    listStr += item.id + ", ";
                }

                c.Output.SendMessage(listStr.Substring(0, listStr.Length - 2));
            }

            if (c.Args[1][0] == '_') c.Args[1] = c.Args[1].Substring(1);
            c.Output.SendMessage("Found " + list.Count() + (co == 1 ? " instance of " : " instances of ") + c.Args[1]);
        }
    }

    private static void FormatPlural(ref string s)
    {
        if (s == "entity") s = "entities";
        else s += "s";
    }

    private static void LogEntity(Entity e, ICommandOutput output)
    {
        output.SendMessage("type: " + e.GetType().Name);
        output.SendMessage("id: " + e.id);
        output.SendMessage("isPersistent: " + e.isPersistent);
        output.SendMessage("pos: " + e.Position.ToString("F2"));
        output.SendMessage("age: " + e.age);
        output.SendMessage("dead: " + e.dead);
        if (e is EntityPlayer player)
        {
            output.SendMessage("deathTime: " + player.deathTime);
            output.SendMessage("health: " + player.health);
            output.SendMessage("name: " + player.name);
        }
        else if (e is EntityLiving living)
        {
            output.SendMessage("deathTime: " + living.deathTime);
            output.SendMessage("health: " + living.health);
        }

        if (e.passenger != null)
            output.SendMessage("Passenger: " + e.passenger.id);
        if (e.vehicle != null)
            output.SendMessage("Vehicle: " + e.vehicle.id);
    }
}
