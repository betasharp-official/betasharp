using Brigadier.NET.Context;

namespace BetaSharp.Server.Command;

public static class CommandContextExtension
{
    public static T? GetArgumentOrDefault<T>(this CommandContext<Command.CommandSource> context, string argumentName, T? defaultValue = default)
    {
        try
        {
            return context.GetArgument<T>(argumentName);
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }
}
