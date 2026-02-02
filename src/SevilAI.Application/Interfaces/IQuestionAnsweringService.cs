using SevilAI.Application.DTOs;

namespace SevilAI.Application.Interfaces;

public interface IQuestionAnsweringService
{
    Task<AskResponse> AskAsync(AskRequest request, CancellationToken cancellationToken = default);
}
