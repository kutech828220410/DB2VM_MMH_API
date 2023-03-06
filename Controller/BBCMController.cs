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
using System.Xml;
namespace DB2VM.Controller
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCMController : ControllerBase
    {
       
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
            string[] Node_array = new string[] { "soap:Body", "Drug_DATAResponse", "Drug_DATAResult", "diffgr:diffgram", "NewDataSet", "Temp1"};

            XmlElement xmlElement = Xml.Xml_GetElement(Node_array);
            string MCODE = xmlElement.Xml_GetInnerXml("MCODE");
            string FullName = xmlElement.Xml_GetInnerXml("FULLNAME");
            string ShortName = xmlElement.Xml_GetInnerXml("SHORTNAME");
            string COMPAR2 = xmlElement.Xml_GetInnerXml("COMPAR2");
            string MLEVEL = xmlElement.Xml_GetInnerXml("MLEVEL");
            if (MCODE.StringIsEmpty()) return "[]";
            List<MedClass> medClasses = new List<MedClass>();
            MedClass medClass = new MedClass();
            medClass.藥品碼 = MCODE;
            medClass.藥品名稱 = FullName;
            medClass.藥品學名 = ShortName;
            medClass.警訊藥品 = (COMPAR2 == "Y") ? "True" : "False";
            medClass.管制級別 = MLEVEL;

            medClasses.Add(medClass);
            //while (reader.Read())
            //{
            //    MedClass medClass = new MedClass();
            //    medClass.藥品碼 = reader["UDDRGNO"].ToString().Trim();
            //    medClass.藥品名稱 = reader["UDARNAME"].ToString().Trim();
            //    medClass.料號 = reader["UDSTOKNO"].ToString().Trim();
            //    medClass.ATC主碼 = reader["UDATC"].ToString().Trim();
            //    medClass.藥品條碼1 = reader["UDBARCD1"].ToString().Trim();
            //    medClass.藥品條碼2 = reader["UDBARCD2"].ToString().Trim();
            //    medClass.藥品條碼3 = reader["UDBARCD3"].ToString().Trim();
            //    medClass.藥品條碼4 = reader["UDBARCD4"].ToString().Trim();
            //    medClass.藥品條碼5 = reader["UDBARCD5"].ToString().Trim();


            //    medClasses.Add(medClass);
            //}

            if (medClasses.Count == 0) return "[]";
            string jsonString = medClasses.JsonSerializationt();
            return jsonString;
        }
    }
}
