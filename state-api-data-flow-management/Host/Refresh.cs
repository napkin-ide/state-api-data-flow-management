using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Fathym;
using Microsoft.Azure.Storage.Blob;
using System.Runtime.Serialization;
using Fathym.API;
using System.Collections.Generic;
using System.Linq;
using LCU.Personas.Client.Applications;
using LCU.StateAPI.Utilities;
using System.Security.Claims;
using LCU.Personas.Client.Enterprises;
using LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement.State;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement.Host
{
    [Serializable]
    [DataContract]
    public class RefreshRequest : BaseRequest
    { }

    public class Refresh
    {
        protected ApplicationDeveloperClient appDev;
        
        protected ApplicationManagerClient appMgr;
        
        protected EnterpriseManagerClient entMgr;

        public Refresh(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev)
        {
            this.appDev = appDev;
            
            this.appMgr = appMgr;
            
            this.entMgr = entMgr;
        }

        [FunctionName("Refresh")]
        public virtual async Task<Status> Run([HttpTrigger] HttpRequest req, ILogger log,
            [SignalR(HubName = DataFlowManagementState.HUB_NAME)]IAsyncCollector<SignalRMessage> signalRMessages,
            [Blob("state-api/{headers.lcu-ent-lookup}/{headers.lcu-hub-name}/{headers.x-ms-client-principal-id}/{headers.lcu-state-key}", FileAccess.ReadWrite)] CloudBlockBlob stateBlob)
        {
            return await stateBlob.WithStateHarness<DataFlowManagementState, RefreshRequest, DataFlowManagementStateHarness>(req, signalRMessages, log,
                async (harness, refreshReq, actReq) =>
            {
                log.LogInformation($"Refresh");

                var stateDetails = StateUtils.LoadStateDetails(req);

                await harness.Refresh(entMgr, appMgr, appDev, stateDetails.EnterpriseLookup, stateDetails.Host);

                return Status.Success;
            });
        }
    }
}
