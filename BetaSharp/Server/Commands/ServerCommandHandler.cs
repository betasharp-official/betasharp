namespace BetaSharp.Server.Commands;

internal class ServerCommandHandler
{
    private readonly MinecraftServer server;

    private delegate void CommandAction(MinecraftServer server, string senderName, string[] args, CommandOutput output);

    private readonly Dictionary<string, CommandAction> commands = new();
    private readonly List<(string usage, string description)> helpEntries = [];

    public ServerCommandHandler(MinecraftServer server)
    {
        this.server = server;
        ItemCommands.Initialize();
        RegisterAllCommands();
    }

    public void ExecuteCommand(Command command)
    {
        string input = command.commandAndArgs;
        CommandOutput output = command.output;
        string senderName = output.GetName();

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string commandName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : [];

        if (commands.TryGetValue(commandName, out var action))
        {
            action(server, senderName, args, output);
        }
        else
        {
            output.SendMessage("Unknown command. Type \"help\" for help.");
        }
    }

    private void RegisterAllCommands()
    {
        Register(PlayerCommands.Kill, "kill", "kills yourself", "kill");
        Register(PlayerCommands.Heal, "heal [amount]", "heals yourself", "heal");
        Register(PlayerCommands.Clear, "clear", "clears your inventory", "clear");
        Register(PlayerCommands.Teleport, "tp <x> <y> <z> / <p1> <p2>", "teleport", "tp", "teleport");
        Register(PlayerCommands.MoveToDimension, "tpdim <id> [player]", "teleports to a dimension", "tpdim");
        Register(DataCommands.Data, "data get ...", "gives yourself an item", "data");

        Register(ItemCommands.Give, "give <item> [count]", "gives yourself an item", "give");

        Register(WorldCommands.Time, "time <set|add> <value>", "sets the world time", "time", "settime");
        Register(WorldCommands.Weather, "weather <clear|rain|storm>", "sets the weather", "weather");
        Register(WorldCommands.Summon, "summon <entity>", "spawns an entity at your location", "summon", "spawn");
        Register(WorldCommands.KillAll, "killall [filter]", "kills entities by type", "killall");
        Register(WorldCommands.GameRule, "gamerule [rule name] [value]", "gets or sets a game rule", "gamerule");

        Register(ChatCommands.Say, "say <message>", "broadcasts a message", "say");
        Register(ChatCommands.Tell, "tell <player> <message>", "whispers to a player", "tell");

        Register(AdminCommands.List, "list", "lists connected players", "list");
        Register(AdminCommands.Stop, "stop", "stops the server", "stop");
        Register(AdminCommands.SaveAll, "save-all", "forces a world save", "save-all");
        Register(AdminCommands.Op, "op <player>", "makes a player operator", "op");
        Register(AdminCommands.Deop, "deop <player>", "removes operator status", "deop");
        Register(AdminCommands.Ban, "ban <player>", "bans a player", "ban");
        Register(AdminCommands.Pardon, "pardon <player>", "pardons a player", "pardon");
        Register(AdminCommands.BanIp, "ban-ip <ip>", "bans an IP address", "ban-ip");
        Register(AdminCommands.PardonIp, "pardon-ip <ip>", "pardons an IP address", "pardon-ip");
        Register(AdminCommands.Kick, "kick <player>", "kicks a player", "kick");
        Register(AdminCommands.Whitelist, "whitelist <action> [player]", "manages the whitelist", "whitelist");

        commands["save-off"] = (s, sender, a, o) => AdminCommands.SaveToggle(s, sender, disable: true);
        commands["save-on"] = (s, sender, a, o) => AdminCommands.SaveToggle(s, sender, disable: false);
        helpEntries.Add(("save-off / save-on", "toggles level saving"));

        commands["help"] = (s, sender, a, o) => ShowHelp(o);
        commands["?"] = (s, sender, a, o) => ShowHelp(o);
        helpEntries.Insert(0, ("help / ?", "shows this message"));
    }

    private void Register(CommandAction action, string usage, string description, params string[] names)
    {
        foreach (string name in names)
        {
            commands[name] = action;
        }
        helpEntries.Add((usage, description));
    }

    private void ShowHelp(CommandOutput output)
    {
        output.SendMessage("Available commands:");
        foreach (var (usage, description) in helpEntries)
        {
            output.SendMessage($"  {usage,-30} - {description}");
        }
    }

    /// <summary>
    /// Gets all available command names
    /// </summary>
    public List<string> GetAvailableCommandNames()
    {
        return commands.Keys.ToList();
    }
}
