using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data.Entity;
using System.Diagnostics;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Reports
{
    public class ReportsViewModel
    {
        private string kpiXmlFilePath = string.Empty;

        public decimal TonergiantGrossProfitBefore { get; set; }
        public decimal TonergiantGrossProfit { get; set; }
        public decimal TonergiantGrossProfitPercentageBefore { get; set; }
        public decimal TonergiantGrossProfitPercentage { get; set; }
        public decimal TonergiantOrders { get; set; }
        public decimal TonergiantSales { get; set; }
        public decimal TonergiantVouchers { get; set; }

        public decimal CartridgeMonkeyGrossProfitBefore { get; set; }
        public decimal CartridgeMonkeyGrossProfit { get; set; }
        public decimal CartridgeMonkeyGrossProfitPercentageBefore { get; set; }
        public decimal CartridgeMonkeyGrossProfitPercentage { get; set; }
        public decimal CartridgeMonkeyOrders { get; set; }
        public decimal CartridgeMonkeySales { get; set; }
        public decimal CartridgeMonkeyVouchers { get; set; }

        public decimal NetgiantGrossProfitBefore { get; set; }
        public decimal NetgiantGrossProfit { get; set; }
        public decimal NetgiantGrossProfitPercentageBefore { get; set; }
        public decimal NetgiantGrossProfitPercentage { get; set; }
        public decimal NetgiantOrders { get; set; }
        public decimal NetgiantSales { get; set; }
        public decimal NetgiantVouchers { get; set; }

        public decimal TotalGrossProfitBefore { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public decimal TotalGrossProfitPercentageBefore { get; set; }
        public decimal TotalGrossProfitPercentage { get; set; }
        public decimal TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalVouchers { get; set; }

        public FileInfo[] LogFiles { get; set; }
        public string[] LogContent { get; set; }

        public ReportsViewModel GetKpiData()
        {
            SetXMLFilePath();
            ReadXML();

            return this;
        }

        private void SetXMLFilePath()
        {
            bool isLocal = Debugger.IsAttached;

            if (!isLocal)
            {
                kpiXmlFilePath = SharedFunctions.GetConfigurationSetting("Reports", "KpiFilePath");
            }
            else
            {
                kpiXmlFilePath = @"C:\Temp\KPI\KPI.xml";
            }
        }

        private void ReadXML()
        {
            try
            {
                XDocument xml = XDocument.Load(kpiXmlFilePath);
                
                var tgSite = xml.Descendants("site").Where(x => x.Attribute("id").Value == "1").FirstOrDefault();
                var cmSite = xml.Descendants("site").Where(x => x.Attribute("id").Value == "2").FirstOrDefault();
                var ngSite = xml.Descendants("site").Where(x => x.Attribute("id").Value == "3").FirstOrDefault();

                TonergiantOrders = Convert.ToDecimal(tgSite.Descendants("orders").FirstOrDefault().Value);
                TonergiantSales = Convert.ToDecimal(tgSite.Descendants("sales").FirstOrDefault().Value);
                TonergiantVouchers = Convert.ToDecimal(tgSite.Descendants("vouchers").FirstOrDefault().Value);
                TonergiantGrossProfitBefore = Convert.ToDecimal(tgSite.Descendants("grossprofitbefore").FirstOrDefault().Value);
                TonergiantGrossProfit = Convert.ToDecimal(tgSite.Descendants("grossprofit").FirstOrDefault().Value);
                TonergiantGrossProfitPercentageBefore = Convert.ToDecimal(tgSite.Descendants("grossprofitpercentbefore").FirstOrDefault().Value);
                TonergiantGrossProfitPercentage = Convert.ToDecimal(tgSite.Descendants("grossprofitpercent").FirstOrDefault().Value);

                CartridgeMonkeyOrders = Convert.ToDecimal(cmSite.Descendants("orders").FirstOrDefault().Value);
                CartridgeMonkeySales = Convert.ToDecimal(cmSite.Descendants("sales").FirstOrDefault().Value);
                CartridgeMonkeyVouchers = Convert.ToDecimal(cmSite.Descendants("vouchers").FirstOrDefault().Value);
                CartridgeMonkeyGrossProfitBefore = Convert.ToDecimal(cmSite.Descendants("grossprofitbefore").FirstOrDefault().Value);
                CartridgeMonkeyGrossProfit = Convert.ToDecimal(cmSite.Descendants("grossprofit").FirstOrDefault().Value);
                CartridgeMonkeyGrossProfitPercentageBefore = Convert.ToDecimal(cmSite.Descendants("grossprofitpercentbefore").FirstOrDefault().Value);
                CartridgeMonkeyGrossProfitPercentage = Convert.ToDecimal(cmSite.Descendants("grossprofitpercent").FirstOrDefault().Value);

                NetgiantOrders = Convert.ToDecimal(ngSite.Descendants("orders").FirstOrDefault().Value);
                NetgiantSales = Convert.ToDecimal(ngSite.Descendants("sales").FirstOrDefault().Value);
                NetgiantVouchers = Convert.ToDecimal(ngSite.Descendants("vouchers").FirstOrDefault().Value);
                NetgiantGrossProfitBefore = Convert.ToDecimal(ngSite.Descendants("grossprofitbefore").FirstOrDefault().Value);
                NetgiantGrossProfit = Convert.ToDecimal(ngSite.Descendants("grossprofit").FirstOrDefault().Value);
                NetgiantGrossProfitPercentageBefore = Convert.ToDecimal(ngSite.Descendants("grossprofitpercentbefore").FirstOrDefault().Value);
                NetgiantGrossProfitPercentage = Convert.ToDecimal(ngSite.Descendants("grossprofitpercent").FirstOrDefault().Value);

                TotalGrossProfitBefore = GrossProfitTotalBefore();
                TotalGrossProfit = GrossProfitTotal();
                TotalRevenue = RevenueTotal();
                TotalGrossProfitPercentageBefore = GrossProfitPercentTotalBefore();
                TotalGrossProfitPercentage = GrossProfitPercentTotal();
                TotalOrders = NumberOfOrdersTotal();
                TotalVouchers = NumberOfVouchersTotal();

            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException("XML File Not Found", e.InnerException);
            }
            catch (FormatException e)
            {
                throw new ApplicationException("Invalid Value found in XML", e.InnerException);
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message, e.InnerException);
            }
        }

        private decimal GrossProfitTotalBefore()
        {
            return Math.Round(TonergiantGrossProfitBefore + CartridgeMonkeyGrossProfitBefore + NetgiantGrossProfitBefore, 2);
        }

        private decimal GrossProfitTotal()
        {
            return Math.Round(TonergiantGrossProfit + CartridgeMonkeyGrossProfit + NetgiantGrossProfit, 2);
        }

        private decimal GrossProfitPercentTotalBefore()
        {
            decimal returnValue = 0;

            if (TotalRevenue > 0)
            {
                returnValue = Math.Round(TotalGrossProfitBefore / TotalRevenue * 100, 2);
            }

            return returnValue;
        }

        private decimal GrossProfitPercentTotal()
        {
            decimal returnValue = 0;

            if (TotalRevenue > 0)
            {
                returnValue = Math.Round(TotalGrossProfit / TotalRevenue * 100, 2);
            }

            return returnValue;
        }

        private decimal NumberOfOrdersTotal()
        {
            return TonergiantOrders + CartridgeMonkeyOrders + NetgiantOrders;
        }

        private decimal NumberOfVouchersTotal()
        {
            return TonergiantVouchers + CartridgeMonkeyVouchers + NetgiantVouchers;
        }

        private decimal RevenueTotal()
        {
            return Math.Round(TonergiantSales + CartridgeMonkeySales + NetgiantSales, 2);
        }

        public List<configurationSetting> GetWallBoardTargets()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.configurationSetting.Where(x => x.sectionName == "WallBoard").ToList();
            }
        }

        public static string SetWallBoardTargets(int stretchTargetValue, int baseTargetValue)
        {
            string success = "Successfully Updated Settings";

            using (ngmdEntities db = new ngmdEntities())
            {
                var stretchRecord = db.configurationSetting.Where(x => x.sectionName == "WallBoard" && x.settingName == "stretchTarget").FirstOrDefault();
                var baseRecord = db.configurationSetting.Where(x => x.sectionName == "WallBoard" && x.settingName == "baseTarget").FirstOrDefault();

                if (stretchRecord != null)
                {
                    stretchRecord.settingValue = stretchTargetValue.ToString();
                    db.Entry(stretchRecord).State = EntityState.Modified;
                }
                else
                {
                    success = "Error Saving Stretch Setting. Could Not Find Setting.";
                }

                if (baseRecord != null)
                {
                    baseRecord.settingValue = baseTargetValue.ToString();
                    db.Entry(baseRecord).State = EntityState.Modified;
                }
                else
                {
                    success = "Error Saving Base Setting. Could Not Find Setting.";
                }

                db.SaveChanges();
            }

            return success;
        }

        public ReportsViewModel ListBatchLogs()
        {
            var path = SharedFunctions.GetConfigurationSetting("BatchProgram", "LogFilePath");
            DirectoryInfo dir = new DirectoryInfo(path);
            LogFiles = dir.GetFiles().OrderByDescending(x => x.CreationTime).ToArray();
            
            return this;
        }

        public ReportsViewModel ReadLog(string logName)
        {
            var path = SharedFunctions.GetConfigurationSetting("BatchProgram", "LogFilePath");
            LogContent = File.ReadAllLines(path + logName);

            return this;
        }
    }
}
