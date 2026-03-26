using Microsoft.Extensions.Logging;
using ModuleLLM.Services;
using Shared.Contracts;

namespace Application.Services.Llm;

/// <summary>
/// Сервис для работы с LLM
/// </summary>
public class LlmService : ILlmService
{
    private readonly ILlmApiService _llmApiService;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        ILlmApiService llmApiService,
        ILogger<LlmService> logger)
    {
        _llmApiService = llmApiService;
        _logger = logger;
    }
}
