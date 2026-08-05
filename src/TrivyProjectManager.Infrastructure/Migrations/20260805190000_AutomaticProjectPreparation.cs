using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TrivyProjectManager.Infrastructure.Data;

#nullable disable

namespace TrivyProjectManager.Infrastructure.Migrations;

[DbContext(typeof(TrivyProjectManagerDbContext))]
[Migration("20260805190000_AutomaticProjectPreparation")]
public partial class AutomaticProjectPreparation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE Projects ADD COLUMN AutoDetectPreparation INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE Projects ADD COLUMN IsPreparationTrusted INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE ProjectCommands ADD COLUMN PreparationTargetKey TEXT NULL;

            UPDATE Projects
            SET AutoDetectPreparation = 1
            WHERE NOT EXISTS (
                SELECT 1 FROM ProjectCommands custom
                WHERE custom.ProjectId = Projects.Id
                  AND NOT (
                    (custom.Command = 'dotnet' AND custom.Name IN ('Restore', 'Build', 'Test'))
                    OR (custom.Command IN ('npm', 'pnpm', 'yarn') AND custom.Name IN ('Install', 'Build', 'Test'))
                  )
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // SQLite cannot drop these columns without rebuilding the tables.
    }
}
