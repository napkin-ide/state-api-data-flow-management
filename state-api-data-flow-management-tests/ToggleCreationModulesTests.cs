using Fathym.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace state_api_data_flow_management_tests
{
    [TestClass]
    public class ToggleCreationModulesTests : AzFunctionTestBase
    {
        
        public ToggleCreationModulesTests() : base()
        {
            APIRoute = "api/ToggleCreationModules";                
        }

        [TestMethod]
        public async Task TestToggleCreationModules()
        {
            LcuEntLookup = "";            
            PrincipalId = "";

            addRequestHeaders();

            var url = $"{HostURL}/{APIRoute}";            

            var response = await httpGet(url); 

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var model = getContent<dynamic>(response);

            dynamic result = model.Result;            

            throw new NotImplementedException("Implement me!");                  
        }
    }
}
