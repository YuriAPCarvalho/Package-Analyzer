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

        if (command.Command.IndexOfAny(UnsafeExecutableCharacters) >= 0 || command.Command.Contains(' '))
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
