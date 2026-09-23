using Microsoft.EntityFrameworkCore;

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef;

public sealed class StudioDbContext : DbContext
{
    public StudioDbContext(DbContextOptions<StudioDbContext> options)
        : base(options)
    {
    }

    public DbSet<ModelEntity> Models => Set<ModelEntity>();

    public DbSet<ProposalEntity> Proposals => Set<ProposalEntity>();

    public DbSet<RagChunkEntity> RagChunks => Set<RagChunkEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModelEntity>(entity =>
        {
            entity.ToTable("models");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(512).IsRequired();
            entity.Property(e => e.ArchimateXml).HasColumnType("text").IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.HasIndex(e => e.UpdatedAt);
        });

        modelBuilder.Entity<ProposalEntity>(entity =>
        {
            entity.ToTable("proposals");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatchJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.Source).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<RagChunkEntity>(entity =>
        {
            entity.ToTable("rag_chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceId).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Text).HasColumnType("text").IsRequired();
            entity.Property(e => e.EmbeddingJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.MetadataJson).HasColumnType("text").IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => new { e.ModelId, e.Kind, e.SourceId }).IsUnique();
        });
    }
}
