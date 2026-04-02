using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BetaSharp.Launcher.Features;

internal sealed class ProcessService(MinecraftService minecraftService)
{
    public async Task<Process> StartAsync(Kind kind, params string[] args)
    {
        string suffix = kind.ToString();
        string directory = Path.Combine(AppContext.BaseDirectory, suffix);

        bool redirect = kind is Kind.Server;

        await minecraftService.DownloadAsync(directory);

        var info = new ProcessStartInfo
        {
            Arguments = string.Join(" ", args),
            CreateNoWindow = true,
            FileName = Path.Combine(directory, $"{nameof(BetaSharp)}.{suffix}"),
            RedirectStandardInput = redirect,
            RedirectStandardOutput = redirect,
            WorkingDirectory = directory
        };

        var process = Process.Start(info);

        ArgumentNullException.ThrowIfNull(process);

        return process;
    }
}

internal enum Kind
{
    Client,
    Server
}
