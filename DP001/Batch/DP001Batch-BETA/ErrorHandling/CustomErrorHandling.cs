using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP001BusinessLogic.Shared;
using System.Diagnostics;
using DP001DataAccess.Utilities;
using DP001Batch.BusinessObjects.Shared;

namespace DP001Batch
{
    public class CustomErrorHandling
    {
        public void LogError(Exception ex, Dictionary<string, string> parms)
        {
            if (Debugger.IsAttached)
                return;

            string message = "";
            message += "ERROR: " + Environment.NewLine;
            message += "Environment: " + CommonFunctions.GetMachineAppSetting("Environment") + Environment.NewLine;
            message += "Environment: " + CommonFunctions.GetMachineAppSetting("Environment") + Environment.NewLine;
            message += "Event Time: " + CommonDataFunctions.GetCurrentDateTime() + Environment.NewLine;
            message += "Switch Arguments: " + String.Join(" ", parms) + Environment.NewLine;
            message += "Exception Message: " + ex.Message + Environment.NewLine;
            message += "Exception Type: " + ex.GetType() + Environment.NewLine;
            message += "Stack Trace: " + ex.StackTrace + Environment.NewLine;
            message += "Inner Exception: " + ex.InnerException + Environment.NewLine;

            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), message, "Error", true);
        }
    }
}
