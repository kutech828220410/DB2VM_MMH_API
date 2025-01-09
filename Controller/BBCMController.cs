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
            if (Code.StringIsEmpty()) return "[]";
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
            //string[] Node_array = new string[] { "soap:Body", "Drug_DATAResponse", "Drug_DATAResult", "diffgr:diffgram", "NewDataSet", "Temp1"};
            string[] Node_array = new string[] { "soap:Body", "Drug_DATAResponse", "Drug_DATAResult" };


            XmlElement xmlElement = Xml.Xml_GetElement(Node_array);
            string MCODE = xmlElement.Xml_GetInnerXml("MCODE");
            string FullName = xmlElement.Xml_GetInnerXml("FULLNAME");
            string ShortName = xmlElement.Xml_GetInnerXml("SHORTNAME");
            string COMPAR2 = xmlElement.Xml_GetInnerXml("COMPAR2");
            string MLEVEL = xmlElement.Xml_GetInnerXml("MLEVEL");
            if (MCODE.StringIsEmpty()) return "[]";
            List<medClass> medClasses = new List<medClass>();
            medClass medClass = new medClass();
            medClass.藥品碼 = MCODE;
            medClass.藥品名稱 = FullName;
            medClass.藥品學名 = ShortName;
            medClass.警訊藥品 = (COMPAR2 == "Y") ? "True" : "False";
            medClass.管制級別 = MLEVEL;

            medClasses.Add(medClass);

            //List<object[]> list_藥檔資料 = sQLControl_藥檔資料.GetRowsByDefult(null, (int)enum_雲端藥檔.藥品碼, MCODE);
            //if(list_藥檔資料.Count == 0)
            //{
            //    object[] value = new object[new enum_雲端藥檔().GetLength()];
            //    value[(int)enum_雲端藥檔.GUID] = Guid.NewGuid().ToString();
            //    value[(int)enum_雲端藥檔.藥品碼] = medClass.藥品碼;
            //    value[(int)enum_雲端藥檔.藥品名稱] = medClass.藥品名稱;
            //    value[(int)enum_雲端藥檔.藥品學名] = medClass.藥品學名;
            //    value[(int)enum_雲端藥檔.警訊藥品] = medClass.警訊藥品;
            //    value[(int)enum_雲端藥檔.管制級別] = medClass.管制級別;
            //    sQLControl_藥檔資料.AddRow(null, value);
            //}
            //else
            //{
            //    object[] value = list_藥檔資料[0];
            //    value[(int)enum_雲端藥檔.藥品碼] = medClass.藥品碼;
            //    value[(int)enum_雲端藥檔.藥品名稱] = medClass.藥品名稱;
            //    value[(int)enum_雲端藥檔.藥品學名] = medClass.藥品學名;
            //    value[(int)enum_雲端藥檔.警訊藥品] = medClass.警訊藥品;
            //    value[(int)enum_雲端藥檔.管制級別] = medClass.管制級別;
            //    List<object[]> list = new List<object[]>();
            //    list.Add(value);
            //    sQLControl_藥檔資料.UpdateByDefulteExtra(null, list);
            //}
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
