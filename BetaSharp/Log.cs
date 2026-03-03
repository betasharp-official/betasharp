using System.Text;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public sealed class Log
{
    public static Log Instance { get; } = new();

    private readonly ILoggerFactory _factory;

    private bool _initialized;
    private string? _directory;

    private Log()
    {
        _factory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddSimpleConsole(options => options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "));
    }

    public void Initialize(string directory)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _directory = Path.Combine(directory, "logs");

        Directory.CreateDirectory(_directory);

        // $"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log"

        string path = Path.Combine(
            _directory,
            $"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log");

        _factory.AddProvider(new FileLoggerProvider(path));

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => UnhandledException((Exception)eventArgs.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, eventArgs) => UnhandledException(eventArgs.Exception);
    }

    public ILogger<T> For<T>()
    {
        return _factory.CreateLogger<T>();
    }

    public ILogger For(string name)
    {
        return _factory.CreateLogger(name);
    }

    private void UnhandledException(Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_directory);

        string parent = Path.Combine(
            _directory,
            "crashes");

        Directory.CreateDirectory(parent);

        string path = Path.Combine(parent, $"{DateTime.Now:yyyy-MM-dd_HH.mm.ss}.log");

        File.WriteAllText(path, exception.ToString());
    }
}

internal sealed class FileLoggerProvider(string path) : ILoggerProvider
{
    private readonly FileStream _stream = File.OpenWrite(path);

    private sealed class FileLogger(string category, FileStream stream) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {category}: {message}";

            stream.Write(Encoding.UTF8.GetBytes(line));

            if (exception is not null)
            {
                stream.Write(Encoding.UTF8.GetBytes($"{Environment.NewLine}{exception}"));
            }

            stream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
            stream.Flush();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            throw new InvalidOperationException();
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _stream);
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
