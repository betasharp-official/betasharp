namespace BetaSharp.Server.Command;

public interface ICommandOutput
{
    void SendMessage(string message);
    string Name { get; }
    byte PermissionLevel { get; }
}
