using BetaSharp.Server.Command;

namespace BetaSharp.Server.Commands;

public class SaveAllCommand : ICommand
{
    public string Usage => "save-all";
    public string Description => "Forces a world save";
    public string[] Names => ["save-all"];
    public byte PermissionLevel => 4;

    public void Execute(ICommand.CommandContext c)
    {
        c.LogOp( "Forcing save..");
        c.Server.playerManager?.savePlayers();

        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].SaveWithLoadingDisplay(true, null);
        }

        c.LogOp( "Save complete.");
    }
}
