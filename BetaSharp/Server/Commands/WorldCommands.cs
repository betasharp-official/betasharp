using BetaSharp.Entities;
using BetaSharp.Rules;
using BetaSharp.Worlds;

namespace BetaSharp.Server.Commands;

internal static class WorldCommands
{
    public static void Time(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 1)
        {
            output.SendMessage("Usage: time <set|add> <value>  or  time <named_time>");
            return;
        }

        if (args.Length >= 2 && (args[0].Equals("set", StringComparison.OrdinalIgnoreCase) ||
                                 args[0].Equals("add", StringComparison.OrdinalIgnoreCase)))
        {
            string mode = args[0].ToLower();
            if (!TryParseTimeValue(args[1], out long timeValue))
            {
                output.SendMessage("Invalid time value: " + args[1]);
                return;
            }

            for (int i = 0; i < server.worlds.Length; i++)
            {
                ServerWorld world = server.worlds[i];
                if (mode == "add")
                {
                    world.synchronizeTimeAndUpdates(world.getTime() + timeValue);
                }
                else
                {
                    world.synchronizeTimeAndUpdates(timeValue);
                }
            }

            string message = mode == "add" ? $"Added {timeValue} to time" : $"Set time to {timeValue}";
            output.SendMessage(message);
            AdminCommands.LogCommand(server, senderName, message);
            return;
        }

        if (args.Length == 1 && TryParseTimeValue(args[0], out long namedTime))
        {
            for (int i = 0; i < server.worlds.Length; i++)
            {
                server.worlds[i].synchronizeTimeAndUpdates(namedTime);
            }

            output.SendMessage($"Time set to {args[0]} ({namedTime})");
            AdminCommands.LogCommand(server, senderName, $"Set time to {namedTime}");
            return;
        }

        output.SendMessage("Usage: time <set|add> <value>  or  time <named_time>");
        output.SendMessage("Named values: sunrise, morning, noon, sunset, night, midnight");
    }

    public static void Weather(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 1) { output.SendMessage("Usage: weather <clear|rain|storm>"); return; }

        string weather = args[0].ToLower();
        for (int i = 0; i < server.worlds.Length; i++)
        {
            ServerWorld world = server.worlds[i];
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
                    output.SendMessage("Unknown weather type. Use: clear, rain, or storm");
                    return;
            }
        }

        output.SendMessage($"Weather set to {weather}.");
    }

    public static void Summon(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        if (args.Length < 1)
        {
            output.SendMessage("Usage: summon <entity_name> [count]");
            return;
        }

        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        if (player == null)
        {
            output.SendMessage("Could not find your player.");
            return;
        }

        string entityName = args[0];

        int count = 1;
        if (args.Length >= 2)
        {
            if (!int.TryParse(args[1], out count) || count < 1)
            {
                output.SendMessage("Invalid count. Must be a positive number.");
                return;
            }
        }

        ServerWorld world = server.getWorld(player.dimensionId);
        int summoned = 0;

        for (int i = 0; i < count; i++)
        {
            Entity? entity = EntityRegistry.createEntityAt(entityName, world, (float)player.x, (float)player.y, (float)player.z);
            if (entity != null)
            {
                summoned++;
            }
        }

        if (summoned > 0)
        {
            output.SendMessage($"Summoned {summoned}x {entityName}");
        }
        else
        {
            output.SendMessage($"Unknown entity: {entityName}");
        }
    }

    public static void KillAll(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        string filter = args.Length > 0 ? args[0].ToLower() : "all";
        int count = 0;

        for (int w = 0; w < server.worlds.Length; w++)
        {
            ServerWorld world = server.worlds[w];
            var entities = new List<Entity>(world.entities);

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
                    world.Remove(entity);
                    count++;
                }
            }
        }

        output.SendMessage($"Killed {count} entities (filter: {filter}).");
    }

    internal static bool TryParseTimeValue(string input, out long time)
    {
        time = input.ToLower() switch
        {
            "sunrise" or "dawn" => 0,
            "morning" => 1000,
            "noon" or "day" => 6000,
            "sunset" or "dusk" => 12000,
            "night" => 13000,
            "midnight" => 18000,
            _ => -1
        };

        if (time >= 0) return true;

        if (long.TryParse(input, out time))
        {
            return true;
        }

        time = 0;
        return false;
    }

    public static void GameRule(MinecraftServer server, string senderName, string[] args, CommandOutput output)
    {
        ServerPlayerEntity player = server.playerManager.getPlayer(senderName);
        ServerWorld world = player != null ? server.getWorld(player.dimensionId) : server.worlds[0];
        RuleSet rules = world.Rules;
        RuleRegistry registry = RuleRegistry.Instance;

        if (args.Length == 0)
        {
            output.SendMessage("Available Game Rules:");
            foreach (IGameRule rule in registry.All)
            {
                IRuleValue val = rules.Get(rule.Key);
                output.SendMessage($"  {rule.Key} = {rule.Serialize(val)}");
            }
            return;
        }

        if (args.Length == 1)
        {
            string ruleName = args[0];
            ResourceLocation key = ResourceLocation.Parse(ruleName);
            if (registry.TryGet(key, out IGameRule? rule))
            {
                IRuleValue val = rules.Get(key);
                output.SendMessage($"{ruleName} = {rule.Serialize(val)}");
            }
            else
            {
                output.SendMessage($"Unknown game rule: {ruleName}");
            }
            return;
        }

        if (args.Length >= 2)
        {
            string ruleName = args[0];
            string valueStr = args[1];
            ResourceLocation key = ResourceLocation.Parse(ruleName);

            if (!registry.TryGet(key, out IGameRule? _))
            {
                output.SendMessage($"Unknown game rule: {ruleName}");
                return;
            }

            try
            {
                if (rules.TrySet(key, valueStr))
                {
                    output.SendMessage($"Game rule {ruleName} has been updated to {valueStr}");
                    AdminCommands.LogCommand(server, senderName, $"Set game rule {ruleName} to {valueStr}");
                }
                else
                {
                    output.SendMessage($"Failed to parse value '{valueStr}' for game rule {ruleName}");
                }
            }
            catch (Exception ex)
            {
                output.SendMessage($"Error setting game rule: {ex.Message}");
            }
        }
    }
}
