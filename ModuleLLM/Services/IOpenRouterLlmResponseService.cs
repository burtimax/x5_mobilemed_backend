using ModuleLLM.Models.OpenRouter.LlmResponses;
using Shared.Contracts;

namespace ModuleLLM.Services;

/// <summary>
/// Вызов OpenRouter через Responses API (<c>/api/v1/responses</c>), независимо от chat completions.
/// </summary>
public interface IOpenRouterLlmResponseService
{
    Task<Result<OpenRouterLlmResponsesResponse>> SendAsync(
        OpenRouterLlmResponsesRequest request,
        CancellationToken cancellationToken = default);
}
