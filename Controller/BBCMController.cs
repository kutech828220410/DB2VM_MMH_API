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
using SQLUI;
using System.Xml;
using HIS_DB_Lib;
using System.IO;
using System.Text;
using ExcelDataReader;
namespace DB2VM.Controller
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCMController : ControllerBase
    {


        static string MySQL_server = $"{ConfigurationManager.AppSettings["MySQL_server"]}";
        static string MySQL_database = $"{ConfigurationManager.AppSettings["MySQL_database"]}";
        static string MySQL_userid = $"{ConfigurationManager.AppSettings["MySQL_user"]}";
        static string MySQL_password = $"{ConfigurationManager.AppSettings["MySQL_password"]}";
        static string MySQL_port = $"{ConfigurationManager.AppSettings["MySQL_port"]}";

        private SQLControl sQLControl_藥檔資料 = new SQLControl(MySQL_server, MySQL_database, "medicine_page_cloud", MySQL_userid, MySQL_password, (uint)MySQL_port.StringToInt32(), MySql.Data.MySqlClient.MySqlSslMode.None);

        static public string API_Server = "http://127.0.0.1:4433";

        [HttpGet]
        public string Get(string Code)
        {
            //if (Code.StringIsEmpty()) return "[]";
            System.Text.StringBuilder soap = new System.Text.StringBuilder();
            soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soap.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            soap.Append("<soap:Body>");
            soap.Append("<Drug_DATA xmlns=\"http://tempuri.org/\">");
            soap.Append("<myhospital>1</myhospital>");
            soap.Append("<myNS></myNS>");
            soap.Append($"<myMCODE>{Code}</myMCODE>");
            soap.Append("<myDB>OPD</myDB>");
            soap.Append("</Drug_DATA>");
            soap.Append("</soap:Body>");
            soap.Append("</soap:Envelope>");
            string Xml = Basic.Net.WebServicePost("https://tpord.mmh.org.tw/ADC_WS_A226/ADCDrugWS.asmx?op=Drug_DATA", soap);
            //string[] Node_array = new string[] { "soap:Body", "Drug_DATAResponse", "Drug_DATAResult"};

            string[] Node_array = new string[] { "soap:Body", "Drug_DATAResponse", "Drug_DATAResult", "diffgr:diffgram", "NewDataSet", "Temp1" };
            //List<XmlElement> xmlElements = Xml.Xml_GetElements(Node_array);
            //if (xmlElements == null || xmlElements.Count == 0) return "[]";


            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(Xml);

            // 定義 XPath 取得所有 Temp1 節點
            XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("diffgr", "urn:schemas-microsoft-com:xml-diffgram-v1");
            nsManager.AddNamespace("msdata", "urn:schemas-microsoft-com:xml-msdata");

            XmlNodeList temp1Nodes = xmlDoc.SelectNodes("//Temp1", nsManager);
            List<medClass> medClasses = new List<medClass>();

            if (temp1Nodes == null || temp1Nodes.Count == 0)
            {
                return "[]";
            }

            foreach (XmlNode node in temp1Nodes)
            {
                string MCODE = node.SelectSingleNode("MCODE")?.InnerText ?? "";
                string FullName = node.SelectSingleNode("FULLNAME")?.InnerText ?? "";
                string ShortName = node.SelectSingleNode("SHORTNAME")?.InnerText ?? "";
                string COMPAR2 = node.SelectSingleNode("COMPAR2")?.InnerText ?? "";
                string MLEVEL = node.SelectSingleNode("MLEVEL")?.InnerText ?? "";

                // 檢查 MCODE 是否為空
                if (string.IsNullOrWhiteSpace(MCODE)) continue;

                medClass medClass = new medClass
                {
                    藥品碼 = MCODE,
                    藥品名稱 = FullName,
                    藥品學名 = ShortName,
                    警訊藥品 = (COMPAR2 == "Y") ? "True" : "False",
                    管制級別 = MLEVEL,
                    中西藥 = "西藥"
                };
                medClasses.Add(medClass);

            }

            

            if (medClasses.Count == 0) return "[]";
            medClass.add_med_clouds(API_Server, medClasses);
            string jsonString = medClasses.JsonSerializationt();
            return jsonString;
        }
        [HttpGet("update_medCloud")]
        public string update_medCloud()
        {
            returnData returnData = new returnData();
            returnData.Method = "update_medCloud";
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {
                List<string> codes = ExcuteExcel();
                for (int i = 0; i < codes.Count; i++)
                {
                    string code = codes[i];
                    Get(code);
                }
                returnData.Code = 200;
                returnData.Result = $"取得藥品資料共{codes.Count}筆";
                returnData.TimeTaken = $"{myTimerBasic}";
                //returnData.Data = medClasses;
                return returnData.JsonSerializationt(true);
            }
            catch(Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt(true);
            }
            

        }
        private List<string> ExcuteExcel()
        {
            string folderPath = @"C:\med_cloud";
            string searchPattern = "UD藥物品項.xlsx";
            string[] files = Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
            string NewFile = files[0];
            string filePath = Path.Combine(folderPath, NewFile);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            List<string> codes = new List<string>();
            

            using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // 循環處理每個工作表
                    do
                    {
                        reader.Read();
                        reader.Read();
                        reader.Read();
                        while (reader.Read()) // 每次讀取一列
                        {
                            if (reader.FieldCount >= 3) // 確保至少有三格資料
                            {
                                string code = reader.GetValue(2)?.ToString()?.Trim(); // 第三格的值
                                codes.Add(code ?? string.Empty); // 加入列表
                            }
                        }
                    } while (reader.NextResult()); // 切換到下一個工作表（如果有）
                }
            }

            //List<string> codes = new List<string>();
            //for (int i = 0; i < list_UD.Count(); i++)
            //{
            //    string code = list_UD[i][2].ObjectToString();
            //    codes.Add(code);
            //}
            return codes;
        }
        //public enum enum_excel
        //{
        //    Source.Name,
        //    Name,
        //    Data.Column1,
        //}
    }


}
