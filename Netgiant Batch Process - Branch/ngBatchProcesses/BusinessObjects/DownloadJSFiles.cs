using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects
{
    class DownloadJSFiles
    {
        public static void ProcessFiles(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"]);
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Started");

            string outputFile = parms["output"];
            string bkpFile = parms["output"].Replace(".js", "_bkp.js");
            bool errorOccured = false;
            string[] urls = new string[1];
            string download = "";

            if (parms["subtype"] == "tg")
            {
                urls[0] = "https://c.la2w2.salesforceliveagent.com/content/g/js/43.0/deployment.js";
                //urls[1] = "http://www.googleadservices.com/pagead/conversion.js";
                //urls[2] = "http://www.google-analytics.com/plugins/ga/inpage_linkid.js";
                //urls[3] = "https://www.dwin1.com/5500.js";
            }
            if (parms["subtype"] == "cm")
            {
                urls[0] = "https://c.la2w2.salesforceliveagent.com/content/g/js/43.0/deployment.js";
                //urls[1] = "http://www.googleadservices.com/pagead/conversion.js";
                //urls[2] = "http://www.google-analytics.com/plugins/ga/inpage_linkid.js";
                //urls[3] = "https://www.dwin1.com/808.js";
            }

            WebClient wc = new WebClient();

            foreach (string url in urls)
            {
                try
                {
                    download += "// Start of " + url + Environment.NewLine;
                    download += wc.DownloadString(url);
                    download += Environment.NewLine + "// End of " + url + Environment.NewLine;
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error Occured** - " + ex.Message);
                    errorOccured = true;
                }
            }

            if (errorOccured == false) 
            {
                File.Copy(outputFile, bkpFile, true);
                File.WriteAllText(outputFile, download.Replace("\n", ""));
            }
            else
            {
                string acitivityLogFileName = stnFunc.LogActivity(parms["type"]);
                stnFunc.SendSimpleEmail(parms["type"], acitivityLogFileName); 
            }
            
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Completed");
            stnFunc.LogActivity(parms["type"]);
            stnFunc = null;
        }
    }
}
