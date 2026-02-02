using Microsoft.EntityFrameworkCore;
using SevilAI.Api.Configuration;
using SevilAI.Application.Interfaces;
using SevilAI.Application.Services;
using SevilAI.Domain.Interfaces;
using SevilAI.Infrastructure.Embeddings;
using SevilAI.Infrastructure.LLMProviders;
using SevilAI.Infrastructure.Persistence;
using SevilAI.Infrastructure.Persistence.Repositories;
using SevilAI.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SevilAI API",
        Version = "v1",
        Description = "RAG-based knowledge engine for answering questions about Sevil Aydın's professional profile",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Sevil Aydın",
            Email = "contact@sevilai.dev"
        }
    });
});

// Database configuration
var useInMemory = builder.Configuration.GetValue<bool>("DatabaseSettings:UseInMemory", false);
if (useInMemory)
{
    builder.Services.AddDbContext<SevilAIDbContext>(options =>
    {
        options.UseInMemoryDatabase("SevilAI_InMemory");
    });
}
else
{
    builder.Services.AddDbContext<SevilAIDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.UseVector();
        });
    });
}

// Repository registrations
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkRepository, ChunkRepository>();
builder.Services.AddScoped<IEmbeddingRepository, EmbeddingRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IExperienceRepository, ExperienceRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IQueryLogRepository, QueryLogRepository>();

// Application service registrations
builder.Services.AddScoped<IQuestionAnsweringService, QuestionAnsweringService>();
builder.Services.AddScoped<IEffortEstimationService, EffortEstimationService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<ISeedingService, SeedingService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();

// Embedding service
var embeddingDimensions = builder.Configuration.GetValue<int>("EmbeddingSettings:Dimensions", 384);
builder.Services.AddScoped<IEmbeddingService>(sp =>
    new LocalEmbeddingService(
        sp.GetRequiredService<ILogger<LocalEmbeddingService>>(),
        embeddingDimensions));

// LLM provider configuration
builder.Services.ConfigureLLMProvider(builder.Configuration);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline - Swagger always enabled
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "SevilAI API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Auto-migrate and seed on startup (optional)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SevilAIDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var useInMemoryDb = builder.Configuration.GetValue<bool>("DatabaseSettings:UseInMemory", false);

        if (useInMemoryDb)
        {
            // For in-memory, just ensure database is created
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("In-memory database created");
        }
        else
        {
            // Apply migrations for PostgreSQL
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }

        // Auto-seed if configured
        var autoSeed = builder.Configuration.GetValue<bool>("SeedingSettings:AutoSeedOnStartup", false);
        if (autoSeed)
        {
            var seedingService = scope.ServiceProvider.GetRequiredService<ISeedingService>();
            var existingDocs = await db.Documents.AnyAsync();

            if (!existingDocs)
            {
                logger.LogInformation("Auto-seeding knowledge base...");
                var result = await seedingService.SeedFromEmbeddedResourceAsync(false);
                logger.LogInformation(
                    "Seeding completed: {Docs} documents, {Chunks} chunks, {Embeddings} embeddings",
                    result.DocumentsCreated, result.ChunksCreated, result.EmbeddingsCreated);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database initialization warning - this is expected on first run before DB is ready");
    }
}

app.Run();
