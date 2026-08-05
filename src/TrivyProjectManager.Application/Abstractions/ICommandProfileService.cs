using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Application.DTOs;

namespace TrivyProjectManager.Application.Abstractions;

public interface ICommandProfileService
{
    IReadOnlyList<ProjectCommand> CreateAutomaticCommands(ProjectDetectionResult detection);
}
