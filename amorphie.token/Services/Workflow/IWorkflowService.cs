
using amorphie.token.core.Models.Workflow;

namespace amorphie.token.Services.Workflow;

public interface IWorkflowService
{
    public Task<ServiceResponse<WorkflowPostTransitionResponse>> PostTransaction(WorkflowPostTransitionRequest workflowPostTransactionRequest);
}
