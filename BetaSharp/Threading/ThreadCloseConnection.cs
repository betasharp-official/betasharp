using BetaSharp.Network;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Threading;

internal class ThreadCloseConnection
{

    private readonly ILogger<ThreadCloseConnection> _logger = Log.Instance.For<ThreadCloseConnection>();
    public readonly Connection Connection;

    public ThreadCloseConnection(Connection connection)
    {
        Connection = connection;
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000);

                if (Connection.isOpen(Connection))
                {
                    Connection.getWriter(Connection).Interrupt();
                    Connection.Disconnect(Connection, new Exception("disconnect.closed"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing connection");
            }
        });
    }
}
