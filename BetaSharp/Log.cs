using System.Text;
using Microsoft.Extensions.Logging;

namespace BetaSharp;

public readonly record struct LogEntry(DateTime Timestamp, LogLevel Level, string Category, string Message, Exception? Exception);

public sealed class Log
{
    public static Log Instance { get; } = new();

    private readonly ILoggerFactory _factory;
    private readonly MemoryLoggerProvider _memoryProvider = new();

    private bool _initialized;
    private string? _directory;

    private Log()
    {
        _factory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddProvider(_memoryProvider)
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

    public LogEntry[] GetRecentEntries() => _memoryProvider.GetEntries();

    public void ClearLog() => _memoryProvider.Clear();

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

internal sealed class MemoryLoggerProvider : ILoggerProvider
{
    private const int Capacity = 500;
    private readonly Queue<LogEntry> _entries = new(Capacity);
    private readonly Lock _lock = new();

    private sealed class MemoryLogger(string category, MemoryLoggerProvider provider) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, exception);
            provider.Add(new LogEntry(DateTime.Now, logLevel, category, message, exception));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    public void Add(LogEntry entry)
    {
        lock (_lock)
        {
            if (_entries.Count >= Capacity)
                _entries.Dequeue();
            _entries.Enqueue(entry);
        }
    }

    public LogEntry[] GetEntries()
    {
        lock (_lock)
            return [.. _entries];
    }

    public void Clear()
    {
        lock (_lock)
            _entries.Clear();
    }

    public ILogger CreateLogger(string categoryName) => new MemoryLogger(categoryName, this);

    public void Dispose() { }
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
