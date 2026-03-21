using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class HelpCommand : ICommand
{
    public string Usage => "help";
    public string Description => "Lists commands";
    public string[] Names => ["help", "h", "?"];
    public byte PermissionLevel => 0;

    [Obsolete]
    private readonly List<(string usage, string description)> _helpEntriesObsolete = [];
    private readonly List<ICommand> _helpEntries = [];


    public void Execute(ICommand.CommandContext c)
    {
        c.Output.SendMessage("Available commands:");
        byte per = c.Output.GetPermissionLevel();
        foreach (var cmd in _helpEntries)
        {
            if (per >= cmd.PermissionLevel)
                c.Output.SendMessage($"  {cmd.Usage,-30} - {cmd.Description}");
        }

        foreach (var (usage, description) in _helpEntriesObsolete)
        {
            c.Output.SendMessage($"  {usage,-30} - {description}");
        }
    }

    internal void Add(ICommand command)
    {
        _helpEntries.Add(command);
    }

    [Obsolete("Use Add(ICommand) instead")]
    internal void Add(string usage, string description)
    {
        _helpEntriesObsolete.Add((usage, description));
    }
}
