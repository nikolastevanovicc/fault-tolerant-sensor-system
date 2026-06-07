using IngestionService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IngestionService.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorReadingEntity> SensorReadings => Set<SensorReadingEntity>();

    public DbSet<SensorStateEntity> SensorStates => Set<SensorStateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SensorReadingEntity>(entity =>
        {
            entity.ToTable("SensorReadings");
            entity.HasKey(reading => reading.Id);
            entity.Property(reading => reading.SensorId).HasMaxLength(100).IsRequired();
            entity.Property(reading => reading.Temperature).IsRequired();
            entity.Property(reading => reading.Timestamp).IsRequired();
            entity.Property(reading => reading.ReceivedAt).IsRequired();
            entity.Property(reading => reading.Quality).IsRequired();
            entity.Property(reading => reading.AlarmPriority).IsRequired();
            entity.Property(reading => reading.MessageId).IsRequired();
            entity.Property(reading => reading.IsConsensusValue).IsRequired();
            entity.HasIndex(reading => new { reading.SensorId, reading.MessageId });
            entity.HasIndex(reading => reading.Timestamp);
        });

        modelBuilder.Entity<SensorStateEntity>(entity =>
        {
            entity.ToTable("SensorStates");
            entity.HasKey(state => state.SensorId);
            entity.Property(state => state.SensorId).HasMaxLength(100).IsRequired();
            entity.Property(state => state.LastMessageTime).IsRequired();
            entity.Property(state => state.IsActive).IsRequired();
            entity.Property(state => state.Quality).IsRequired();
            entity.Property(state => state.LastMessageId).IsRequired();
        });
    }
}
