using Microsoft.Extensions.Logging;
using TrivyProjectManager.Application.Services;

namespace TrivyProjectManager.App.Services;

public sealed class LocalFileLoggerProvider : ILoggerProvider
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrivyProjectManager", "logs", "app.log");
    private readonly SecretMaskingService _masking = new();

    public ILogger CreateLogger(string categoryName) => new LocalFileLogger(_path, categoryName, _masking);

    public void Dispose()
    {
    }

    private sealed class LocalFileLogger(string path, string category, SecretMaskingService masking) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            Rotate(path);
            var message = masking.Mask(formatter(state, exception));
            if (exception is not null)
            {
                message += Environment.NewLine + masking.Mask(exception.ToString());
            }

            File.AppendAllText(path, $"[{DateTimeOffset.UtcNow:O}] {logLevel} {category}: {message}{Environment.NewLine}");
        }

        private static void Rotate(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length < 1_000_000)
            {
                return;
            }

            var archive = Path.ChangeExtension(path, ".1.log");
            if (File.Exists(archive))
            {
                File.Delete(archive);
            }

            File.Move(path, archive);
        }
    }
}
