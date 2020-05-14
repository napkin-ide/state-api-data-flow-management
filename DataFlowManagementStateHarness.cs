using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fathym;
using LCU.Presentation.State.ReqRes;
using LCU.StateAPI.Utilities;
using LCU.StateAPI;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Collections.Generic;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Enterprises;
using LCU.Personas.Client.Applications;
using Fathym.API;
using LCU.Graphs.Registry.Enterprises.DataFlows;
using Newtonsoft.Json.Linq;

namespace LCU.State.API.NapkinIDE.NapkinIDE.DataFlowManagement
{
    public class DataFlowManagementStateHarness : LCUStateHarness<DataFlowManagementState>
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public DataFlowManagementStateHarness(DataFlowManagementState state)
            : base(state ?? new DataFlowManagementState())
        { }
        #endregion

        #region API Methods
        public virtual async Task AddIoTInfrastructure(DevOpsArchitectClient devOpsArch, string entApiKey, string username)
        {
            var resp = await devOpsArch.SetEnvironmentInfrastructure(new Personas.DevOps.SetEnvironmentInfrastructureRequest()
            {
                Template = "fathym\\daf-iot-setup"
            }, entApiKey, State.EnvironmentLookup, username);
        }

        public virtual async Task CheckActiveDataFlowStatus(ApplicationDeveloperClient appDev, string entApiKey)
        {
            var resp = await appDev.CheckDataFlowStatus(new Personas.Applications.CheckDataFlowStatusRequest()
            {
                DataFlow = State.ActiveDataFlow,
                Type = Personas.Applications.DataFlowStatusTypes.QuickView
            }, entApiKey, State.EnvironmentLookup);

            State.ActiveDataFlow = resp.DataFlow;
        }

        public virtual async Task DeleteDataFlow(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev, string entApiKey, string dataFlowLookup)
        {
            var resp = await appMgr.DeleteDataFlow(entApiKey, State.EnvironmentLookup, dataFlowLookup);

            await LoadDataFlows(appMgr, appDev, entApiKey);
        }

        public virtual async Task DeployDataFlow(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev, string entApiKey, string dataFlowLookup)
        {
            var resp = await appDev.DeployDataFlow(new Personas.Applications.DeployDataFlowRequest()
            {
                DataFlowLookup = dataFlowLookup
            }, entApiKey, State.EnvironmentLookup);

            State.IsCreating = !resp.Status;

            await LoadDataFlows(appMgr, appDev, entApiKey);
        }

        public virtual async Task LoadDataFlows(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev, string entApiKey)
        {
            var resp = await appMgr.ListDataFlows(entApiKey, State.EnvironmentLookup);

            State.DataFlows = resp.Model;

            await SetActiveDataFlow(appDev, entApiKey, State?.ActiveDataFlow?.Lookup);
        }

        public virtual async Task LoadEnvironment(EnterpriseManagerClient entMgr, string entApiKey)
        {
            var resp = await entMgr.ListEnvironments(entApiKey);

            State.EnvironmentLookup = resp.Model?.FirstOrDefault()?.Lookup;
        }

        public virtual async Task LoadInfrastructure(EnterpriseManagerClient entMgr, string entApiKey, string envLookup, string type)
        {
            var regHosts = await entMgr.LoadInfrastructureDetails(entApiKey, envLookup, type);

            State.InfrastructureDetails = regHosts.Model;
        }

        public virtual async Task LoadModulePackSetup(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, string entApiKey, string host)
        {
            var mpsResp = await appMgr.ListModulePackSetups(entApiKey);

            State.ModulePacks = new List<ModulePack>();

            State.ModuleDisplays = new List<ModuleDisplay>();

            State.ModuleOptions = new List<ModuleOption>();

            var moduleOptions = new List<ModuleOption>();

            if (mpsResp.Status)
            {
                mpsResp.Model.Each(mps =>
                {
                    State.ModulePacks = State.ModulePacks.Where(mp => mp.Lookup != mps.Pack.Lookup).ToList();

                    if (mps.Pack != null)
                        State.ModulePacks.Add(mps.Pack);

                    State.ModuleDisplays = State.ModuleDisplays.Where(mp => !mps.Displays.Any(disp => disp.ModuleType == mp.ModuleType)).ToList();

                    if (!mps.Displays.IsNullOrEmpty())
                        State.ModuleDisplays.AddRange(mps.Displays.Select(md =>
                        {
                            md.Element = $"{mps.Pack.Lookup}-{md.ModuleType}-element";

                            md.Toolkit = $"https://{host}{mps.Pack.Toolkit}";

                            return md;
                        }));

                    moduleOptions = moduleOptions.Where(mo => !mps.Options.Any(opt => opt.ModuleType == mo.ModuleType)).ToList();

                    if (!mps.Options.IsNullOrEmpty())
                        moduleOptions.AddRange(mps.Options.Select(mo =>
                        {
                            mo.Settings = new MetadataModel();

                            return mo;
                        }));
                });

                var nonInfraModuleTypes = new[] { "data-map", "data-emulator", "warm-query" };

                await moduleOptions.Each(async mo =>
                {
                    var moInfraResp = await entMgr.LoadInfrastructureDetails(entApiKey, State.EnvironmentLookup, mo.ModuleType);

                    var moDisp = State.ModuleDisplays.FirstOrDefault(md => md.ModuleType == mo.ModuleType);

                    if (!nonInfraModuleTypes.Contains(mo.ModuleType) && moInfraResp.Status && !moInfraResp.Model.IsNullOrEmpty())
                    {
                        moInfraResp.Model.Each(infraDets =>
                        {
                            var newMO = mo.JSONConvert<ModuleOption>();

                            newMO.ID = Guid.Empty;

                            newMO.Name = $"{mo.Name} - {infraDets.DisplayName}";

                            newMO.Settings.Metadata["Infrastructure"] = infraDets.JSONConvert<JToken>();

                            State.ModuleOptions.Add(newMO);

                            var newMODisp = moDisp.JSONConvert<ModuleDisplay>();

                            newMODisp.ModuleType = newMO.ModuleType;

                            State.ModuleDisplays.Add(newMODisp);

                            // if (State.AllowCreationModules)
                            // {
                            //     State.ModuleOptions.Add(mo);

                            //     State.ModuleDisplays.Add(moDisp);
                            // }
                        });
                    }
                    else if (nonInfraModuleTypes.Contains(mo.ModuleType))
                    {
                        State.ModuleOptions.Add(mo);

                        State.ModuleDisplays.Add(moDisp);
                    }
                });
            }
        }

        public virtual async Task Refresh(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev, string entApiKey, string host)
        {
            await LoadEnvironment(entMgr, entApiKey);

            await LoadDataFlows(appMgr, appDev, entApiKey);

            await LoadModulePackSetup(entMgr, appMgr, entApiKey, host);

        }

        public virtual async Task SaveDataFlow(ApplicationManagerClient appMgr, ApplicationDeveloperClient appDev, string entApiKey, DataFlow dataFlow)
        {
            var resp = await appMgr.SaveDataFlow(dataFlow, entApiKey, State.EnvironmentLookup);

            State.IsCreating = !resp.Status;

            await LoadDataFlows(appMgr, appDev, entApiKey);
        }

        public virtual async Task SetActiveDataFlow(ApplicationDeveloperClient appDev, string entApiKey, string dfLookup)
        {
            State.ActiveDataFlow = State.DataFlows.FirstOrDefault(df => df.Lookup == dfLookup);

            if (State.ActiveDataFlow != null)
            {
                //  Trying on refresh only...
                // await LoadModulePackSetup();

                await CheckActiveDataFlowStatus(appDev, entApiKey);
            }
        }

        public virtual async Task ToggleCreationModules(EnterpriseManagerClient entMgr, ApplicationManagerClient appMgr, string entApiKey, string host)
        {
            State.AllowCreationModules = !State.AllowCreationModules;

            await LoadModulePackSetup(entMgr, appMgr, entApiKey, host);
        }

        public virtual async Task ToggleIsCreating()
        {
            State.IsCreating = !State.IsCreating;
        }
        #endregion
    }
}
