using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BetaSharp.Server;

internal sealed class ServerMonitoringHost : IDisposable
{
    private readonly BetaSharpServer _server;
    private readonly HttpListener _listener = new();
    private readonly ILogger<ServerMonitoringHost> _logger = Log.Instance.For<ServerMonitoringHost>();
    private readonly CancellationTokenSource _shutdown = new();

    private Task? _serverTask;
    private bool _disposed;

    public ServerMonitoringHost(BetaSharpServer server, string host, int port)
    {
        _server = server;
        Prefix = $"http://{NormalizeHost(host)}:{port}/";
        _listener.Prefixes.Add(Prefix);
    }

    public string Prefix { get; }

    public void Start()
    {
        _listener.Start();
        _serverTask = Task.Run(ListenLoopAsync);
    }

    private async Task ListenLoopAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            HttpListenerContext? context = null;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (_shutdown.IsCancellationRequested || !_listener.IsListening)
            {
                break;
            }
            catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monitoring listener failed while waiting for a request.");
            }

            if (context != null)
            {
                _ = Task.Run(() => HandleRequestAsync(context));
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            string path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;
            switch (path)
            {
                case "":
                case "/healthz":
                    await WriteResponseAsync(context.Response, "text/plain; charset=utf-8", "ok\n");
                    break;

                case "/stats":
                    await WriteResponseAsync(context.Response, "text/plain; charset=utf-8", _server.GetDiagnosticsSummary());
                    break;

                case "/stats.json":
                    await WriteResponseAsync(context.Response, "application/json; charset=utf-8", _server.GetDiagnosticsJson());
                    break;

                case "/metrics":
                    await WriteResponseAsync(context.Response, "text/plain; version=0.0.4; charset=utf-8", _server.GetMetricsText());
                    break;

                case "/profiler":
                    await WriteResponseAsync(context.Response, "text/plain; charset=utf-8", _server.GetProfilerText());
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    await WriteResponseAsync(context.Response, "text/plain; charset=utf-8", "not found\n");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monitoring request handling failed.");
        }
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string contentType, string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await using Stream output = response.OutputStream;
        await output.WriteAsync(bytes);
    }

    private static string NormalizeHost(string host)
    {
        if (host == "0.0.0.0" || host == "*" || host == "+")
        {
            return "+";
        }

        return host;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shutdown.Cancel();

        try
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
        }
        catch (HttpListenerException)
        {
        }

        _listener.Close();

        try
        {
            _serverTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        _shutdown.Dispose();
    }
}
