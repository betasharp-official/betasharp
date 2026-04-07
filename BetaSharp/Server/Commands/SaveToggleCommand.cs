using Brigadier.NET.Builder;
using Brigadier.NET.Context;

namespace BetaSharp.Server.Commands;

public class SaveOnCommand : Command.Command
{
    public override string Usage => "save-on";
    public override string Description => "Enables level saving";
    public override string[] Names => ["save-on"];
    public override byte PermissionLevel => 4;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        CommandSource c = context.Source;
        c.LogOp("Enabling level saving..");
        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].savingDisabled = false;
        }

        return 1;
    }
}

public class SaveOffCommand : Command.Command
{
    public override string Usage => "save-off";
    public override string Description => "Disables level saving";
    public override string[] Names => ["save-off"];
    public override byte PermissionLevel => 4;

    public override LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) =>
        argBuilder.Executes(Execute);

    private static int Execute(CommandContext<CommandSource> context)
    {
        CommandSource c = context.Source;
        c.LogOp("Disabling level saving..");
        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].savingDisabled = true;
        }

        return 1;
    }
}
