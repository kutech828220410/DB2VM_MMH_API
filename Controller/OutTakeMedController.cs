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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using MySql.Data.MySqlClient;
using HIS_DB_Lib;
namespace DB2VM_API
{
   
    [Route("api/[controller]")]
    [ApiController]
    public class OutTakeMedController
    {
        [Route("log")]
        [HttpGet()]
        public string Get_log()
        {
            SQLUI.SQLControl sQLControl = new SQLUI.SQLControl("127.0.0.1", "dbvm", "log", "user", "66437068", 3306, MySqlSslMode.None);
            List<object[]> list_value = sQLControl.GetAllRows(null);

            return list_value.JsonSerializationt(true);
        }
        [Route("Sample")]
        [HttpGet()]
        public string Get_Sample()
        {
            string str = Basic.Net.WEBApiGet(@"http://10.13.66.58:4433/api/OutTakeMed/Sample");

            return str;
        }
        [HttpPost]
        public string Post([FromBody] List<class_OutTakeMed_data> data)
        {
            if (data.Count == 0) return "";
            string json = data.JsonSerializationt();
            string str = "";
            //SQLUI.SQLControl sQLControl = new SQLUI.SQLControl("127.0.0.1", "dbvm", "log", "user", "66437068", 3306, MySqlSslMode.None);
            //object[] value = new object[3];
            //value[0] = Guid.NewGuid().ToString();
            //value[1] = DateTime.Now.ToDateTimeString();
            //value[2] = data.JsonSerializationt(false);
            //sQLControl.AddRow(null, value);

            if (data[0].成本中心 == "1")
            {
                returnData returnData = new returnData();
                returnData.ServerName = "B1UD";
                returnData.Data = data;
                str = Basic.Net.WEBApiPostJson("http://10.13.66.58:4433/api/OutTakeMed/new", returnData.JsonSerializationt());
            }
            if (data[0].成本中心 == "2")
            {
                returnData returnData = new returnData();
                returnData.ServerName = "B2UD";
                returnData.Data = data;
                str = Basic.Net.WEBApiPostJson("http://10.13.66.58:4433/api/OutTakeMed/new", returnData.JsonSerializationt());
            }
            return str;
        }
    }
}
