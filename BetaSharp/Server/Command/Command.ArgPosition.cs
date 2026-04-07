using BetaSharp.Util.Maths;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Exceptions;
using StringReader = Brigadier.NET.StringReader;

namespace BetaSharp.Server.Command;

public abstract partial class Command
{
    private class ArgPosition : IArgumentType<Vec3D>
    {
        public Vec3D Parse(IStringReader reader) => new(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

        public Vec3D Parse<T>(StringReader reader, T source)
        {
            int curs = reader.Cursor;

            try
            {
                if (source is not CommandSource c)
                {
                    return Parse(reader);
                }

                Vec3D pos = new();
                Vec3D? player = null;

                if (reader.Peek() == '~')
                {
                    player ??= SenderPosition(c);
                    reader.Cursor++;
                    pos.x = player.Value.x;
                    if (reader.Peek() != ' ')
                    {
                        pos.x += reader.ReadDouble();
                    }
                }
                else
                {
                    pos.x = reader.ReadDouble();
                }

                reader.Cursor++;

                if (reader.Peek() == '~')
                {
                    player ??= SenderPosition(c);
                    reader.Cursor++;
                    pos.y = player.Value.y;
                    if (reader.Peek() != ' ')
                    {
                        pos.y += reader.ReadDouble();
                    }
                }
                else
                {
                    pos.y = reader.ReadDouble();
                }

                reader.Cursor++;

                if (reader.Peek() == '~')
                {
                    player ??= SenderPosition(c);
                    reader.Cursor++;
                    pos.z = player.Value.z;
                    if (reader.RemainingLength != 0 && reader.Peek() != ' ')
                    {
                        pos.z += reader.ReadDouble();
                    }
                }
                else
                {
                    pos.z = reader.ReadDouble();
                }

                return pos;
            }
            catch (Exception e)
            {
                reader.Cursor = curs;
                throw CommandSyntaxException.BuiltInExceptions.DispatcherUnknownArgument().Create();
            }
        }

        public IEnumerable<string> Examples => ["~ ~ ~", "19 -5.2, 109", "~12 ~-4 ~8.2"];

        private Vec3D SenderPosition(CommandSource s) => s.Server.playerManager.getPlayer(s.SenderName)?.GetPosition() ?? throw new Exception("Player not found.");
    }
}
