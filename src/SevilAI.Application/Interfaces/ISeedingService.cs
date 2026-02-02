using SevilAI.Application.DTOs;

namespace SevilAI.Application.Interfaces;

public interface ISeedingService
{
    Task<SeedResponse> SeedFromJsonAsync(string? jsonData, bool clearExisting, CancellationToken cancellationToken = default);
    Task<SeedResponse> SeedFromEmbeddedResourceAsync(bool clearExisting, CancellationToken cancellationToken = default);
}
