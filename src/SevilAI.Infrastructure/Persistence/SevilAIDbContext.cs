using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SevilAI.Domain.Entities;

namespace SevilAI.Infrastructure.Persistence;

public class SevilAIDbContext : DbContext
{
    public SevilAIDbContext(DbContextOptions<SevilAIDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<QueryLog> QueryLogs => Set<QueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isInMemory = Database.IsInMemory();

        // JSON converters for Dictionary properties
        var dictConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

        var dictComparer = new ValueComparer<Dictionary<string, object>>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

        var listStringConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        var listStringComparer = new ValueComparer<List<string>>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => JsonSerializer.Deserialize<List<string>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<string>());

        var listGuidConverter = new ValueConverter<List<Guid>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>());

        var listGuidComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
            c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
            c => JsonSerializer.Deserialize<List<Guid>>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new List<Guid>());

        // Enable pgvector extension (only for PostgreSQL)
        if (!isInMemory)
        {
            modelBuilder.HasPostgresExtension("vector");
        }

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasMany(d => d.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Chunk configuration
        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.ToTable("chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(e => e.ChunkIndex).HasColumnName("chunk_index").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.TokenCount).HasColumnName("token_count").IsRequired();
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex }).IsUnique();

            entity.HasOne(c => c.Embedding)
                  .WithOne(e => e.Chunk)
                  .HasForeignKey<Embedding>(e => e.ChunkId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Embedding configuration
        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.ToTable("embeddings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChunkId).HasColumnName("chunk_id").IsRequired();
            entity.Property(e => e.ModelName).HasColumnName("model_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            if (isInMemory)
            {
                // For InMemory, store as JSON string
                var floatArrayConverter = new ValueConverter<float[], string>(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<float[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<float>());

                entity.Property(e => e.Vector)
                      .HasColumnName("embedding")
                      .HasConversion(floatArrayConverter);
            }
            else
            {
                // For PostgreSQL with pgvector
                entity.Property(e => e.Vector)
                      .HasColumnName("embedding")
                      .HasColumnType("vector(384)")
                      .HasConversion(
                          v => new Vector(v),
                          v => v.ToArray());
            }

            entity.HasIndex(e => e.ChunkId).IsUnique();
        });

        // Skill configuration
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("skills");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100)
                  .HasConversion<string>();
            entity.Property(e => e.ProficiencyLevel).HasColumnName("proficiency_level").HasMaxLength(50)
                  .HasConversion<string>();
            entity.Property(e => e.YearsExperience).HasColumnName("years_experience").HasPrecision(4, 1);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Experience configuration
        modelBuilder.Entity<Experience>(entity =>
        {
            entity.ToTable("experiences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Company).HasColumnName("company").HasMaxLength(300).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(300).IsRequired();
            entity.Property(e => e.PeriodStart).HasColumnName("period_start");
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Achievements).HasColumnName("achievements")
                  .HasConversion(listStringConverter)
                  .Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.Technologies).HasColumnName("technologies")
                  .HasConversion(listStringConverter)
                  .Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.IsConfidential).HasColumnName("is_confidential");
            entity.Property(e => e.NdaNote).HasColumnName("nda_note");
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(300).IsRequired();
            entity.Property(e => e.ProjectType).HasColumnName("project_type").HasMaxLength(100)
                  .HasConversion<string>();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Technologies).HasColumnName("technologies")
                  .HasConversion(listStringConverter)
                  .Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.Features).HasColumnName("features")
                  .HasConversion(listStringConverter)
                  .Metadata.SetValueComparer(listStringComparer);
            entity.Property(e => e.ArchitectureNotes).HasColumnName("architecture_notes");
            entity.Property(e => e.IsConfidential).HasColumnName("is_confidential");
            entity.Property(e => e.NdaNote).HasColumnName("nda_note");
            entity.Property(e => e.Metadata).HasColumnName("metadata")
                  .HasConversion(dictConverter)
                  .Metadata.SetValueComparer(dictComparer);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // QueryLog configuration
        modelBuilder.Entity<QueryLog>(entity =>
        {
            entity.ToTable("query_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QueryText).HasColumnName("query_text").IsRequired();
            entity.Property(e => e.ResponseText).HasColumnName("response_text");
            entity.Property(e => e.ChunksUsed).HasColumnName("chunks_used")
                  .HasConversion(listGuidConverter)
                  .Metadata.SetValueComparer(listGuidComparer);
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasPrecision(5, 4);
            entity.Property(e => e.GenerationMode).HasColumnName("generation_mode").HasMaxLength(50)
                  .HasConversion<string>();
            entity.Property(e => e.LatencyMs).HasColumnName("latency_ms");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
