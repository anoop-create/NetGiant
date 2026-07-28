using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.IO;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    // TIDYUP
    class TradeSupplyTrackingReport
    {
        // NO LONGER IN USE
        //public static void GetTradeSupplyTrackingReport(Dictionary<string, string> parms)
        //{
        //    StandardFunctions.WriteProcessStarted();

        //    string thisFile = Properties.Settings.Default.LocalDirectory + parms["output"] + "\\" + parms["filea"];

        //    try
        //    {
        //        FtpUtilities.DownloadFTPFiles(
        //            parms["ftppath"],
        //            parms["ftpusername"],
        //            parms["ftppassword"],
        //            Properties.Settings.Default.LocalDirectory + parms["output"],
        //            parms["filea"]
        //            );
        //    }
        //    catch (Exception e)
        //    {
        //        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to FTP to Trade Supply - " + e.ToString(), ErrorCode = "ERROR" });
        //    }

        //    try
        //    {
        //        //alter the CSV file
        //        string[] allFile = File.ReadAllLines(thisFile);
        //        StringBuilder qwe = new StringBuilder();

        //        //start counting from 1 because the first line is the titles
        //        for (int i = 1; i < allFile.Length; i++)
        //        {
        //            string[] allItems = allFile[i].Split(',');
        //            for (int j = 0; j < allItems.Length; j++)
        //            {
        //                allItems[j] = "=" + allItems[j];
        //            }
        //            allFile[i] = string.Join(",", allItems);
        //        }

        //        File.WriteAllLines(thisFile, allFile);
        //    }
        //    catch (Exception e)
        //    {
        //        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to edit the Despatch Report - " + e.ToString(), ErrorCode = "ERROR" });
        //    }


        //    try
        //    {
        //        List<string> emailAddresses = new List<string>();
        //        emailAddresses.Add("purchasing@netgiant.com");
                
        //        Email.SendEmail(
        //            emailAddresses,
        //            "sales@tonergiant.co.uk",
        //            "Trade Supply Despatch Report " + DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy"),
        //            "Trade Supply Despatch Report " + DateTime.Now.AddDays(-1).ToString("dd-MM-yyyy"),
        //            false,
        //            thisFile,
        //            ""
        //            );
        //    }
        //    catch (Exception e)
        //    {
        //        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to Email Trade Supply Despatch Report - " + e.ToString(), ErrorCode = "ERROR" });
        //    }

        //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        //}
    }
}
