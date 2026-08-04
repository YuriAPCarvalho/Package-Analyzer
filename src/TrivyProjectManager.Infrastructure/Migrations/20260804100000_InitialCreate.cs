using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrivyProjectManager.Infrastructure.Data;

#nullable disable

namespace TrivyProjectManager.Infrastructure.Migrations;

[DbContext(typeof(TrivyProjectManagerDbContext))]
[Migration("20260804100000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS Projects (
                Id TEXT NOT NULL CONSTRAINT PK_Projects PRIMARY KEY,
                Name TEXT NOT NULL,
                Path TEXT NOT NULL,
                Technology INTEGER NOT NULL,
                PackageManager INTEGER NOT NULL,
                StorageMode INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                LastScanAt TEXT NULL,
                IsActive INTEGER NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Projects_Path ON Projects(Path);
            CREATE TABLE IF NOT EXISTS ProjectCommands (
                Id TEXT NOT NULL CONSTRAINT PK_ProjectCommands PRIMARY KEY,
                ProjectId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Command TEXT NOT NULL,
                Arguments TEXT NOT NULL,
                ExecutionOrder INTEGER NOT NULL,
                IsEnabled INTEGER NOT NULL,
                ContinueOnError INTEGER NOT NULL,
                WorkingDirectory TEXT NULL,
                CONSTRAINT FK_ProjectCommands_Projects_ProjectId FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_ProjectCommands_ProjectId ON ProjectCommands(ProjectId);
            CREATE TABLE IF NOT EXISTS Scans (
                Id TEXT NOT NULL CONSTRAINT PK_Scans PRIMARY KEY,
                ProjectId TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                FinishedAt TEXT NULL,
                Status INTEGER NOT NULL,
                TrivyVersion TEXT NULL,
                TrivyDatabaseUpdatedAt TEXT NULL,
                CriticalCount INTEGER NOT NULL,
                HighCount INTEGER NOT NULL,
                MediumCount INTEGER NOT NULL,
                LowCount INTEGER NOT NULL,
                UnknownCount INTEGER NOT NULL,
                MisconfigurationCount INTEGER NOT NULL,
                SecretCount INTEGER NOT NULL,
                UniqueVulnerabilityCount INTEGER NOT NULL,
                TotalOccurrenceCount INTEGER NOT NULL,
                NewCount INTEGER NOT NULL,
                ResolvedCount INTEGER NOT NULL,
                ExistingCount INTEGER NOT NULL,
                RegressionCount INTEGER NOT NULL,
                RawReportPath TEXT NULL,
                LogPath TEXT NULL,
                ErrorMessage TEXT NULL,
                CONSTRAINT FK_Scans_Projects_ProjectId FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_Scans_ProjectId ON Scans(ProjectId);
            CREATE INDEX IF NOT EXISTS IX_Scans_StartedAt ON Scans(StartedAt);
            CREATE TABLE IF NOT EXISTS Findings (
                Id TEXT NOT NULL CONSTRAINT PK_Findings PRIMARY KEY,
                ScanId TEXT NOT NULL,
                FindingKey TEXT NOT NULL,
                FindingType INTEGER NOT NULL,
                Target TEXT NULL,
                VulnerabilityId TEXT NULL,
                PackageName TEXT NULL,
                PackagePath TEXT NULL,
                InstalledVersion TEXT NULL,
                FixedVersion TEXT NULL,
                Severity INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Title TEXT NULL,
                Description TEXT NULL,
                PrimaryUrl TEXT NULL,
                PublishedDate TEXT NULL,
                LastModifiedDate TEXT NULL,
                FilePath TEXT NULL,
                StartLine INTEGER NULL,
                MaskedCodeSnippet TEXT NULL,
                CONSTRAINT FK_Findings_Scans_ScanId FOREIGN KEY(ScanId) REFERENCES Scans(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_Findings_ScanId ON Findings(ScanId);
            CREATE INDEX IF NOT EXISTS IX_Findings_VulnerabilityId ON Findings(VulnerabilityId);
            CREATE INDEX IF NOT EXISTS IX_Findings_PackageName ON Findings(PackageName);
            CREATE INDEX IF NOT EXISTS IX_Findings_Severity ON Findings(Severity);
            CREATE INDEX IF NOT EXISTS IX_Findings_FindingKey ON Findings(FindingKey);
            CREATE TABLE IF NOT EXISTS FindingReferences (
                Id TEXT NOT NULL CONSTRAINT PK_FindingReferences PRIMARY KEY,
                FindingId TEXT NOT NULL,
                Url TEXT NOT NULL,
                CONSTRAINT FK_FindingReferences_Findings_FindingId FOREIGN KEY(FindingId) REFERENCES Findings(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_FindingReferences_FindingId ON FindingReferences(FindingId);
            CREATE TABLE IF NOT EXISTS FindingOccurrences (
                Id TEXT NOT NULL CONSTRAINT PK_FindingOccurrences PRIMARY KEY,
                FindingId TEXT NOT NULL,
                Target TEXT NULL,
                FilePath TEXT NULL,
                ProjectName TEXT NULL,
                CONSTRAINT FK_FindingOccurrences_Findings_FindingId FOREIGN KEY(FindingId) REFERENCES Findings(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_FindingOccurrences_FindingId ON FindingOccurrences(FindingId);
            CREATE TABLE IF NOT EXISTS SecurityExceptions (
                Id TEXT NOT NULL CONSTRAINT PK_SecurityExceptions PRIMARY KEY,
                ProjectId TEXT NOT NULL,
                VulnerabilityId TEXT NULL,
                PackageName TEXT NULL,
                Reason TEXT NOT NULL,
                CreatedBy TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                ExpiresAt TEXT NULL,
                IsActive INTEGER NOT NULL,
                CONSTRAINT FK_SecurityExceptions_Projects_ProjectId FOREIGN KEY(ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS IX_SecurityExceptions_ProjectId ON SecurityExceptions(ProjectId);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SecurityExceptions");
        migrationBuilder.DropTable("FindingOccurrences");
        migrationBuilder.DropTable("FindingReferences");
        migrationBuilder.DropTable("Findings");
        migrationBuilder.DropTable("ProjectCommands");
        migrationBuilder.DropTable("Scans");
        migrationBuilder.DropTable("Projects");
    }
}
