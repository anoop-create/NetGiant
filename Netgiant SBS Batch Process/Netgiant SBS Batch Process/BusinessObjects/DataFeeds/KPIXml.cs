using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGSBP.DataAccessLayer.DataUtilities;
using System.Data;
using System.Xml.Linq;
using NGSBP.DataAccessLayer.SCOM.Services;
using NGSBP.DataAccessLayer.SCOM.SImpleEntities;

namespace ngSBSBatchProcesses.BusinessObjects.DataFeeds
{
    public class KPIXml
    {
        public static void CreateXML(Dictionary<string, string> parms)
        {
            List<KPISE> results = KPIServices.GetKPIData();
            XDocument xml = BuildXMLFramework();
            BuildXMlSites(xml, results);
            SaveXML(xml, parms);
            FTPXML(parms);
        }

        private static XDocument BuildXMLFramework()
        {
            XDocument xml = new XDocument();
            xml.Add(new XElement("kpi"));

            return xml;
        }

        private static void BuildXMlSites(XDocument xml, List<KPISE> kpiList)
        {
            XElement kpiNode = xml.Descendants("kpi").FirstOrDefault();

            for (int i = 1; i <= 3; i++)
            {
                KPISE websiteRecord = kpiList.Where(x => x.WebsiteID == i.ToString()).FirstOrDefault();
                string orders = websiteRecord != null ? websiteRecord.Orders : "0";
                string sales = websiteRecord != null ? websiteRecord.Sales : "0";
                string vouchers = websiteRecord != null ? websiteRecord.Vouchers : "0";
                string grossProfitBefore = "0";
                string grossProfitPercentBefore = "0";
                string grossProfit = "0";
                string grossProfitPercent = "0";                

                if (websiteRecord != null)
                {
                    decimal dGrossProfitBefore = Math.Round(Convert.ToDecimal(websiteRecord.Sales) + Convert.ToDecimal(websiteRecord.Vouchers) - Convert.ToDecimal(websiteRecord.Cost), 2);
                    grossProfitBefore = dGrossProfitBefore > 0 ? dGrossProfitBefore.ToString() : "0";
                    grossProfitPercentBefore = (Math.Round((dGrossProfitBefore / Convert.ToDecimal(websiteRecord.Sales)) * 100, 2)).ToString();
                    decimal dGrossProfit = Math.Round(Convert.ToDecimal(websiteRecord.Sales) - Convert.ToDecimal(websiteRecord.Cost), 2);
                    grossProfit = dGrossProfit > 0 ? dGrossProfit.ToString() : "0";
                    grossProfitPercent = (Math.Round((dGrossProfit / Convert.ToDecimal(websiteRecord.Sales)) * 100, 2)).ToString();
                }

                XElement siteNode = new XElement("site");
                siteNode.Add(new XAttribute("id", i.ToString()),
                    new XElement("orders", orders),
                    new XElement("sales", sales),
                    new XElement("vouchers", vouchers),
                    new XElement("grossprofitbefore", grossProfitBefore),
                    new XElement("grossprofit", grossProfit),
                    new XElement("grossprofitpercentbefore", grossProfitPercentBefore),
                    new XElement("grossprofitpercent", grossProfitPercent));

                kpiNode.Add(siteNode);
            }

        }

        private static void SaveXML(XDocument xml, Dictionary<string, string> parms)
        {
            xml.Save(parms["output"]);
        }

        private static void FTPXML(Dictionary<string, string> parms)
        {
            FtpUtilities.UploadFTPFile(parms["output"],
                parms["ftpsite"] + "/" + "KPI.xml",
                parms["ftpusername"],
                parms["ftppassword"], 
                false); 
        }
    }
}
