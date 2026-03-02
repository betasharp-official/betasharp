namespace BetaSharp.Network;

class NetworkReaderThread
{
    public readonly Connection netManager;

    public NetworkReaderThread(Connection var1)
    {
        this.netManager = var1;
    }


    public void Run()
    {
        object var1 = Connection.LOCK;
        lock (var1)
        {
            ++Connection.READ_THREAD_COUNTER;
        }

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
                if (var12)
                {
                    object var5 = Connection.LOCK;
                    lock (var5)
                    {
                        --Connection.READ_THREAD_COUNTER;
                    }
                }
            }
        }

        var1 = Connection.LOCK;
        lock (var1)
        {
            --Connection.READ_THREAD_COUNTER;
        }
    }
}