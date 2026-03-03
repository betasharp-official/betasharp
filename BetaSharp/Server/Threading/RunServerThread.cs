namespace BetaSharp.Server.Threading;

public class RunServerThread : java.lang.Thread
{
    private readonly BetaSharpServer mcServer;

    public RunServerThread(BetaSharpServer server, string name) : base(name)
    {
        mcServer = server;
    }

    public override void run()
    {
        mcServer.run();
    }
}
