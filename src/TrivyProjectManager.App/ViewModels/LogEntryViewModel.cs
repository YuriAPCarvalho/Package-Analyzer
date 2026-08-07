using System.Text.RegularExpressions;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.App.ViewModels;

public enum LogDisplayLevel
{
    Info,
    Warning,
    Error
}

public sealed class LogEntryViewModel
{
    private static readonly Regex ErrorLevelPattern = new(@"\b(?:ERR(?:OR)?|FATAL)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WarningLevelPattern = new(@"\bWARN(?:ING)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InformationLevelPattern = new(@"\bINFO(?:RMATION)?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public LogEntryViewModel(ProcessLogLine line)
    {
        At = line.At;
        Stream = line.Stream;
        Message = line.Message;
        Level = Classify(line.Stream, line.Message);
    }

    public DateTimeOffset At { get; }
    public string Stream { get; }
    public string Message { get; }
    public LogDisplayLevel Level { get; }
    public string FormattedText => $"{At:HH:mm:ss} {Stream}: {Message}";
    public bool IsInfo => Level == LogDisplayLevel.Info;
    public bool IsWarning => Level == LogDisplayLevel.Warning;
    public bool IsError => Level == LogDisplayLevel.Error;

    public static LogDisplayLevel Classify(string? stream, string? message)
    {
        var content = message ?? string.Empty;
        if (ErrorLevelPattern.IsMatch(content))
        {
            return LogDisplayLevel.Error;
        }

        if (WarningLevelPattern.IsMatch(content))
        {
            return LogDisplayLevel.Warning;
        }

        if (InformationLevelPattern.IsMatch(content))
        {
            return LogDisplayLevel.Info;
        }

        if (string.Equals(stream, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return LogDisplayLevel.Warning;
        }

        return string.Equals(stream, "stderr", StringComparison.OrdinalIgnoreCase)
            ? LogDisplayLevel.Error
            : LogDisplayLevel.Info;
    }
}
