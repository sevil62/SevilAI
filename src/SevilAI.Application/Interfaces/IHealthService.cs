using SevilAI.Application.DTOs;

namespace SevilAI.Application.Interfaces;

public interface IHealthService
{
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
