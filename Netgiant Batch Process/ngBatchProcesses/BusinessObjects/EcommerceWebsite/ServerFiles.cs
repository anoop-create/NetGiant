using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class ServerFiles
    {
        public static void ClearServerFiles(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();

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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Occured clearing server files", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Server Files Cleared: " + deletedCount });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
