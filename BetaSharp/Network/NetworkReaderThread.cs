namespace BetaSharp.Network;

class NetworkReaderThread : java.lang.Thread
{
    public readonly Connection netManager;

    public NetworkReaderThread(Connection var1, string var2) : base(var2)
    {
        this.netManager = var1;
    }


    public override void run()
    {
        while (true)
        {
            bool var12 = false;

            try
            {
                var12 = true;
                if (!Connection.isOpen(this.netManager))
                {
                    var12 = false;
                    break;
                }

                if (Connection.isClosed(this.netManager))
                {
                    var12 = false;
                    break;
                }

                while (Connection.readPacket(this.netManager))
                {
                }

                netManager.waitForSignal(10);
            }
            finally
            {
            }
        }
    }
}
