using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Command;

public interface ICommand
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(ICommand));

    public string Usage { get; }
    public string Description { get; }
    public string[] Names { get; }

    /// <summary>
    /// Required permission for command execution.
    /// </summary>
    /// <remarks>
    /// Currently, levels of permission are not implemented, but we allow it in command for future use.
    /// Mojang uses the following naming:<br/>
    /// 0 - All<br/>
    /// 1 - Moderator<br/>
    /// 2 - Admin<br/>
    /// 3 - Gamemaster<br/>
    /// 4 - Owner
    /// </remarks>
    public byte PermissionLevel => 2;

    /// <summary>
    /// When true, the command can only be executed on external servers (muliplayer).
    /// </summary>
    public bool DisallowInternalServer => false;

    /// <summary>
    /// Run command.
    /// </summary>
    /// <param name="context">command context</param>
    public void Execute(CommandContext context);

    public class CommandContext(BetaSharpServer server, string senderName, string[] args, ICommandOutput output)
    {
        public BetaSharpServer Server { get; } = server;
        public string SenderName { get; } = senderName;
        public string[] Args { get; } = args;
        public ICommandOutput Output { get; } = output;

        /// <summary>
        /// Log to all operators and console.
        /// </summary>
        public void LogOp(string message)
        {
            string logMessage = SenderName + ": " + message;
            Server.playerManager.BroadcastOp("§7(" + logMessage + ")");
            s_logger.LogInformation(logMessage);
        }
    }
}
