using BetaSharp.Entities;

namespace BetaSharp.Server.Commands;

public static class DataCommands
{
    public static void Data(BetaSharpServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 2)
        {
            output.SendMessage("Usage: data [get] ...");
            return;
        }

        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        if (player == null)
        {
            output.SendMessage("Could not find your player.");
            return;
        }


        if (args[0].ToLower() == "get")
        {
            if (args.Length < 2)
            {
                output.SendMessage("Usage: data get [entity|player|global] <target?>");
                return;
            }

            DataGet(server, player, args, output);
        }
        else
        {
            // TODO: Add a set here
            output.SendMessage(args[0] + " is not supported.");
        }
    }

    private static void DataGet(BetaSharpServer server, ServerPlayerEntity player, string[] args, CommandOutput output)
    {
        // convert short to the full string.
        switch (args[1] = args[1].ToLower())
        {
            case "e":
                args[1] = "entity";
                break;
            case "p":
                args[1] = "player";
                break;
            case "g":
                args[1] = "global";
                break;
        }

        switch (args[1])
        {
            case "entity":
                LogEntity(server.getWorld(player.dimensionId).entities, player, args, output);
                return;
            case "player":
                LogEntity(server.getWorld(player.dimensionId).players, player, args, output);
                return;
            case "global":
                LogEntity(server.getWorld(player.dimensionId).globalEntities, player, args, output);
                return;
            default:
                output.SendMessage("Usage: data get [entity|player|global] <target?>");
                return;
        }
    }

    private static void LogEntity<T>(List<T> items, ServerPlayerEntity player, string[] args, CommandOutput output) where T : Entity
    {
        var sel = GetSelector(args);

        if (args.Length < 3)
        {
            if (items.Count != 1) FormatPlural(ref args[1]);
            output.SendMessage("Found " + items.Count + " " + args[1]);
            return;
        }

        if (args.Length < 4 && sel != Selector.None)
        {
            LogEntitySub(sel, items.ToArray(), player, args, output);
            return;
        }

        if (!int.TryParse(args[2], out int id))
        {
            // Type comparision is faster than string comparison.
            // So if we can, compare using type.
            T[] filtered;
            if (EntityRegistry.TryGetTypeFromName(args[2], out Type? type))
            {
                args[1] = '_' + type.Name;
                filtered = items.Where(entity => entity.GetType() == type).ToArray();
            }
            else
            {
                args[1] = '_' + args[2];
                filtered = items.Where(entity => entity.GetType().Name == args[2]).ToArray();
            }

            LogEntitySub(sel, filtered, player, args, output, true);
            return;
        }

        T? e = items.Find(entity => entity.id == id);

        if (e == null)
        {
            output.SendMessage(id + " not found.");
            return;
        }

        LogEntity(e, output);
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

    private static void LogEntitySub<T>(Selector sel, IEnumerable<T> items, ServerPlayerEntity player, string[] args, CommandOutput output, bool listHits = false) where T : Entity
    {
        if (sel == Selector.First)
        {
            T? item = items.FirstOrDefault();
            if (item == null)
            {
                if (args[1][0] == '_') args[1] = args[1].Substring(1);
                output.SendMessage("Found 0 instances of " + args[1]);
                return;
            }

            LogEntity(item, output);
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
                output.SendMessage("0 matches");
                return;
            }

            LogEntity(item, output);
        }
        else
        {
            var list = items.ToList();
            int c = list.Count();

            // output list of all items
            if (listHits && c > 0)
            {
                string listStr = "";
                foreach (T item in list)
                {
                    listStr += item.id + ", ";
                }

                output.SendMessage(listStr.Substring(0, listStr.Length - 2));
            }

            if (args[1][0] == '_') args[1] = args[1].Substring(1);
            output.SendMessage("Found " + list.Count() + (c == 1 ? " instance of " : " instances of ") + args[1]);
        }
    }

    private static void FormatPlural(ref string s)
    {
        if (s == "entity") s = "entities";
        else s += "s";
    }

    private static void LogEntity(Entity e, CommandOutput output)
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
