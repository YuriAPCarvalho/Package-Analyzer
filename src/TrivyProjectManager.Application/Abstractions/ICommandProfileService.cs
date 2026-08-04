using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.Abstractions;

public interface ICommandProfileService
{
    IReadOnlyList<ProjectCommand> CreateDefaultCommands(ProjectTechnology technology, PackageManagerType packageManager, string projectPath);
}
