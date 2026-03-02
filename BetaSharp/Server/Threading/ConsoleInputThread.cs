namespace BetaSharp.Server.Threading;

public class ConsoleInputThread
{
    private readonly MinecraftServer _mcServer;
    private readonly Thread _thread;

    public ConsoleInputThread(MinecraftServer server)
    {
        _mcServer = server;
        _thread = new Thread(Run)
        {
            Name = "Server console handler",
            IsBackground = true
        };
    }

    public void Start() => _thread.Start();

    private void Run()
    {
        while (!_mcServer.Stopped && _mcServer.Running)
        {
            string? line = Console.ReadLine();
            if (line != null)
            {
                _mcServer.QueueCommands(line, _mcServer);
            }
        }
    }
}
