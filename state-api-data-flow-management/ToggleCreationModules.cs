using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Fathym;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.WindowsAzure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Applications;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement
{
    [Serializable]
    [DataContract]
    public class ToggleCreationModulesRequest
    { }

    public class ToggleCreationModules
    {
        protected ApplicationManagerClient appMgr;
        
        protected EnterpriseManagerClient entMgr;

        public ToggleCreationModules(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr)
        {
            this.appMgr = appMgr;
            
            this.entMgr = entMgr;
        }

        [FunctionName("ToggleCreationModules")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = DataFlowManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<DataFlowManagementState, ToggleCreationModulesRequest, DataFlowManagementStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Refresh");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.ToggleCreationModules(entMgr, appMgr, stateDetails.EnterpriseAPIKey, stateDetails.Host);

                return Status.Success;
            });
        }
    }
}
