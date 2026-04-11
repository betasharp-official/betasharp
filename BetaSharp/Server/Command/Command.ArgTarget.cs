using BetaSharp.Entities;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using StringReader = Brigadier.NET.StringReader;

namespace BetaSharp.Server.Command;

public abstract partial class Command
{
    private class ArgTarget : IArgumentType<Entity>
    {
        public Entity Parse(IStringReader reader) => throw new Exception("Unsupported invocation.");

        public Entity Parse<T>(StringReader reader, T source) => ParseStatic(reader, source);

        public static Entity ParseStatic<T>(StringReader reader, T source)
        {
            Entity[] e = ArgTargets.Parse(reader, source, false, 1);
            if (e.Length < 1) throw ArgTargets.TargetNotFound.CreateWithContext(reader);
            return e[0];
        }

        public Task<Suggestions> ListSuggestions<T>(
            CommandContext<T> context,
            SuggestionsBuilder builder) =>
            context is not CommandContext<CommandSource> c ? Suggestions.Empty() : ArgTargets.ListSuggestionsAsync(c, builder);

        public IEnumerable<string> Examples => ArgTargets.StaticExamples;
    }
}
