using Microsoft.EntityFrameworkCore;
using Pigment.Infrastructure.Entities;

namespace Pigment.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the Pigment integration — tracks run history only.
/// HR query results come directly via Dapper (no EF mapping needed).
/// </summary>
public sealed class PigmentDbContext : DbContext
{
    public PigmentDbContext(DbContextOptions<PigmentDbContext> options) : base(options) { }

    public DbSet<PigmentRunEntity> PigmentRuns { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PigmentRunEntity>(entity =>
        {
            entity.ToTable("PigmentRuns", "Pigment");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TaxYear).HasMaxLength(4).IsRequired();
            entity.Property(e => e.TaxPeriod).HasMaxLength(2).IsRequired();
            entity.Property(e => e.TriggeredBy).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.FileUrl).HasMaxLength(2000);
            entity.Property(e => e.RecordCount);
            entity.Property(e => e.RunDateTimeUtc).IsRequired();
        });
    }
}
