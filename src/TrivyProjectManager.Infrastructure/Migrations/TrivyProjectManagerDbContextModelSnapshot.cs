using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TrivyProjectManager.Infrastructure.Data;

#nullable disable

namespace TrivyProjectManager.Infrastructure.Migrations;

[DbContext(typeof(TrivyProjectManagerDbContext))]
public sealed class TrivyProjectManagerDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        new TrivyProjectManagerDbContext(new DbContextOptionsBuilder<TrivyProjectManagerDbContext>().UseSqlite("Data Source=:memory:").Options)
            .Model.GetEntityTypes();
    }
}
