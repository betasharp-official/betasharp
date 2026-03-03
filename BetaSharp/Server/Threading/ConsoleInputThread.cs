namespace BetaSharp.Server.Threading;

public class ConsoleInputThread : java.lang.Thread
{
    private readonly BetaSharpServer mcServer;

    public ConsoleInputThread(BetaSharpServer server) : base("Server console handler")
    {
        mcServer = server;
    }

    public override void run()
    {
        while (!mcServer.stopped && mcServer.running)
        {
            string? line = Console.ReadLine();
            if (line != null)
            {
                mcServer.queueCommands(line, mcServer);
            }
        }
    }
}
