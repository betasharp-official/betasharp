using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class SaveOnCommand : ICommand
{
    public string Usage => "save-on";
    public string Description => "Enables level saving";
    public string[] Names => ["save-on"];
    public byte PermissionLevel => 4;

    public void Execute(ICommand.CommandContext c)
    {
        c.LogOp( "Enabling level saving..");
        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].savingDisabled = false;
        }
    }
}

public class SaveOffCommand : ICommand
{
    public string Usage => "save-off";
    public string Description => "Disables level saving";
    public string[] Names => ["save-off"];
    public byte PermissionLevel => 4;

    public void Execute(ICommand.CommandContext c)
    {
        c.LogOp( "Disabling level saving..");
        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].savingDisabled = true;
        }
    }
}
