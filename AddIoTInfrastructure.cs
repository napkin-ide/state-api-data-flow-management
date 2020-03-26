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
using LCU.Personas.Client.DevOps;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement
{
    [Serializable]
    [DataContract]
    public class AddIoTInfrastructureRequest
    {
    }

    public class AddIoTInfrastructure
    {
        protected DevOpsArchitectClient devOpsArch;

        public AddIoTInfrastructure(DevOpsArchitectClient devOpsArch)
        {
            this.devOpsArch = devOpsArch;
        }
        
        [FunctionName("AddIoTInfrastructure")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = DataFlowManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-api-key}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<DataFlowManagementState, AddIoTInfrastructureRequest, DataFlowManagementStateHarness>(req, signalRMessages, log,
                async (harness, reqData, actReq) =>
            {
                log.LogInformation($"Adding IoT Infrastructure");
                
                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.AddIoTInfrastructure(devOpsArch, stateDetails.EnterpriseAPIKey, stateDetails.Username);
            });
        }
    }
}
