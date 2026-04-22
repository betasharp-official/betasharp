using BetaSharp.Server.Commands;
using BetaSharp.Server.Internal;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Brigadier.NET.Exceptions;

namespace BetaSharp.Server.Command;

public interface ICommandHandler
{
    public BetaSharpServer Server { get; }
    public CommandDispatcher<Command.CommandSource> Dispatcher { get; }
}

internal class ServerCommandHandler : ICommandHandler
{
    private readonly HelpCommand _helpCommand = new();

    public ServerCommandHandler(BetaSharpServer server)
    {
        Server = server;
        ItemLookup.Initialize();
        RegisterAllCommands();
    }

    public BetaSharpServer Server { get; }
    public CommandDispatcher<Command.CommandSource> Dispatcher { get; } = new();

    public void ExecuteCommand(PendingCommand pendingCommand)
    {
        ICommandOutput output = pendingCommand.Output;
        int code = 0;
        try
        {
            code = Dispatcher.Execute(pendingCommand.CommandAndArgs, new Command.CommandSource(this, output.Name, output));
        }
        catch (CommandSyntaxException e)
        {
            if (e.Type == CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand())
            {
                code = 0;
            }
            else
            {
                output.SendMessage(e.Message);
                output.SendMessage("Try /help " + GetCommandFromError(e));
                code = -1;
            }
        }

        if (code == 0)
        {
            output.SendMessage("Unknown command. Type \"/help\" for help.");
        }
    }

    private static string GetCommandFromError(CommandSyntaxException e)
    {
        if (e.Input == null || e.Cursor < 0)
        {
            return "";
        }

        int num = Math.Min(e.Input.Length, e.Cursor);
        return e.Input.Substring(0, num);
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

        // Data commands
        Register(new ReloadCommand());

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

    public void Register(Command command)
    {
        foreach (string commandName in command.Names)
        {
            Dispatcher.Register(l => command.Register(l.Literal(commandName).Requires(ctx => Requirement(command, ctx))));
        }

        _helpCommand.Add(command);
    }

    private static bool Requirement(Command cmd, Command.CommandSource ctx)
    {
        bool isInternalServer = ctx.Server is InternalServer;

        if (isInternalServer && cmd.DisallowInternalServer)
        {
            return false;
        }

        return cmd.PermissionLevel == 0 || isInternalServer || cmd.PermissionLevel <= ctx.Output.PermissionLevel;
    }
}
