using BetaSharp.Server.Command;
using BetaSharp.Server.Internal;
using Brigadier.NET.Builder;
using Brigadier.NET.Context;
using Brigadier.NET.Tree;

namespace BetaSharp.Server.Commands;

public class HelpCommand : Command.Command
{
    private readonly List<Command.Command> _helpEntries = [];
    public override string Usage => "help <command>";
    public override string Description => "Lists commands";
    public override string[] Names => ["help", "h", "?"];
    public override byte PermissionLevel => 0;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder
            .Then(ArgumentString("command").Executes(HelpTargeted))
            .Executes(HelpAll);

    private int HelpAll(CommandContext<CommandSource> context)
    {
        CommandSource c = context.Source;
        bool inInternalServer = c.Server is InternalServer;
        byte per = inInternalServer ? (byte)4 : c.Output.PermissionLevel;

        c.Output.SendMessage("Available commands:");
        foreach (Command.Command cmd in _helpEntries)
        {
            if (per < cmd.PermissionLevel)
            {
                continue;
            }

            if (inInternalServer && cmd.DisallowInternalServer)
            {
                continue;
            }

            c.Output.SendMessage($"  {cmd.Usage,-30} - {cmd.Description}");
        }

        return 1;
    }

    private int HelpTargeted(CommandContext<CommandSource> context)
    {
        CommandSource c = context.Source;

        string arg = context.GetArgument<string>("command");
        string s = context.Input;
        s = s.Substring(s.IndexOf(' ') + 1);

        CommandNode<CommandSource>? a = c.Handler.Dispatcher.FindNode(s.Split(' '));
        if (a != null)
        {
            BuildHelp('/' + s, c.Output, a);
        }

        c.Output.SendMessage($"Command \"{arg}\" not found, use /help to list all commands");
        return 1;
    }

    private static void BuildHelp(string s, ICommandOutput o, CommandNode<CommandSource> context)
    {
        if (context.Children.Count == 0)
        {
            o.SendMessage("  " + s);
            return;
        }

        foreach (CommandNode<CommandSource> node in context.Children)
        {
            BuildHelp(s + ' ' + node.UsageText, o, node);
        }
    }

    internal void Add(Command.Command command) => _helpEntries.Add(command);
}
