using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class LoadSalesHistory
    {
        public LoadSalesHistory(Dictionary<string, string> parms)
        {
            _suppliedParams = parms;
            InitializeTenant();
        }

        private readonly Dictionary<string, string> _suppliedParams;
        private Tenant _tenant;
        private Channel _channel;
        private static List<SalesHistory> _salesHistoryList;
        private static bool _errorOccured;
        private static List<DownloadedFileData> _feedFiles;

        public bool Load()
        {
            CleanupStagingTables();

            if (ValidateAndGetFeeds(_channel))
            {
                _tenant.LoadSalesHistoryData(_channel);
                SetSalesHistoryMappings(_channel);
            }

            return true;
        }

        public static bool ValidateAndGetFeeds(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START ValidateAndGetSalesHistoryFeeds", "Information");

            var passValidation = true;

            try
            {
                _feedFiles.AddRange(GetFtpFiles(channel, FtpFileType.SalesHistory));

                foreach (var feed in _feedFiles)
                {
                    if (passValidation)
                    {
                        if (!feed.DownloadResult.IsSuccess)
                        {
                            passValidation = false;
                            break;
                        }
                        else
                        {
                            if (!FeedHasData(feed, channel))
                            {
                                passValidation = false;
                                CommonDataFunctions.CreateLogEntry(channel, "Feed: " + feed.Settings.FTPFileName +
                                    " is invalid. Processing stopped. No prices have been calculated or amended.",
                                    "Notification", true);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                passValidation = false;
                CommonDataFunctions.CreateLogEntry(channel, "Feeds have failed validation checks. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Notification", true);
            }

            CommonDataFunctions.CreateLogEntry(channel, "END ValidateAndGetSalesHistoryFeeds", "Information");

            return passValidation;
        }

        private static bool FeedHasData(DownloadedFileData feed, Channel channel)
        {
            var hasData = false;
            var fileContent = CommonFunctions.ReadFileToString(feed.DownloadResult.Path, feed.DownloadResult.BlobContainer);
            var isIndexBased = CommonFunctions.IsIndexBased(feed.Settings.FieldMapping);

            var counter = 1;
            using (var csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                while (!csvReader.EndOfData && counter < 5)
                {
                    var currentLine = csvReader.ReadLine();

                    if (!isIndexBased && counter == 2)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            hasData = true;
                            break;
                        }
                    }
                    else if (isIndexBased && counter == 1)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            hasData = true;
                            break;
                        }
                    }

                    counter++;
                }
            }

            return hasData;
        }

        private static List<DownloadedFileData> GetFtpFiles(Channel channel, FtpFileType fileType)
        {
            var ftpFileList = new List<DownloadedFileData>();
            var ftpSettingList = new List<FTPSetting>();
            ftpSettingList = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Sales History Inventory").ToList();


            foreach (var ftpSetting in ftpSettingList)
            {
                ftpSetting.FTPPath = !string.IsNullOrEmpty(ftpSetting.FTPPath) ?
                    string.Format("//{0}//", ftpSetting.FTPPath) : string.Empty;

                var ftpFileName = !string.IsNullOrEmpty(ftpSetting.FTPZipFileName) ? ftpSetting.FTPZipFileName : ftpSetting.FTPFileName;

                var ftpHostDetails = new Ftp.FtpHostDetails()
                {
                    BlobContainer = "tenantfolders",
                    FileName = ftpFileName,
                    FtpHost = ftpSetting.FTPServer,
                    FtpUser = ftpSetting.FTPUser,
                    FtpPassword = ftpSetting.FTPPassword,
                    FolderPath = ftpSetting.FTPPath,
                    Protocol = CommonFunctions.LookupFtpProtocol(ftpSetting.FTPProtocolFK),
                    SavePath = channel.TenantFK.ToString() + "\\" + ftpSetting.FTPSettingsID + "_" + ftpFileName,
                };

                var ftpDownloadResult = Ftp.DownloadFTPFile(ftpHostDetails);

                if (ftpDownloadResult.IsSuccess)
                {
                    if (Path.GetExtension(ftpDownloadResult.Path) == ".zip")
                    {
                        var extractZipResult = CommonFunctions.ExtractZipFile(ftpDownloadResult.Path,
                            channel.TenantFK.ToString() + "\\",
                            "tenantfolders", ftpSetting.FTPFileName,
                            ftpSetting.FTPSettingsID.ToString());

                        if (extractZipResult.IsSuccess)
                        {
                            ftpDownloadResult.Path = extractZipResult.Path;
                        }
                        else
                        {
                            CommonFunctions.LogExtractZipError(channel, ftpSetting, extractZipResult);
                            continue;
                        }
                    }

                    CommonFunctions.UpdateFeedTimestamp(ftpSetting, ftpDownloadResult);
                }
                else
                {
                    CommonFunctions.LogFtpError(channel, ftpDownloadResult);
                }

                ftpFileList.Add(new DownloadedFileData()
                {
                    Settings = ftpSetting,
                    DownloadResult = ftpDownloadResult,
                    FileType = fileType
                });
            }

            return ftpFileList;
        }

        public static bool PopulateSalesHistoryFromFtp(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START ProcessSalesHistoryFiles", "Information");

            var ftpFiles = _feedFiles.Where(x => x.Settings.Lookup.LookupName == "Sales History Inventory").ToList();

            _salesHistoryList = new List<SalesHistory>();

            foreach (var downloadedFile in ftpFiles)
            {
                var salesHistoryList = GetSalesHistoryList(channel, downloadedFile);

                if (salesHistoryList?.Count == 0)
                {
                    throw new EmptyFeedException("Sales History file: " + downloadedFile.Settings.FTPFileName +
                                                 " Contains no data. Processing stopped. No prices have been calculated or amended.");
                }

                if (salesHistoryList != null) _salesHistoryList.AddRange(salesHistoryList);
            }

            CreateSalesHistoryData(channel);

            CommonDataFunctions.CreateLogEntry(channel, "END ProcessSalesHistoryFiles", "Information");

            return true;
        }

        private static List<SalesHistory> GetSalesHistoryList(Channel channel, DownloadedFileData file)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START GetSalesHistoryList", "Information");

            var salesHistoryList = new List<SalesHistory>();
            var fileContent = CommonFunctions.ReadFileToString(file.DownloadResult.Path, file.DownloadResult.BlobContainer);
            var delimeter = "";

            using (var sr = new StringReader(fileContent))
            {
                delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
            }

            using (TextFieldParser csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                csvReader.SetDelimiters(new string[] { delimeter });
                csvReader.TrimWhiteSpace = true;

                var firstLine = new string[] { };
                var fields = file.Settings.FieldMapping;

                if (!CommonFunctions.IsIndexBased(fields))
                {
                    firstLine = csvReader.ReadFields();
                }

                var indexList = new List<int>();
                var periodColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.Period, indexList);
                var endDateColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.Date, indexList);
                var clientProductIdColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.ClientProductID, indexList);
                var quantityColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.Quantity, indexList);
                var averageCostPriceColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.Price2, indexList, false);
                var averagePriceColumn = CommonFunctions.LookupFieldIndex(file, firstLine, fields.Price, indexList, false);

                if (CommonFunctions.CheckValidColumns(indexList))
                {
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            var rowData = csvReader.ReadFields();
                            var period = CommonFunctions.GetRowFieldData(rowData, periodColumn);
                            var endDate = DateTime.ParseExact(CommonFunctions.GetRowFieldData(rowData, endDateColumn), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                            var clientProductId = CommonFunctions.GetRowFieldData(rowData, clientProductIdColumn);
                            var quantity = Convert.ToInt32(CommonFunctions.GetRowFieldData(rowData, quantityColumn));
                            var averageCostPrice = Convert.ToDecimal(CommonFunctions.GetRowFieldData(rowData, averageCostPriceColumn));
                            var averagePrice = Convert.ToDecimal(CommonFunctions.GetRowFieldData(rowData, averagePriceColumn));

                            var salesHistory = new SalesHistory()
                            {
                                EndDate = endDate,
                                StartDate = DetermineStartDate(endDate, period),
                                ClientProductId = clientProductId,
                                Quantity = quantity,
                                AverageCostPrice = averageCostPrice,
                                AveragePrice = averagePrice
                            };

                            salesHistoryList.Add(salesHistory);
                        }
                        catch (Exception e)
                        {
                            CommonDataFunctions.CreateLogEntry(channel, "ERROR:" + e.Message + " " + e.StackTrace, "Error");
                        }
                    }
                }
                else
                {
                    LogFieldMappingError(channel, file);
                    salesHistoryList = null;
                }
            }

            CommonDataFunctions.CreateLogEntry(channel, "END GetSalesHistoryList", "Information");

            return salesHistoryList;
        }

        private static DateTime DetermineStartDate(DateTime endDate, string period)
        {
            switch (period)
            {
                case "d":
                    return endDate.AddDays(-1);
                case "w":
                    return endDate.AddDays(-7);
                case "m":
                    return endDate.AddMonths(-1);
                case "y":
                    return endDate.AddYears(-1);
                default:
                    return endDate.AddDays(-1);
            }
        }

        private static void CreateSalesHistoryData(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START CreateSalesHistoryData - COMPUTE", "Information");

            try
            {
                var salesHistoryCrud = new CrudSalesHistory();
                foreach (var sale in _salesHistoryList)
                {
                    sale.ChannelFk = channel.ChannelID;
                }

                var successUpdate = salesHistoryCrud.Create(_salesHistoryList, channel);

                if (!successUpdate)
                    _errorOccured = true;
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to create / update sales history data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
                CommonDataFunctions.CreateLogEntry(channel, "Error creating sales history data. Please contact support", "Notification", true);

                _errorOccured = true;
            }

        }

        private void CleanupStagingTables()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CleanupStagingSalesHistoryTable", "Information");

            try
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
                sqlParm1.Value = _channel.ChannelID;
                sqlParms.Add(sqlParm1);
                var isSuccess = SQL.ExecuteStoredProcedure("DP001", "DeleteStagingSalesHistoryEntries", sqlParms, _channel.ChannelID);

                if (!isSuccess)
                    CommonDataFunctions.CreateLogEntry(_channel, "Unable to complete process due to errors found. Please contact support.", "Notification");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to cleanup staging sales history table. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
                _errorOccured = true;
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CleanupStagingSalesHistoryTable", "Information");
        }

        private void InitializeTenant()
        {
            try
            {
                _tenant = new Tenant();
                _channel = _tenant.GetChannelRecord(Convert.ToInt32(_suppliedParams["channelid"]));
                _tenant.SetupTenantDelegates(_channel);
                _feedFiles = new List<DownloadedFileData>();

                _errorOccured = false;
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to initialize tenant. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
                _errorOccured = true;
            }
        }

        private static void LogFieldMappingError(Channel channel, DownloadedFileData file)
        {
            CommonDataFunctions.CreateLogEntry(channel, "Could not find all column mappings in file '" +
                                    file.Settings.FTPFileName + "'", "Notification", true);
            _errorOccured = true;
        }

        private static void SetSalesHistoryMappings(Channel channel)
        {
            try
            {
                CommonDataFunctions.CreateLogEntry(channel, "START SetSalesHistoryMappings", "Information");
                using (DP001Entities db = new DP001Entities())
                {
                    db.Database.CommandTimeout = 120;
                    db.SetSalesHistoryMappings(channel.ChannelID);
                }
                CommonDataFunctions.CreateLogEntry(channel, "END SetSalesHistoryMappings", "Information");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to map Sales History Data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
                CommonDataFunctions.CreateLogEntry(channel, "Error Mapping Sales History Data. Please contact support", "Notification", true);

                _errorOccured = true;
            }
        }
    }
}
