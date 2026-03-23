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
        AdminCommands.LogCommand(c.Server, c.SenderName, "Forcing save..");
        c.Server.playerManager?.savePlayers();

        for (int i = 0; i < c.Server.worlds.Length; i++)
        {
            c.Server.worlds[i].saveWithLoadingDisplay(true, null);
        }

        AdminCommands.LogCommand(c.Server, c.SenderName, "Save complete.");
    }
}
