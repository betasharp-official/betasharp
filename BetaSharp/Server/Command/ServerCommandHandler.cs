using BetaSharp.Server.Commands;
using BetaSharp.Server.Internal;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Command;

internal class ServerCommandHandler
{
    private static readonly ILogger<ServerCommandHandler> s_logger = Log.Instance.For<ServerCommandHandler>();

    private readonly BetaSharpServer _server;

    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly HelpCommand _helpCommand = new();

    public ServerCommandHandler(BetaSharpServer server)
    {
        _server = server;
        ItemLookup.Initialize();
        RegisterAllCommands();
    }

    public void ExecuteCommand(PendingCommand pendingCommand)
    {
        string input = pendingCommand.CommandAndArgs;
        ICommandOutput output = pendingCommand.Output;
        string senderName = output.Name;

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string commandName = parts[0].ToLower();
        string[] args = parts.Length > 1 ? parts[1..] : [];

        bool isInternalServer = _server is InternalServer;

        if (_commands.TryGetValue(commandName, out var command))
        {
            if (isInternalServer && command.DisallowInternalServer)
            {
                output.SendMessage("This command is not available in singleplayer.");
            }
            else if (command.PermissionLevel == 0 || isInternalServer || command.PermissionLevel <= pendingCommand.Output.PermissionLevel)
            {
                command.Execute(new ICommand.CommandContext(_server, senderName, args, output));
            }
            else
            {
                s_logger.LogInformation($"{senderName} tried command: {input}");
                output.SendMessage($"§cYou do not have permission to use this command.");
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

        // Player commands
        Register(new KillSelfCommand());
        Register(new HealCommand());
        Register(new ClearCommand());
        Register(new TeleportCommand());
        Register(new TeleportDimensionCommand());
        Register(new GiveCommand());
        Register(new GameModeCommand());

        // Info commands
        Register(new ListCommand());
        Register(new DataCommand());

        // World commands
        Register(new TimeCommand());
        Register(new WeatherCommand());
        Register(new SummonCommand());
        Register(new KillAllCommand());
        Register(new GameRuleCommand());
        Register(new SeedCommand());

        // Chat commands
        Register(new SayCommand());
        Register(new TellCommand());

        // Admin commands
        Register(new StopCommand());
        Register(new SaveAllCommand());
        Register(new SaveOnCommand());
        Register(new SaveOffCommand());
        Register(new OpCommand());
        Register(new DeopCommand());
        Register(new BanCommand());
        Register(new PardonCommand());
        Register(new BanIpCommand());
        Register(new PardonIpCommand());
        Register(new KickCommand());
        Register(new WhitelistCommand());
    }

    public void Register(ICommand command)
    {
        foreach (string name in command.Names)
        {
            _commands[name] = command;
        }

        _helpCommand.Add(command);
    }

    /// <summary>
    /// Gets all available command names
    /// </summary>
    public List<string> GetAvailableCommandNames()
    {
        return _commands.Keys.ToList();
    }
}
