using System;
using System.Collections.Generic;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.PricingEngine
{
    public class Priceology
    {
        public static void InsertPrices(Dictionary<string, string> parms)
        {
            var settings = Properties.Settings.Default;

            try
            {
                StandardFunctions.WriteProcessStarted();

                var folders = new[] { "TG", "CM" };

                foreach (var folder in folders)
                {
                    var sqlParams = new List<KeyValuePair<string, string>>();
                    sqlParams.Add(new KeyValuePair<string, string>("priceFile", @"D:\PMSPrices\" + folder + @"\Priceology_Output.txt"));
                    SQLUtilities.ExecuteStoredProcedureQuery("netgiantMasterData", "ngmd.InsertPriceologyPrices", sqlParams);
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Occured executing stored procedure ngmd.InsertPriceologyPrices", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
