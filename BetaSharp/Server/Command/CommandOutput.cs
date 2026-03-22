namespace BetaSharp.Server.Command;

public interface ICommandOutput
{
    void SendMessage(string message);
    string GetName();
    byte GetPermissionLevel();
}
