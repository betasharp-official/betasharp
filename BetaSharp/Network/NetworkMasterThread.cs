using Microsoft.Extensions.Logging;

namespace BetaSharp.Network;

internal class NetworkMasterThread
{
    public readonly Connection netManager;

    public NetworkMasterThread(Connection var1)
    {
        netManager = var1;
    }

    public void Start()
    {
        var thread = new Thread(Run) { Name = "NetworkMaster", IsBackground = true };
        thread.Start();
    }

    private void Run()
    {
        try
        {
            Thread.Sleep(5000);
            Thread? reader = Connection.getReader(netManager);
            if (reader?.IsAlive == true)
            {
                try { reader.Interrupt(); } catch (Exception) { }
            }

            Thread? writer = Connection.getWriter(netManager);
            if (writer?.IsAlive == true)
            {
                try { writer.Interrupt(); } catch (Exception) { }
            }
        }
        catch (ThreadInterruptedException ex)
        {
            Log.Instance.For<NetworkMasterThread>().LogError(ex, ex.Message);
        }
    }
}
