using BetaSharp.Client.Rendering;

namespace BetaSharp.Client;

internal readonly record struct ClientStartupOptions(
    string PlayerName,
    string SessionToken,
    RendererBackendKind RendererBackend
);

internal static class ClientStartupArgumentParser
{
    public static ClientStartupOptions Parse(string[] args)
    {
        RendererBackendKind rendererBackend = RendererBackendKind.OpenGL;
        List<string> positional = [];

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.StartsWith("--renderer=", StringComparison.OrdinalIgnoreCase))
            {
                rendererBackend = ParseRendererBackendOrThrow(arg["--renderer=".Length..]);
                continue;
            }

            if (string.Equals(arg, "--renderer", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("Missing value for --renderer. Supported values: opengl, vulkan.");
                }

                rendererBackend = ParseRendererBackendOrThrow(args[++i]);
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unknown option '{arg}'. Supported option: --renderer=<opengl|vulkan>.");
            }

            positional.Add(arg);
        }

        (string Name, string Session) player = positional.Count switch
        {
            0 => ($"Player{Random.Shared.Next()}", "-"),
            1 => (positional[0], "-"),
            _ => (positional[0], positional[1]),
        };

        return new(player.Name, player.Session, rendererBackend);
    }

    private static RendererBackendKind ParseRendererBackendOrThrow(string value)
    {
        if (!RendererBackendFactory.TryParse(value, out RendererBackendKind backend))
        {
            throw new ArgumentException(
                $"Unsupported renderer backend '{value}'. Supported values: opengl, vulkan.");
        }

        return backend;
    }
}
