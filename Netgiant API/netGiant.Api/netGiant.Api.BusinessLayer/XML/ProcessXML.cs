using netGiant.Api.BusinessLayer.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace netGiant.Api.BusinessLayer.XML
{
    public class ProcessXML
    {
        public ProcessXML(string _groupNo, int _websiteFK, string _sitesPath)
        {
            axisGroupNo = _groupNo;
            websiteFK = _websiteFK;
            webURL = GetWebURL(_websiteFK);
            sitesPath = _sitesPath;
        }

        string axisGroupNo;
        int websiteFK;
        string webURL;
        bool isLive;
        string sitesPath;

        public XDocument GetUpdatedXML()
        {
            DataTable productData = GetProductData();
            XDocument xml = LoadXML();
            List<XElement> prods = xml.Descendants("pt").ToList();

            foreach (var prd in prods)
            {
                var stkRefElem = prd.Descendants()
                    .Where(x => (string)x.Attribute("i") == "productreference")
                    .FirstOrDefault();

                var stockReference = stkRefElem != null ? stkRefElem.Value : "";

                if (stockReference != "")
                {
                    var productEntry = productData.AsEnumerable()
                        .Where(x => x.Field<string>("axisRef") == stockReference)
                            .FirstOrDefault();

                    if (productEntry != null)
                    {
                        UpdateXMLProduct(prd, productEntry);
                    }
                }
            }

            return xml;
        }

        private void UpdateXMLProduct(XElement prd, DataRow productEntry)
        {
            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "retailpriceex")
                .First().SetValue(productEntry.Field<double>("retailPriceExVat"));

            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "tradepriceex")
                .First().SetValue(productEntry.Field<double>("tradePriceExVat"));

            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "retailpriceinc")
                .First().SetValue(productEntry.Field<decimal>("retailPriceIncVat"));

            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "tradepriceinc")
                .First().SetValue(productEntry.Field<decimal>("tradePriceIncVat"));

            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "sohid")
                .First().SetValue(productEntry.Field<int>("availability"));

            prd.Descendants()
                .Where(x => (string)x.Attribute("i") == "sohtext")
                .First().SetValue(SetStockText(productEntry.Field<int>("availability")));
        }

        private XDocument LoadXML()
        {
            if (!Debugger.IsAttached)
            {
                return XDocument.Load(sitesPath + "\\" + webURL + 
                    "\\data\\ProductGroups\\Current\\" + axisGroupNo + ".xml");
            }
            else
            {
                return XDocument.Load(@"C:\Temp\GRID\" + axisGroupNo + ".xml");
            }
        }

        private string SetStockText(int a)
        {
            string stkMessage = "";

            switch (a)
            {
                case 1:
                    stkMessage = "In Stock";
                    break;
                case 2:
                    stkMessage = "Availability 2-3 Days";
                    break;
                case 4:
                    stkMessage = "Availability 2-3 Days";
                    break;
                case 5:
                    stkMessage = "In Stock";
                    break;
                default:
                    stkMessage = "Back In Stock Soon";
                    break;
            }

            return stkMessage;
        }

        private DataTable GetProductData()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                DataTable dt = new DataTable();

                List<KeyValuePair<string, string>> parms = new List<KeyValuePair<string, string>>();
                parms.Add(new KeyValuePair<string, string>("groupNo", axisGroupNo));
                parms.Add(new KeyValuePair<string, string>("language", GetLanguage().ToString()));

                dt = StandardFunctions.ExecuteStoredProcedureQuery("netgiantMasterData", "ngmd.GetProdGridData", parms);

                return dt;
            }
        }

        private string GetWebURL(int id)
        {
            var url = "";

            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    url = db.Websites.Find(id).WebURL;
                }
            }

            isLive = url.Contains("beta") ? false : true;

            return url;
        }

        private int GetLanguage()
        {
            int language = 1;

            switch (websiteFK)
            {
                case 1:
                    language = 1;
                    break;
                case 2:
                    language = 2;
                    break;
                case 3:
                    language = 5;
                    break;
                default:
                    break;
            }

            return language;
        }
    }
}
