using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Data.SqlClient;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Configuration;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Reports
{
    public class ReportsViewModel : CommonViewModel
    {
        private string kpiXmlFilePath = string.Empty;

        public DataTable KpiDt { get; set; }
        public DataTable ItDt { get; set; }

        public decimal TonergiantGrossProfitBefore { get; set; }
        public decimal TonergiantGrossProfit { get; set; }
        public decimal TonergiantGrossProfitPercentageBefore { get; set; }
        public decimal TonergiantGrossProfitPercentage { get; set; }
        public decimal TonergiantOrders { get; set; }
        public decimal TonergiantSales { get; set; }
        public decimal TonergiantVouchers { get; set; }
        public decimal TonergiantAverageOrderValue { get; set; }

        public decimal CartridgeMonkeyGrossProfitBefore { get; set; }
        public decimal CartridgeMonkeyGrossProfit { get; set; }
        public decimal CartridgeMonkeyGrossProfitPercentageBefore { get; set; }
        public decimal CartridgeMonkeyGrossProfitPercentage { get; set; }
        public decimal CartridgeMonkeyOrders { get; set; }
        public decimal CartridgeMonkeySales { get; set; }
        public decimal CartridgeMonkeyVouchers { get; set; }
        public decimal CartridgeMonkeyAverageOrderValue { get; set; }

        public decimal TotalGrossProfitBefore { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public decimal TotalGrossProfitPercentageBefore { get; set; }
        public decimal TotalGrossProfitPercentage { get; set; }
        public decimal TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalVouchers { get; set; }
        public decimal TotalAverageOrderValue { get; set; }

        public FileInfo[] LogFiles { get; set; }
        public string[] LogContent { get; set; }

        public IQueryable<Telerik> LogList { get; set; }






        public class itTable
        {
            public string rowname { get; set; }
            public string hour { get; set; }
            public string day { get; set; }
            public string week { get; set; }
            public string month { get; set; }
        }

        public List<itTable> itTableList { get; set; }

        public ReportsViewModel GetKpiData(int websiteId)
        {
            bool force = websiteId == 1;
            KpiDt = DataCache.GetKpiData(force);
            ReadData();

            ItDt = DataCache.GetItData(force);
            List<itTable> x = new List<itTable>();
            foreach(DataRow xx in ItDt.Rows)
            {
                itTable xxx = new itTable();
                xxx.rowname = Convert.ToString(xx["Description"]);
                xxx.hour = Convert.ToString(xx["Hour"]);
                xxx.day = Convert.ToString(xx["Day"]);
                xxx.week = Convert.ToString(xx["Week"]);
                xxx.month = Convert.ToString(xx["Month"]);

                x.Add(xxx);
            }

            /*
            0-ALERTBATCH
            1-ALERTWEB
            2-AMAZON
            3-BATCH (errors)
            4-ECOM (errors)
            5-PAYPAL
            6-SAGEPAY
            7-WebError
            8-BatchError
            */
            
            itTable webError = new itTable();
            webError.rowname = "WebError";
            webError.hour = Convert.ToInt32(x.Find(y => y.rowname == "ECOM").hour) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTWEB").hour)
                ? "" : "background-color:red;";
            webError.day = Convert.ToInt32(x.Find(y => y.rowname == "ECOM").day) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTWEB").day)
                ? "" : "background-color:red;";
            webError.week = Convert.ToInt32(x.Find(y => y.rowname == "ECOM").week) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTWEB").week)
                ? "" : "background-color:red;";
            webError.month = Convert.ToInt32(x.Find(y => y.rowname == "ECOM").month) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTWEB").month)
                ? "" : "background-color:red;";
            x.Add(webError);

            itTable batchError = new itTable();
            batchError.rowname = "BatchError";
            batchError.hour = Convert.ToInt32(x.Find(y => y.rowname == "BATCH").hour) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTBATCH").hour)
                ? "" : "background-color:red;";
            batchError.day = Convert.ToInt32(x.Find(y => y.rowname == "BATCH").day) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTBATCH").day)
                ? "" : "background-color:red;";
            batchError.week = Convert.ToInt32(x.Find(y => y.rowname == "BATCH").week) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTBATCH").week)
                ? "" : "background-color:red;";
            batchError.month = Convert.ToInt32(x.Find(y => y.rowname == "BATCH").month) < Convert.ToInt32(x.Find(y => y.rowname == "ALERTBATCH").month)
                ? "" : "background-color:red;";
            x.Add(batchError);

            itTableList = x;

            return this;
        }         

        private void ReadData()
        {
            try
            {
                foreach (DataRow dr in KpiDt.Rows)
                {
                    decimal orders = Convert.ToDecimal(dr["orders"].ToString());
                    decimal sales = Convert.ToDecimal(dr["sales"].ToString());
                    decimal cost = Convert.ToDecimal(dr["cost"].ToString());
                    decimal vouchers = Convert.ToDecimal(dr["vouchers"].ToString());

                    decimal dGrossProfitBefore = Math.Round((sales + vouchers - cost), 2);
                    decimal grossProfitBefore = dGrossProfitBefore > 0 ? dGrossProfitBefore : 0;
                    decimal dGrossProfit = Math.Round((sales - cost), 2);
                    decimal grossProfit = dGrossProfit > 0 ? dGrossProfit : 0;
                    decimal grossProfitPercentageBefore = (Math.Round((dGrossProfitBefore / sales) *100, 2));
                    decimal grossProfitPercentage = (Math.Round((dGrossProfit / sales) * 100, 2));
                    decimal averageOrderValue = orders > 0 ? sales / orders : 0m;

                    switch (int.Parse(dr["website"].ToString()))
                    {
                        case 1:
                        {
                            TonergiantOrders = orders;
                            TonergiantSales = sales;
                            TonergiantVouchers = vouchers;
                            TonergiantGrossProfitBefore = grossProfitBefore;
                            TonergiantGrossProfit = grossProfit;
                            TonergiantGrossProfitPercentageBefore = grossProfitPercentageBefore;
                            TonergiantGrossProfitPercentage = grossProfitPercentage;
                            TonergiantAverageOrderValue = averageOrderValue;
                            break;
                        }
                        case 2:
                        {
                            CartridgeMonkeyOrders = orders;
                            CartridgeMonkeySales = sales;
                            CartridgeMonkeyVouchers = vouchers;
                            CartridgeMonkeyGrossProfitBefore = grossProfitBefore;
                            CartridgeMonkeyGrossProfit = grossProfit;
                            CartridgeMonkeyGrossProfitPercentageBefore = grossProfitPercentageBefore;
                            CartridgeMonkeyGrossProfitPercentage = grossProfitPercentage;
                            CartridgeMonkeyAverageOrderValue = averageOrderValue;
                            break;
                        }
                    }
                }

                TotalGrossProfitBefore = GrossProfitTotalBefore();
                TotalGrossProfit = GrossProfitTotal();
                TotalRevenue = RevenueTotal();
                TotalGrossProfitPercentageBefore = GrossProfitPercentTotalBefore();
                TotalGrossProfitPercentage = GrossProfitPercentTotal();
                TotalOrders = NumberOfOrdersTotal();
                TotalVouchers = NumberOfVouchersTotal();
                TotalAverageOrderValue = TotalOrders > 0 ? TotalRevenue / TotalOrders : 0m;

            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message, e.InnerException);
            }
        }

        private decimal GrossProfitTotalBefore()
        {
            return Math.Round(TonergiantGrossProfitBefore + CartridgeMonkeyGrossProfitBefore, 2);
        }

        private decimal GrossProfitTotal()
        {
            return Math.Round(TonergiantGrossProfit + CartridgeMonkeyGrossProfit, 2);
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
            return TonergiantOrders + CartridgeMonkeyOrders;
        }

        private decimal NumberOfVouchersTotal()
        {
            return TonergiantVouchers + CartridgeMonkeyVouchers;
        }

        private decimal RevenueTotal()
        {
            return Math.Round(TonergiantSales + CartridgeMonkeySales, 2);
        }

        public List<configurationSetting> GetWallBoardTargets()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.configurationSetting.Where(x => x.sectionName == "WallBoard").ToList();
            }
        }

        public static string SetWallBoardTargets(string[] optionsArray)
        {
            string success = "Successfully Updated Settings";

            string TargetName = "";

            TargetName = "baseTargetMonday";
            success = SaveTarget(TargetName, optionsArray[1]) != true ? "Base Target Monday Failed" : success;
            TargetName = "baseTargetTuesday";
            success = SaveTarget(TargetName, optionsArray[3]) != true ? "Base Target Tuesday Failed" : success;
            TargetName = "baseTargetWednesday";
            success = SaveTarget(TargetName, optionsArray[5]) != true ? "Base Target Wednesday Failed" : success;
            TargetName = "baseTargetThursday";
            success = SaveTarget(TargetName, optionsArray[7]) != true ? "Base Target Thursday Failed" : success;
            TargetName = "baseTargetFriday";
            success = SaveTarget(TargetName, optionsArray[9]) != true ? "Base Target Fridayday Failed" : success;

            TargetName = "stretchTargetMonday";
            success = SaveTarget(TargetName, optionsArray[0]) != true ? "stretch Target Monday Failed" : success;
            TargetName = "stretchTargetTuesday";
            success = SaveTarget(TargetName, optionsArray[2]) != true ? "stretch Target Tuesday Failed" : success;
            TargetName = "stretchTargetWednesday";
            success = SaveTarget(TargetName, optionsArray[4]) != true ? "stretch Target Wednesday Failed" : success;
            TargetName = "stretchTargetThursday";
            success = SaveTarget(TargetName, optionsArray[6]) != true ? "stretch Target Thursday Failed" : success;
            TargetName = "stretchTargetFriday";
            success = SaveTarget(TargetName, optionsArray[8]) != true ? "stretch Target Fridayday Failed" : success;

            TargetName = "targetsAreRevenue";
            success = SaveTarget(TargetName, optionsArray[10]) != true ? "Targets Are Revenue Failed" : success;

            return success;
        }

        private static bool SaveTarget(string TargetName, string TargetValue)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var thisTarget = db.configurationSetting.Where(x => x.sectionName == "WallBoard" && x.settingName == TargetName).FirstOrDefault();

                if (thisTarget != null)
                {
                    thisTarget.settingValue = TargetValue;
                    db.Entry(thisTarget).State = EntityState.Modified;
                    db.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public ReportsViewModel ReadLog(string logName)
        {
            var path = SharedFunctions.GetConfigurationSetting("BatchProgram", "LogFilePath");
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
            {
                path = "C:\\Program Files\\Netgiant\\BatchProcesses\\Logs\\";
            }
            LogContent = File.ReadAllLines(path + logName);

            return this;
        }

        public ReportsViewModel GetLogs()
        {
            var path = SharedFunctions.GetConfigurationSetting("BatchProgram", "LogFilePath");
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
            {
                path = "C:\\Program Files\\Netgiant\\BatchProcesses\\Logs";
            }
            DirectoryInfo dir = new DirectoryInfo(path);
            LogList = dir.GetFiles()
                .OrderByDescending(x => x.CreationTime)
                .AsQueryable()
                .AsTelerikViewModel();
            return this;
        }

        public class Telerik
        {
            public string Filename { get; set; }
            public string ShortFilename { get; set; }
            public DateTime DateCreated { get; set; }
        }
    }

    public static class ReportsModeExtensions
    {
        public static IQueryable<ReportsViewModel.Telerik> AsTelerikViewModel(this IQueryable<FileInfo> logQuery)
        {
            return logQuery.Select(o => new ReportsViewModel.Telerik
            {
                Filename = o.Name,
                ShortFilename = o.Name.Contains("_") ? o.Name.Split('_')[1] : o.Name,
                DateCreated = o.CreationTime
            }); ;
        }
    }
}
