using System;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace ngBatchProcesses.BusinessObjects
{
    public static class DataSuppliers
    {
        private static bool errorOccured = false;

        /// <summary>
        /// Downloads the latest OpenRange data via FTP
        /// Executes a Stored Procedure which reads the downloaded data and inserts into tables prefixed with ngmd.or_ ...
        /// </summary>
        public static void UpdateOpenRange(string switchUsed)
        {
            StandardFunctions.WriteProcessStarted();

            string orRootDirectory = Properties.Settings.Default.LocalDirectory;
            string orSQLFilePath = Properties.Settings.Default.SQLServerFilePath;
            string orWorkingPath = orRootDirectory + (string)Properties.Settings.Default["OpenRangeWorkingPath"];
            string orExtractedPath = orSQLFilePath + (string)Properties.Settings.Default["OpenRangeExtractedPath"];
            string orArchivePath = orRootDirectory + (string)Properties.Settings.Default["OpenRangeArchivePath"] + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss");
            string orFTPUsername = (string)Properties.Settings.Default["OpenRangeFTPUN"];
            string orFTPPassword = (string)Properties.Settings.Default["OpenRangeFTPPW"];
            string orRequestedFile;
            string orRequestedFTPAddress;
            string orRequestedMediaLinksFile;
            string orRequestedMediaLinksFTPAddress;
            string orStoredProcedureName;

            //Set local parameters based on switch used
            switch (switchUsed)
            {
                case "today":
                    orRequestedFile = (string)Properties.Settings.Default["OpenRangeTodayFileNameQuery"];
                    orRequestedFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressIncremental"];
                    orRequestedMediaLinksFile = (string)Properties.Settings.Default["OpenRangeTodayMPLFileNameQuery"];
                    orRequestedMediaLinksFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressMPLIncremental"];
                    orStoredProcedureName = (string)Properties.Settings.Default["OpenRangeStoredProcedureInc"];
                    break;
                case "week":
                    orRequestedFile = (string)Properties.Settings.Default["OpenRangeWeekFileNameQuery"];
                    orRequestedFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressIncremental"];
                    orRequestedMediaLinksFile = (string)Properties.Settings.Default["OpenRangeWeekMPLFileNameQuery"];
                    orRequestedMediaLinksFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressMPLIncremental"];
                    orStoredProcedureName = (string)Properties.Settings.Default["OpenRangeStoredProcedureInc"];
                    break;
                case "month":
                    orRequestedFile = (string)Properties.Settings.Default["OpenRangeMonthFileNameQuery"];
                    orRequestedFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressIncremental"];
                    orRequestedMediaLinksFile = (string)Properties.Settings.Default["OpenRangeMonthMPLFileNameQuery"];
                    orRequestedMediaLinksFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressMPLIncremental"];
                    orStoredProcedureName = (string)Properties.Settings.Default["OpenRangeStoredProcedureInc"];
                    break;
                case "full":
                    orRequestedFile = (string)Properties.Settings.Default["OpenRangeFullFileNameQuery"];
                    orRequestedFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressFull"];
                    orRequestedMediaLinksFile = (string)Properties.Settings.Default["OpenRangeMPLFullFileNameQuery"];
                    orRequestedMediaLinksFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressMPLFull"];
                    orStoredProcedureName = (string)Properties.Settings.Default["OpenRangeStoredProcedureFull"];
                    break;
                default:
                    orRequestedFile = (string)Properties.Settings.Default["OpenRangeFTPAddressToday"];
                    orRequestedFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressToday"];
                    orRequestedMediaLinksFile = (string)Properties.Settings.Default["OpenRangeTodayFileNameQuery"];
                    orRequestedMediaLinksFTPAddress = (string)Properties.Settings.Default["OpenRangeFTPAddressMPLIncremental"];
                    orStoredProcedureName = (string)Properties.Settings.Default["OpenRangeStoredProcedureInc"];
                    break;
            }

            //Download the requested Data file(s)
            try
            {
                string[] ftpAddress = orRequestedFTPAddress.Replace("ftp://", "").Split(new char[] { '/' }, 2);
                Tuple<bool, string> rtn = FtpUtilities.DownloadFTPFiles(
                        ftpAddress[0],
                        orFTPUsername,
                        orFTPPassword,
                        "/" + ftpAddress[1],
                        orWorkingPath,
                        orRequestedFile,
                        ".zip",
                        false);
                if (rtn.Item1)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FluentFTP Download Successful for: " + "/" + ftpAddress[1] });
                }
                else
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR FluentFTP unable to Download FTP Folder: " + "/" + ftpAddress[1] + ". " + rtn.Item2, ErrorCode = "ERROR" });
                }

                //Download the requested MediaLinks file(s)
                ftpAddress = orRequestedMediaLinksFTPAddress.Replace("ftp://", "").Split(new char[] { '/' }, 2);
                rtn = FtpUtilities.DownloadFTPFiles(
                        ftpAddress[0],
                        orFTPUsername,
                        orFTPPassword,
                        "/" + ftpAddress[1],
                        orWorkingPath,
                        orRequestedMediaLinksFile,
                        ".zip",
                        false);
                if (rtn.Item1)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FluentFTP Download Successful for: " + "/" + ftpAddress[1] });
                }
                else
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR FluentFTP unable to Download FTP Folder: " + "/" + ftpAddress[1] + ". " + rtn.Item2, ErrorCode = "ERROR" });
                }

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully downloaded from FTP" });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR downloading from FTP - " + ex.Message, ErrorCode = "ERROR" });
                errorOccured = true;
            }

            if (!errorOccured)
            {
                try
                {
                    StandardFunctions stnFunc = new StandardFunctions();
                    stnFunc.ExtractZipFile(orWorkingPath, orExtractedPath);
                    stnFunc.ArchiveFile(orWorkingPath, orArchivePath);
                    stnFunc.CleanupArchiveLocation(orRootDirectory + (string)Properties.Settings.Default["OpenRangeArchivePath"]);
                    stnFunc = null;
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR - " + ex.Message, ErrorCode = "ERROR" });
                    errorOccured = true;
                }
            }

            //Run the Stored Procedure
            if (!errorOccured)
            {
                try
                {
                    SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", orStoredProcedureName, 0);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Successfully executed stored procedure - {orStoredProcedureName}" });
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR executing stored procedure - {orStoredProcedureName}", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                    errorOccured = true;
                }
            }

            //Log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        /// <summary>
        /// Executes a Stored Procedure which copies CNET data to the netgiantMasterData DB, into tables prefixed with dbo.cds_ ...
        /// </summary>
        //public static void CopyCNETData(string switchUsed)
        //{
        //    StandardFunctions stnFunc = new StandardFunctions();
        //    string spName = (string)Properties.Settings.Default["CopyCNETDataStoredProcedure"];
        //    try
        //    {
        //        SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", spName, 2000);
        //        stnFunc.AddToActivityLog($"Successfully executed stored procedure - {spName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        stnFunc.AddToActivityLog($"**ERROR** executing stored procedure - {spName}");
        //        stnFunc.ProcessException(ex);
        //        errorOccured = true;
        //    }

        //    //Log
        //    stnFunc.AddToActivityLog("Program finished with switch: " + switchUsed);
        //    string acitivityLogFileName = stnFunc.LogActivity(switchUsed);
        //    if (errorOccured) { stnFunc.SendSimpleEmail(switchUsed, acitivityLogFileName); }
        //    stnFunc = null;
        //}
    }
}
