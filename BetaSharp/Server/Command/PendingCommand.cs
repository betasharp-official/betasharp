namespace BetaSharp.Server.Command;

internal class PendingCommand(string contents, ICommandOutput output)
{
    public string CommandAndArgs { get; } = contents;
    public ICommandOutput Output { get; } = output;
}
