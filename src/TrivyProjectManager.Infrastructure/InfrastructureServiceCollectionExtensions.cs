using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TrivyProjectManager.Application.Abstractions;
using TrivyProjectManager.Application.Services;
using TrivyProjectManager.Infrastructure.Data;
using TrivyProjectManager.Infrastructure.Services;

namespace TrivyProjectManager.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTrivyProjectManager(this IServiceCollection services)
    {
        services.AddSingleton<IStoragePathService, StoragePathService>();
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<ITrivyReleaseClient, GitHubTrivyReleaseClient>();
        services.AddSingleton<ITrivyBootstrapService, TrivyBootstrapService>();
        services.AddSingleton<IProjectDetectionService, ProjectDetectionService>();
        services.AddSingleton<ICommandProfileService, CommandProfileService>();
        services.AddSingleton<ISecretMaskingService, SecretMaskingService>();
        services.AddSingleton<IFindingDeduplicationService, FindingDeduplicationService>();
        services.AddSingleton<IScanComparisonService, ScanComparisonService>();
        services.AddSingleton<FixedVersionRecommendationService>();
        services.AddSingleton<ReferenceDisplayService>();
        services.AddSingleton<FindingTextService>();
        services.AddSingleton<UpdateCommandService>();
        services.AddSingleton<TrivyReportRedactionService>();
        services.AddScoped<IDependencyAnalysisService, DependencyAnalysisService>();
        services.AddScoped<IVulnerabilityEnrichmentService, CachedVulnerabilityEnrichmentService>();
        services.AddScoped<ITrivyReportParser, TrivyReportParser>();
        services.AddScoped<ITrivyService, TrivyService>();
        services.AddScoped<IRetentionService, RetentionService>();
        services.AddScoped<SecurityExceptionApplicator>();
        services.AddScoped<IScanOrchestrator, ScanOrchestrator>();
        services.AddScoped<IExternalLinkService, ExternalLinkService>();

        services.AddDbContext<TrivyProjectManagerDbContext>((provider, options) =>
        {
            var paths = provider.GetRequiredService<IStoragePathService>();
            var dbPath = paths.GetDatabasePath();
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            options
                .UseSqlite($"Data Source={dbPath}")
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        return services;
    }
}
