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
            if (source is not CommandSource c)
            {
                throw new Exception("Unsupported source.");
            }

            if (reader.Peek() == '@' && (reader.RemainingLength == 2 || reader.Peek(2) == ' '))
            {
                reader.Cursor++;
                char ch = reader.Next();
                if (ch == 'p' || ch == 'P')
                {
                    return c.Server.playerManager.getPlayer(c.SenderName) ?? throw new Exception("Player not found.");
                }

                throw new Exception("@" + ch + " is invalid at this point.");
            }

            string input = reader.ReadUnquotedString();
            return c.Server.playerManager.getPlayer(input) ?? throw new Exception("Player not found.");
        }

        public Task<Suggestions> ListSuggestions<T>(
            CommandContext<T> context,
            SuggestionsBuilder builder) =>
            context is not CommandContext<CommandSource> c ? Suggestions.Empty() : ListSuggestionsAsync(c, builder);

        public IEnumerable<string> Examples => ["@p", "player"];

        private async Task<Suggestions> ListSuggestionsAsync(
            CommandContext<CommandSource> context,
            SuggestionsBuilder builder)
        {
            string s = builder.RemainingLowerCase;

            if (s.Length > 0 && s[0] == '@')
            {
                if (s.Length == 1 || (s.Length == 2 && s[1] == 'p'))
                {
                    builder.Suggest("@p");
                }
            }
            else
            {
                foreach (ServerPlayerEntity p in context.Source.Server.playerManager.players)
                {
                    if (p.name.StartsWith(s, StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Suggest(p.name);
                    }
                }
            }

            return await builder.BuildAsync();
        }
    }
}
