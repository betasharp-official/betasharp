namespace BetaSharp.Network;

internal class NetworkWriterThread : java.lang.Thread
{
    public readonly Connection netManager;

    public NetworkWriterThread(Connection var1, string var2) : base(var2)
    {
        netManager = var1;
    }


    public override void run()
    {
        while (true)
        {
            bool var13 = false;

            try
            {
                var13 = true;
                if (!Connection.isOpen(netManager))
                {
                    var13 = false;
                    break;
                }

                while (Connection.writePacket(netManager))
                {
                }

                netManager.waitForSignal(10);

                try
                {
                    Connection.getOutputStream(netManager)?.Flush();
                }
                catch (java.io.IOException ex)
                {
                    if (!Connection.isDisconnected(netManager))
                    {
                        Connection.disconnect(this.netManager, ex);
                        ex.printStackTrace();
                    }
                }
            }
            finally
            {
            }
        }
    }
}
