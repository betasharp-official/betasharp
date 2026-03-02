using System.Net;
using System.Net.Sockets;
using BetaSharp.Server.Network;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server.Threading;

internal class AcceptConnectionThread
{
    private readonly ILogger<AcceptConnectionThread> _logger = Log.Instance.For<AcceptConnectionThread>();
    private readonly ConnectionListener _listener;
    private readonly Thread _thread;

    public AcceptConnectionThread(ConnectionListener listener, string name)
    {
        _listener = listener;
        _thread = new Thread(Run)
        {
            Name = name,
            IsBackground = true
        };
    }

    public void Start() => _thread.Start();

    private void Run()
    {
        Dictionary<IPAddress, long> map = [];

        while (_listener.Open)
        {
            try
            {
                Socket socket = _listener.Socket.Accept();

                socket.NoDelay = true;

                var address = ((IPEndPoint?) socket.RemoteEndPoint)?.Address;

                ArgumentNullException.ThrowIfNull(address);

                if (map.TryGetValue(address, out long id) && ! IPAddress.Loopback.Equals(address) && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
 - id < 5000L)
                {
                    map[address] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
                    socket.Close();
                }
                else
                {
                    map[address] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
;
                    ServerLoginNetworkHandler handler = new(_listener.Server, socket, "Connection # " + _listener.ConnectionCounter);
                    _listener.AddPendingConnection(handler);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Failed to accept connection");
            }
        }
    }
}
