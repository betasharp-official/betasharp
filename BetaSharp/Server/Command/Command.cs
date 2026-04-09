using BetaSharp.Entities;
using BetaSharp.Util.Maths;
using Brigadier.NET;
using Brigadier.NET.Builder;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Command;

public abstract partial class Command
{
    private static readonly ILogger s_logger = Log.Instance.For(nameof(Command));

    public abstract string Usage { get; }
    public abstract string Description { get; }
    public abstract string[] Names { get; }

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
    public virtual byte PermissionLevel => 2;

    /// <summary>
    /// When true, the command can only be executed on external servers (muliplayer).
    /// </summary>
    public virtual bool DisallowInternalServer => false;

    public virtual LiteralArgumentBuilder<CommandSource> Register(LiteralArgumentBuilder<CommandSource> argBuilder) => Literal("none");

    protected static LiteralArgumentBuilder<CommandSource> Literal(string literal) => LiteralArgumentBuilder<CommandSource>.LiteralArgument(literal);
    protected static RequiredArgumentBuilder<CommandSource, Entity> ArgumentTarget(string name) => RequiredArgumentBuilder<CommandSource, Entity>.RequiredArgument(name, new ArgTarget());
    protected static RequiredArgumentBuilder<CommandSource, Entity[]> ArgumentTargets(string name) => RequiredArgumentBuilder<CommandSource, Entity[]>.RequiredArgument(name, new ArgTargets());
    protected static RequiredArgumentBuilder<CommandSource, ServerPlayerEntity> ArgumentPlayer(string name) => RequiredArgumentBuilder<CommandSource, ServerPlayerEntity>.RequiredArgument(name, new ArgPlayer());
    protected static RequiredArgumentBuilder<CommandSource, T> ArgumentEnum<T>(string name) where T : struct, Enum => RequiredArgumentBuilder<CommandSource, T>.RequiredArgument(name, Arguments.Enum<T>());
    protected static RequiredArgumentBuilder<CommandSource, Vec3D> ArgumentPos(string name) => RequiredArgumentBuilder<CommandSource, Vec3D>.RequiredArgument(name, new ArgPosition());
    protected static RequiredArgumentBuilder<CommandSource, bool> ArgumentBool(string name) => RequiredArgumentBuilder<CommandSource, bool>.RequiredArgument(name, Arguments.Bool());
    protected static RequiredArgumentBuilder<CommandSource, int> ArgumentInt(string name) => RequiredArgumentBuilder<CommandSource, int>.RequiredArgument(name, Arguments.Integer());
    protected static RequiredArgumentBuilder<CommandSource, string> ArgumentGreedy(string name) => RequiredArgumentBuilder<CommandSource, string>.RequiredArgument(name, Arguments.GreedyString());
    protected static RequiredArgumentBuilder<CommandSource, string> ArgumentString(string name) => RequiredArgumentBuilder<CommandSource, string>.RequiredArgument(name, Arguments.Word());


    public class CommandSource(ICommandHandler handler, string senderName, ICommandOutput output)
    {
        public ICommandHandler Handler { get; } = handler;
        public BetaSharpServer Server => Handler.Server;
        public string SenderName { get; } = senderName;
        public static string[] Args => [];
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
