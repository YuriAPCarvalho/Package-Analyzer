using Microsoft.EntityFrameworkCore;
using TrivyProjectManager.Domain.Entities;
using SecurityExceptionEntity = TrivyProjectManager.Domain.Entities.SecurityException;

namespace TrivyProjectManager.Infrastructure.Data;

public sealed class TrivyProjectManagerDbContext(DbContextOptions<TrivyProjectManagerDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectCommand> ProjectCommands => Set<ProjectCommand>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<FindingReference> FindingReferences => Set<FindingReference>();
    public DbSet<FindingOccurrence> FindingOccurrences => Set<FindingOccurrence>();
    public DbSet<SecurityExceptionEntity> SecurityExceptions => Set<SecurityExceptionEntity>();
    public DbSet<VulnerabilityEnrichment> VulnerabilityEnrichments => Set<VulnerabilityEnrichment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(160).IsRequired();
            entity.Property(e => e.Path).HasMaxLength(1024).IsRequired();
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasMany(e => e.Commands).WithOne(e => e.Project).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Scans).WithOne(e => e.Project).HasForeignKey(e => e.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectCommand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Command).HasMaxLength(260).IsRequired();
            entity.Property(e => e.Arguments).HasMaxLength(2048);
            entity.Property(e => e.WorkingDirectory).HasMaxLength(1024);
            entity.HasIndex(e => e.ProjectId);
        });

        modelBuilder.Entity<Scan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RawReportPath).HasMaxLength(1024);
            entity.Property(e => e.LogPath).HasMaxLength(1024);
            entity.Property(e => e.TrivyVersion).HasMaxLength(80);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.StartedAt);
        });

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FindingKey).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Target).HasMaxLength(1024);
            entity.Property(e => e.VulnerabilityId).HasMaxLength(120);
            entity.Property(e => e.PackageName).HasMaxLength(300);
            entity.Property(e => e.PackagePath).HasMaxLength(1024);
            entity.Property(e => e.Ecosystem).HasMaxLength(80);
            entity.Property(e => e.InstalledVersion).HasMaxLength(160);
            entity.Property(e => e.FixedVersion).HasMaxLength(300);
            entity.Property(e => e.RecommendedFixedVersion).HasMaxLength(160);
            entity.Property(e => e.OtherFixedVersions).HasMaxLength(300);
            entity.Property(e => e.SeveritySource).HasMaxLength(120);
            entity.Property(e => e.PrimaryUrl).HasMaxLength(1024);
            entity.Property(e => e.CvssVector).HasMaxLength(260);
            entity.Property(e => e.CvssSource).HasMaxLength(120);
            entity.Property(e => e.CweIds).HasMaxLength(300);
            entity.Property(e => e.EnrichmentSource).HasMaxLength(120);
            entity.Property(e => e.RuntimeSupportAlert).HasMaxLength(1000);
            entity.Property(e => e.FilePath).HasMaxLength(1024);
            entity.HasIndex(e => e.ScanId);
            entity.HasIndex(e => e.VulnerabilityId);
            entity.HasIndex(e => e.PackageName);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.FindingKey);
            entity.HasMany(e => e.References).WithOne(e => e.Finding).HasForeignKey(e => e.FindingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Occurrences).WithOne(e => e.Finding).HasForeignKey(e => e.FindingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FindingReference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Url).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(240);
            entity.HasIndex(e => e.FindingId);
        });

        modelBuilder.Entity<FindingOccurrence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Target).HasMaxLength(1024);
            entity.Property(e => e.FilePath).HasMaxLength(1024);
            entity.Property(e => e.RelativePath).HasMaxLength(1024);
            entity.Property(e => e.AbsolutePath).HasMaxLength(1024);
            entity.Property(e => e.ProjectFilePath).HasMaxLength(1024);
            entity.Property(e => e.ProjectName).HasMaxLength(300);
            entity.HasIndex(e => e.FindingId);
        });

        modelBuilder.Entity<SecurityExceptionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FindingKey).HasMaxLength(512);
            entity.Property(e => e.VulnerabilityId).HasMaxLength(120);
            entity.Property(e => e.PackageName).HasMaxLength(300);
            entity.Property(e => e.InstalledVersion).HasMaxLength(160);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(160);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.FindingKey);
        });

        modelBuilder.Entity<VulnerabilityEnrichment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.VulnerabilityId).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CvssVector).HasMaxLength(260);
            entity.Property(e => e.CweIds).HasMaxLength(300);
            entity.Property(e => e.Source).HasMaxLength(120).IsRequired();
            entity.HasIndex(e => e.VulnerabilityId).IsUnique();
        });
    }
}
