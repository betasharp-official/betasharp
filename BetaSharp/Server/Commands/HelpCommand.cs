using BetaSharp.Server.Command;
using BetaSharp.Server.Internal;

namespace BetaSharp.Server.Commands;

public class HelpCommand : ICommand
{
    public string Usage => "help";
    public string Description => "Lists commands";
    public string[] Names => ["help", "h", "?"];
    public byte PermissionLevel => 0;

    private readonly List<ICommand> _helpEntries = [];


    public void Execute(ICommand.CommandContext c)
    {
        bool inInternalServer = c.Server is InternalServer;
        byte per = inInternalServer ? (byte)4 : c.Output.PermissionLevel;
        if (c.Args.Length > 0)
        {
            string arg = c.Args[0];

            foreach (var cmd in _helpEntries)
            {
                if (per < cmd.PermissionLevel) continue;
                if (inInternalServer && cmd.DisallowInternalServer) continue;
                if (cmd.Names.All(n => n != arg)) continue;

                c.Output.SendMessage($"{cmd.Usage} - {cmd.Description}");
                return;
            }

            c.Output.SendMessage($"Command \"{arg}\" not found, use /help to list all commands");
            return;
        }

        c.Output.SendMessage("Available commands:");
        foreach (var cmd in _helpEntries)
        {
            if (per < cmd.PermissionLevel) continue;
            if (inInternalServer && cmd.DisallowInternalServer) continue;
            c.Output.SendMessage($"  {cmd.Usage,-30} - {cmd.Description}");
        }
    }

    internal void Add(ICommand command)
    {
        _helpEntries.Add(command);
    }
}
