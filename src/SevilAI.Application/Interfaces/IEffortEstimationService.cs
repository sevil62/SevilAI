using SevilAI.Application.DTOs;

namespace SevilAI.Application.Interfaces;

public interface IEffortEstimationService
{
    Task<EstimateResponse> EstimateAsync(EstimateRequest request, CancellationToken cancellationToken = default);
}
