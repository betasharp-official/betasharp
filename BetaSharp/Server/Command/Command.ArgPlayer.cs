using BetaSharp.Entities;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using StringReader = Brigadier.NET.StringReader;

namespace BetaSharp.Server.Command;

public abstract partial class Command
{
    private class ArgPlayer : IArgumentType<ServerPlayerEntity>
    {
        public ServerPlayerEntity Parse(IStringReader reader) => throw new Exception("Unsupported invocation.");

        public ServerPlayerEntity Parse<T>(StringReader reader, T source)
        {
            Entity[] e = ArgTargets.Parse(reader, source, true, 1);
            if (e.Length < 1) throw ArgTargets.PlayerNotFound.CreateWithContext(reader);
            var p = e[0] as ServerPlayerEntity;
            if (p == null) throw ArgTargets.PlayerNotFound.CreateWithContext(reader);
            return p;
        }

        public Task<Suggestions> ListSuggestions<T>(
            CommandContext<T> context,
            SuggestionsBuilder builder) =>
            context is not CommandContext<CommandSource> c ? Suggestions.Empty() : ArgTargets.ListSuggestionsAsync(c, builder);

        public IEnumerable<string> Examples => ArgTargets.StaticExamples;
    }
}
