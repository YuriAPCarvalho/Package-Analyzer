using TrivyProjectManager.Domain.Entities;

namespace TrivyProjectManager.Application.Services;

public static class CommandValidationService
{
    private static readonly char[] UnsafeExecutableCharacters = ['&', '|', ';', '<', '>', '`'];

    public static IReadOnlyList<string> Validate(ProjectCommand command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.Command))
        {
            errors.Add("Executable is required.");
        }

        var rootedExecutable = Path.IsPathRooted(command.Command) && File.Exists(command.Command);
        if (command.Command.IndexOfAny(UnsafeExecutableCharacters) >= 0 || command.Command.Contains(' ') && !rootedExecutable)
        {
            errors.Add("Executable must be stored separately from arguments.");
        }

        if (!string.IsNullOrWhiteSpace(command.WorkingDirectory) && !Directory.Exists(command.WorkingDirectory))
        {
            errors.Add("Working directory does not exist.");
        }

        return errors;
    }
}
