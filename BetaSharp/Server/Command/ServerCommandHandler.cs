using BetaSharp.Server.Commands;
using BetaSharp.Server.Internal;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Command;

internal class ServerCommandHandler
{
    private static readonly ILogger<ServerCommandHandler> s_logger = Log.Instance.For<ServerCommandHandler>();

    private readonly BetaSharpServer server;

    private delegate void CommandAction(BetaSharpServer server, string senderName, string[] args, ICommandOutput output);

    [Obsolete]
    private readonly Dictionary<string, CommandAction> _commandsAction = new();
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly HelpCommand _helpCommand = new();

    public ServerCommandHandler(BetaSharpServer server)
    {
        this.server = server;
        ItemCommands.Initialize();
        RegisterAllCommands();
    }

    public void ExecuteCommand(PendingCommand pendingCommand)
    {
        string input = pendingCommand.CommandAndArgs;
        ICommandOutput output = pendingCommand.Output;
        string senderName = output.GetName();

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string commandName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : [];

        if (_commands.TryGetValue(commandName, out var command))
        {
            if (command.PermissionLevel == 0 || server is InternalServer || command.PermissionLevel <= pendingCommand.Output.GetPermissionLevel())
            {
                command.Execute(new ICommand.CommandContext(server, senderName, args, output));
            }
            else
            {
                s_logger.LogInformation($"{senderName} tried command: {input}");
                output.SendMessage($"§cYou do not have permission to use this command.");
            }
        }
        else if (_commandsAction.TryGetValue(commandName, out var action))
        {
            if (server is InternalServer || pendingCommand.Output.GetPermissionLevel() > 0)
            {
                action(server, senderName, args, output);
            }
            else
            {
                s_logger.LogInformation($"{senderName} tried command: {input}");
                output.SendMessage("§cYou do not have permission to use this command.");
            }
        }
        else
        {
            output.SendMessage("Unknown command. Type \"help\" for help.");
        }
    }

    private void RegisterAllCommands()
    {
        Register(_helpCommand);
        Register(PlayerCommands.Kill, "kill", "kills yourself", "kill");
        Register(PlayerCommands.Heal, "heal [amount]", "heals yourself", "heal");
        Register(PlayerCommands.Clear, "clear", "clears your inventory", "clear");
        Register(PlayerCommands.Teleport, "tp <x> <y> <z> / <p1> <p2>", "teleport", "tp", "teleport");
        Register(PlayerCommands.MoveToDimension, "tpdim <id> [player]", "teleports to a dimension", "tpdim");
        Register(new DataCommands());

        Register(ItemCommands.Give, "give <item> [count]", "gives yourself an item", "give");

        Register(WorldCommands.Time, "time <set|add> <value>", "sets the world time", "time", "settime");
        Register(WorldCommands.Weather, "weather <clear|rain|storm>", "sets the weather", "weather");
        Register(WorldCommands.Summon, "summon <entity>", "spawns an entity at your location", "summon", "spawn");
        Register(WorldCommands.KillAll, "killall [filter]", "kills entities by type", "killall");
        Register(WorldCommands.GameRule, "gamerule [rule name] [value]", "gets or sets a game rule", "gamerule");
        Register(WorldCommands.Seed, "seed", "prints the world seed", "seed");

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

        _commandsAction["save-off"] = (s, sender, _, _) => AdminCommands.SaveToggle(s, sender, disable: true);
        _commandsAction["save-on"] = (s, sender, _, _) => AdminCommands.SaveToggle(s, sender, disable: false);
        _helpCommand.Add("save-off / save-on", "toggles level saving");
    }

    [Obsolete]
    private void Register(CommandAction action, string usage, string description, params string[] names)
    {
        foreach (string name in names)
        {
            _commandsAction[name] = action;
        }
        _helpCommand.Add(usage, description);
    }

    public void Register(ICommand command)
    {
        foreach (string name in command.Names)
        {
            _commands[name] = command;
        }
        _helpCommand.Add((command));
    }

    /// <summary>
    /// Gets all available command names
    /// </summary>
    public List<string> GetAvailableCommandNames()
    {
        return _commandsAction.Keys.ToList();
    }
}
