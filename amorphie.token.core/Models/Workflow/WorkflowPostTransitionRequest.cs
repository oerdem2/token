namespace amorphie.token.core.Models.Workflow;

public class WorkflowPostTransitionRequest
{
    public string EntityData{get;set;}
    public string FormData{get;set;}
    public string additionalData{get;set;}
    public bool GetSignalRHub{get;set;}
    public string RouteData{get;set;}
    public string QueryData{get;set;}
}
