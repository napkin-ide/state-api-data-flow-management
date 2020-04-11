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
using LCU.Personas.Client.Applications;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement
{
    [Serializable]
    [DataContract]
    public class DeleteDataFlowRequest
    {
        [DataMember]
        public virtual string DataFlowLookup { get; set; }
    }

    public class DeleteDataFlow
    {
        protected ApplicationDeveloperClient appDev;
        
        protected ApplicationManagerClient appMgr;

        public DeleteDataFlow(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev)
        {
            this.appDev = appDev;
            
            this.appMgr = appMgr;
        }

        [FunctionName("DeleteDataFlow")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = DataFlowManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<DataFlowManagementState, DeleteDataFlowRequest, DataFlowManagementStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Deleting Data Flow: {reqData.DataFlowLookup}");
                
                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.DeleteDataFlow(appMgr, appDev, stateDetails.EnterpriseAPIKey, reqData.DataFlowLookup);

                return Status.Success;
            });
        }
    }
}
