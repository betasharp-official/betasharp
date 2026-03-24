namespace BetaSharp.Server.Threading;

public class ConsoleInputThread(BetaSharpServer server)
{
    public void Run()
    {
        Thread t = new(() =>
        {

            while (!server.stopped && server.running)
            {
                string? line = Console.ReadLine();
                if (line != null)
                {
                    server.QueueCommands(line, server);
                }
            }
        })
        {
            Name = "Server console handler",
            IsBackground = true
        };

        t.Start();
    }
}
