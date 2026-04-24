using Codify.Application.DTOs.Execution;

namespace Codify.Application.Interfaces;

public interface IExecutionService
{
    Task<RunCodeResponse> RunAsync(RunCodeRequest request);
}
