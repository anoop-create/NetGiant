using System;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;

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
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog("Program started with switch: " + switchUsed);

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
                FtpUtilities.DownloadFTPFiles(orRequestedFTPAddress,
                                            orFTPUsername,
                                            orFTPPassword,
                                            orWorkingPath,
                                            orRequestedFile,
                                            ".zip");

                //Download the requested MediaLinks file(s)
                FtpUtilities.DownloadFTPFiles(orRequestedMediaLinksFTPAddress,
                                                orFTPUsername,
                                                orFTPPassword,
                                                orWorkingPath,
                                                orRequestedMediaLinksFile,
                                                ".zip");

                stnFunc.AddToActivityLog("Successfully downloaded from FTP");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog(" Error downloading from FTP - " + ex.Message);
                errorOccured = true;
            }

            if (!errorOccured)
            {
                try
                {
                    stnFunc.ExtractZipFile(orWorkingPath, orExtractedPath);
                    stnFunc.ArchiveFile(orWorkingPath, orArchivePath);
                    stnFunc.CleanupArchiveLocation(orRootDirectory + (string)Properties.Settings.Default["OpenRangeArchivePath"]);
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog(" Error - " + ex.Message);
                    errorOccured = true;
                }
            }

            //Run the Stored Procedure
            if (!errorOccured)
            {
                try
                {
                    SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", orStoredProcedureName, 0);
                    stnFunc.AddToActivityLog($"Successfully executed stored procedure - {orStoredProcedureName}");
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog($"**ERROR** executing stored procedure - {orStoredProcedureName}");
                    stnFunc.ProcessException(ex);
                    errorOccured = true;
                }
            }

            //Log
            stnFunc.AddToActivityLog("Program finished with switch: " + switchUsed);
            string acitivityLogFileName = stnFunc.LogActivity(switchUsed);
            if (errorOccured) { stnFunc.SendSimpleEmail(switchUsed, acitivityLogFileName); }
            stnFunc = null;
        }

        /// <summary>
        /// Executes a Stored Procedure which copies CNET data to the netgiantMasterData DB, into tables prefixed with dbo.cds_ ...
        /// </summary>
        public static void CopyCNETData(string switchUsed)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog("Program started with switch: " + switchUsed);
            string spName = (string)Properties.Settings.Default["CopyCNETDataStoredProcedure"];
            try
            {
                SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", spName, 2000);
                stnFunc.AddToActivityLog($"Successfully executed stored procedure - {spName}");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog($"**ERROR** executing stored procedure - {spName}");
                stnFunc.ProcessException(ex);
                errorOccured = true;
            }

            //Log
            stnFunc.AddToActivityLog("Program finished with switch: " + switchUsed);
            string acitivityLogFileName = stnFunc.LogActivity(switchUsed);
            if (errorOccured) { stnFunc.SendSimpleEmail(switchUsed, acitivityLogFileName); }
            stnFunc = null;
        }
    }
}
