using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Helpers;

public static class ZeebeMessageHelper
{
    public static dynamic createMessageVariables(dynamic body, string _transitionName, string TriggeredBy, string TriggeredByBehalfOf, dynamic _data, bool success, string message)
    {
        dynamic variables = new Dictionary<string, dynamic>();

        variables.Add("EntityName", body.GetProperty("EntityName").ToString());
        variables.Add("RecordId", body.GetProperty("RecordId").ToString());
        variables.Add("InstanceId", body.GetProperty("InstanceId").ToString());
        variables.Add("LastTransition", _transitionName);
        variables.Add("Message", message);
        if (success)
            variables.Add("Status", "OK");
        else
        {
            variables.Add("Status", "NOTOK");
        }
        dynamic targetObject = new System.Dynamic.ExpandoObject();
        targetObject.Data = _data;
        targetObject.TriggeredBy = TriggeredBy;
        targetObject.TriggeredByBehalfOf = TriggeredByBehalfOf;


        variables.Add($"TRX-{_transitionName}", targetObject);
        return variables;
    }
}
