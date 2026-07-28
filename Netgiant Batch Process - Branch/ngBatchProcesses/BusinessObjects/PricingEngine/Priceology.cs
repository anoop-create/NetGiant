using System;
using System.Collections.Generic;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.PricingEngine
{
    public class Priceology
    {
        public static void InsertPrices(Dictionary<string, string> parms)
        {
            var stnFunc = new StandardFunctions();
            var settings = Properties.Settings.Default;
            var errorHasOccurred = false;

            try
            {
                stnFunc.AddToActivityLog(parms["type"] + " Process Started");

                var folders = new []{ "NG", "TG", "CM" };

                foreach (var folder in folders)
                {
                    var sqlParams = new List<KeyValuePair<string, string>>();
                    sqlParams.Add(new KeyValuePair<string, string>("priceFile", @"D:\PMSPrices\" + folder + @"\Priceology_Output.txt"));
                    SQLUtilities.ExecuteStoredProcedureQuery("netgiantMasterData", "ngmd.InsertPriceologyPrices", sqlParams);
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Occured executing stored procedure ngmd.InsertPriceologyPrices");
                stnFunc.ProcessException(ex);
                errorHasOccurred = true;
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }
        }
    }
}
