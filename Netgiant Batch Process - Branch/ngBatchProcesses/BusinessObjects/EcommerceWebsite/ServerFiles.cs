using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class ServerFiles
    {
        static bool errorHasOccurred = false;

        public static void ClearServerFiles(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["input"] + " Process Started");

            var deletedCount = Directory.GetFiles(Convert.ToString(parms["input"]))
                 .Select(f => new FileInfo(f))
                 .Where(f => f.CreationTime < DateTime.Now.AddMonths(-Convert.ToInt32(parms["output"])))
                 .ToList().Count();

            try
            {
                Directory.GetFiles(Convert.ToString(parms["input"]))
                 .Select(f => new FileInfo(f))
                 .Where(f => f.CreationTime < DateTime.Now.AddMonths(-Convert.ToInt32(parms["output"])))
                 .ToList()
                 .ForEach(f => f.Delete());
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Occured clearing server files" + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Message**: " + ex.Message + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Stack Trace**: " + ex.StackTrace + Environment.NewLine);
                errorHasOccurred = true;
            }

            stnFunc.AddToActivityLog("Finished Batch Program with switch: clearserverfiles. Server Files Cleared: " + deletedCount + Environment.NewLine);
            string acitivityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && Properties.Settings.Default.Environment == "Live")
                stnFunc.SendSimpleEmail("clearserverfiles", acitivityLogFileName);
        }
    }
}
