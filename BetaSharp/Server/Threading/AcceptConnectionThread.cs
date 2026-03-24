using System.Net;
using System.Net.Sockets;
using BetaSharp.Server.Network;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Threading;

internal class AcceptConnectionThread
{
    private readonly ILogger<AcceptConnectionThread> _logger = Log.Instance.For<AcceptConnectionThread>();
    private readonly ConnectionListener _listener;
    private readonly string _name;
    private const int MAX_CACHE_SIZE = 1000;

    public AcceptConnectionThread(ConnectionListener listener, string name)
    {
        _listener = listener;
        _name = name;
    }

    public void Run()
    {
        Thread t = new(() =>
        {

            Dictionary<IPAddress, long> map = [];

            while (_listener.open)
            {
                try
                {
                    Socket socket = _listener.Socket.Accept();

                    socket.NoDelay = true;

                    IPAddress? address = ((IPEndPoint?)socket.RemoteEndPoint)?.Address;

                    ArgumentNullException.ThrowIfNull(address);

                    if (map.TryGetValue(address, out long id) && !IPAddress.Loopback.Equals(address) && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
     - id < 5000L)
                    {
                        map[address] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    ;
                        socket.Close();
                    }
                    else
                    {
                        map[address] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        if (map.Count > MAX_CACHE_SIZE)
                        {
                            IPAddress? oldest = null;
                            long oldestTime = long.MaxValue;
                            foreach (KeyValuePair<IPAddress, long> kv in map)
                            {
                                if (kv.Value < oldestTime)
                                {
                                    oldestTime = kv.Value;
                                    oldest = kv.Key;
                                }
                            }
                            if (oldest != null) map.Remove(oldest);
                        }

                        ServerLoginNetworkHandler handler = new(_listener.server, socket, "Connection # " + _listener.GetNextConnectionCounter());
                        _listener.AddPendingConnection(handler);
                    }
                }
                catch (SocketException ex)
                {
                    _logger.LogError(ex, "Failed to accept connection");
                }
            }
        })
        {
            Name = _name
        };
        t.Start();
    }
}
