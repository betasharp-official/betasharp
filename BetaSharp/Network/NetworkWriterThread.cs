using System.IO;

namespace BetaSharp.Network;

internal class NetworkWriterThread
{
    public readonly Connection netManager;

    public NetworkWriterThread(Connection var1)
    {
        netManager = var1;
    }


    public void Run()
    {
        object var1 = Connection.LOCK;
        lock (var1)
        {
            ++Connection.WRITE_THREAD_COUNTER;
        }

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
                catch (IOException ex)
                {
                    if (!Connection.isDisconnected(netManager))
                    {
                        Connection.Disconnect(netManager, ex);
                    }
                }
            }
            finally
            {
                if (var13)
                {
                    object var5 = Connection.LOCK;
                    lock (var5)
                    {
                        --Connection.WRITE_THREAD_COUNTER;
                    }
                }
            }
        }

        var1 = Connection.LOCK;
        lock (var1)
        {
            --Connection.WRITE_THREAD_COUNTER;
        }
    }
}
