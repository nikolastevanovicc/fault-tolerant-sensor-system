using Microsoft.EntityFrameworkCore;
using Persistence.Entities;

namespace Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<SensorReadingEntity> SensorReadings => Set<SensorReadingEntity>();

    public DbSet<SensorStateEntity> SensorStates => Set<SensorStateEntity>();

    public DbSet<ConsensusReadingEntity> ConsensusReadings { get; set; }

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

        modelBuilder.Entity<ConsensusReadingEntity>(entity =>
        {
            entity.ToTable("ConsensusReadings");
            entity.HasKey(reading => reading.Id);
            entity.Property(reading => reading.PeriodStart).IsRequired();
            entity.Property(reading => reading.PeriodEnd).IsRequired();
            entity.Property(reading => reading.Value).IsRequired();
            entity.Property(reading => reading.UsedSensorCount).IsRequired();
            entity.Property(reading => reading.RawReadingCount).IsRequired();
            entity.Property(reading => reading.Algorithm).HasMaxLength(100).IsRequired();
            entity.Property(reading => reading.CreatedAt).IsRequired();
            entity.HasIndex(reading => new { reading.PeriodStart, reading.PeriodEnd }).IsUnique();
        });
    }
}
