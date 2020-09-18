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
using Microsoft.Azure.Storage.Blob;
using LCU.StateAPI.Utilities;
using LCU.Personas.Client.Applications;
using LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement.DataFlows
{
    [Serializable]
    [DataContract]
    public class DeployDataFlowRequest
    {
        [DataMember]
        public virtual string DataFlowLookup { get; set; }
    }

    public class DeployDataFlow
    {
        protected ApplicationDeveloperClient appDev;
        
        protected ApplicationManagerClient appMgr;

        public DeployDataFlow(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev)
        {
            this.appDev = appDev;
            
            this.appMgr = appMgr;
        }

        [FunctionName("DeployDataFlow")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = DataFlowManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<DataFlowManagementState, DeployDataFlowRequest, DataFlowManagementStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Deploying Data Flow: {reqData.DataFlowLookup}");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.DeployDataFlow(appMgr, appDev, stateDetails.EnterpriseLookup, reqData.DataFlowLookup);

                return Status.Success;
            });
        }
    }
}
