using TrivyProjectManager.Domain.Entities;
using TrivyProjectManager.Domain.Enums;

namespace TrivyProjectManager.Application.DTOs;

public sealed record CommandProfile(ProjectTechnology Technology, PackageManagerType PackageManager, IReadOnlyList<ProjectCommand> Commands);
