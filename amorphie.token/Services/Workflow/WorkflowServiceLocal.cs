
using amorphie.token.core.Models.Workflow;

namespace amorphie.token.Services.Workflow;

public class WorkflowServiceLocal : IWorkflowService
{
    private readonly IHttpClientFactory _httpClientFactory;
    public WorkflowServiceLocal(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<ServiceResponse<WorkflowPostTransitionResponse>> PostTransaction(WorkflowPostTransitionRequest workflowPostTransactionRequest)
    {
        throw new NotImplementedException();
    }
}
