using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using Oracle.ManagedDataAccess.Client;
namespace DB2VM
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        
        // GET api/values
        [HttpGet]
        public string Get()
        {
            return $"20250221 05:41\n{Basic.Net.WEBApiGet($"http://10.14.16.50:4433/api/test")}";
        }


    }
}
