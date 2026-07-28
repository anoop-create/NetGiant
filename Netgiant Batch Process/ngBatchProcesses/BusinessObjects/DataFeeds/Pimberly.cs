using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class Pimberly
    {
        public Pimberly(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            if (Parms.ContainsKey("subtype"))
            {
                SubType = Parms["subtype"];
            }
            if (Parms.ContainsKey("action"))
            {
                Action = Parms["action"];
            }
            if (Parms.ContainsKey("skip"))
            {
                SkipFtp = true;
            }

            //Files = new string[] { "Products.txt", "Options.txt", "Features.txt", "Attributes.txt", "AttributeGroups.txt", "MediaLinks.txt" };
            Files = new string[] { "Products.txt", "Attributes.txt", "MediaLinks.txt" };
            Folders = Properties.Settings.Default["PimberlyFolders"].ToString().Split('|');
            FTPAddress = (string)Properties.Settings.Default["PimberlyFTPAddress"];
            FTPUsername = (string)Properties.Settings.Default["PimberlyFTPUN"];
            FTPPassword = (string)Properties.Settings.Default["PimberlyFTPPW"];
            SQLFilePath = Properties.Settings.Default.SQLServerFilePath;
            SQLSPFilePath = Properties.Settings.Default.SQLServerLocalDirectory + (string)Properties.Settings.Default["PimberlyExtractedPath"]; ;
            RootDirectory = Properties.Settings.Default.LocalDirectory;
            WorkingPath = RootDirectory + (string)Properties.Settings.Default["PimberlyWorkingPath"];
            ExtractedPath = SQLFilePath + (string)Properties.Settings.Default["PimberlyExtractedPath"];
            ArchivePath = RootDirectory + (string)Properties.Settings.Default["PimberlyArchivePath"];
        }

        public Dictionary<string, string> Parms { get; set; }
        public string SubType { get; set; }
        public string Action { get; set; } = "";
        public string[] Files { get; set; }
        public string[] Folders { get; set; }
        public string RequestedFile { get; set; }
        public string FTPAddress { get; set; }
        public string FTPUsername { get; set; }
        public string FTPPassword { get; set; }
        public string SQLFilePath { get; set; }
        public string SQLSPFilePath { get; set; }
        public string WorkingPath { get; set; }
        public string ExtractedPath { get; set; }
        public string ArchivePath { get; set; }
        public string RootDirectory { get; set; }
        public string SPName1 { get; set; }
        public string SPName2 { get; set; }
        public string SPName3 { get; set; }
        public bool ErrorOccured { get; set; } = false;
        public bool SkipFtp { get; set; } = false;


        public void ProcessFeed()
        {
            switch (SubType)
            {
                case "changes":
                    {
                        ErrorOccured = true;
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR incremental updates not supported", ErrorCode = "ERROR" });

                        SPName1 = "ngmd.pimberlyUpdateInc1";
                        SPName2 = "ngmd.pimberlyUpdateInc2";
                        SPName3 = "ngmd.pimberlyDeleteDuplicates";
                        break;
                    }
                case "complete":
                    {
                        //ErrorOccured = true;
                        //StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR it's not possible to completely reload using this mechanism", ErrorCode = "ERROR" });

                        RequestedFile = "complete.zip";
                        SPName1 = "ngmd.pimberlyUpdateFull1";
                        SPName2 = "ngmd.pimberlyUpdateFull2";
                        SPName3 = "ngmd.pimberlyDeleteDuplicates";
                        break;
                    }
                case "feed":
                    {
                        GenerateFeed();
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                        return;
                    }
            }
            switch (Action)
            {
                case "daily":
                    {
                        RequestedFile = "daily" + DateTime.Now.ToString("yyyyMMdd") + ".zip";
                        break;
                    }
                case "weekly":
                    {
                        RequestedFile = "weekly.zip";
                        break;
                    }
                case "monthly":
                    {
                        RequestedFile = "monthly.zip";
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            if (!ErrorOccured)
            {
                LoadFiles();
            }
            //if (!ErrorOccured)
            //{
            //    LoadAttributesFiles();
            //}
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public static void CopyImages()
        {
            string directory = @"c:\temp\zz\images\";
            // Retrieve images to copy
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            //string sql = @"SELECT 
            //    P.productID, P.partNo, M.equipmentManuName, WI.websiteInventoryID, WI.WebsiteFK, ORM.prodID, ORM.[type], ORM.[url]
            //    FROM ngmd.product P
            //    INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
            //    INNER JOIN ngmd.websiteInventory WI ON WI.productFK = P.productID
            //    INNER JOIN ngmd.or_products ORP ON ORP.partno = P.partNo AND ORP.manufacturer = M.manufacturerName
            //    INNER JOIN ngmd.or_mediaLinks ORM ON ORM.prodID = ORP.prodID AND ORM.type = 'IMG'
            //    OUTER APPLY (SELECT TOP 1 * FROM ngmd.pim_products p1 WHERE p1.partno = P.partNo AND p1.manufacturer = M.manufacturerName) PP
            //    OUTER APPLY (SELECT TOP 1 * FROM ngmd.pim_mediaLinks p2 WHERE p2.prodID = PP.prodID 
            //     AND p2.type IN ('JPG', 'JPEG')
            //    ) PM
            //    WHERE P.productStatusFK IN (1,8)
            //    AND P.productItemTypeFK = 1
            //    AND M.manufacturerName NOT IN ('Own Brand', 'Misc')
            //    AND WI.websiteFK IS NOT NULL
            //    AND ORP.prodID IS NOT NULL
            //    AND ORM.prodID IS NOT NULL
            //    AND M.manufacturerName = 'Epson'
            //    AND PP.prodID IS NOT NULL
            //    AND PM.prodID IS NULL
            //    ORDER BY P.partNo";

            //string sql = @"SELECT
            //    PP.productID, PP.partNo, p.manufacturer, WI.websiteInventoryID, WI.WebsiteFK, om.prodID, om.[type], om.[url]
            //    FROM ngmd.pim_products p
            //    INNER JOIN ngmd.pim_mediaLinks m ON m.prodID = p.prodID
            //    INNER JOIN ngmd.product PP ON PP.partNo = p.partno AND PP.productStatusFK IN(1,8)
            //    INNER JOIN ngmd.websiteInventory WI ON WI.productFK = PP.productID
            //    LEFT OUTER JOIN ngmd.or_products op ON op.partno = p.partno AND op.manufacturer = p.manufacturer
            //    LEFT OUTER JOIN ngmd.or_mediaLinks om ON om.prodID = op.prodID AND om.type = 'IMG'
            //    WHERE m.url LIKE '%logo.jpg'
            //    AND om.prodID IS NOT NULL
            //    ORDER BY p.manufacturer, p.partno";

            string sql = @"SELECT
                P.productID, P.partNo, M.manufacturerName, WI.websiteInventoryID, WI.WebsiteFK, ORM2.prodID, ORM2.[type], ORM2.[url]
                FROM ngmd.product P
                INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
                INNER JOIN ngmd.websiteInventory WI ON WI.productFK = P.productID
                OUTER APPLY(SELECT TOP 1 * FROM ngmd.pim_products or1 WHERE or1.partno = P.partNo AND or1.manufacturer = M.manufacturerName) ORP
                OUTER APPLY(SELECT TOP 1 * FROM ngmd.pim_mediaLinks or3 WHERE or3.prodID = ORP.prodID
                   AND or3.type IN('JPG', 'JPEG')
                ) ORM
                INNER JOIN ngmd.or_products ORP2 ON ORP2.partno = P.partNo AND ORP2.manufacturer = M.manufacturerName
                INNER JOIN ngmd.or_mediaLinks ORM2 ON ORM2.prodID = ORP2.prodID AND ORM2.Type = 'IMG'
                WHERE P.productStatusFK IN(1,8)
                AND P.productItemTypeFK IN (1, 3)
                AND M.manufacturerName NOT IN('Own Brand', 'Misc')
                AND WI.websiteFK IS NOT NULL
                AND(ORP.prodID IS NULL OR(ORP.prodID IS NOT NULL AND ORM.prodID IS NULL))
                ORDER BY M.manufacturerName, P.partNo";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "img");
            dt = ds.Tables[0];

            string tempPartNo = "";
            int imgNo = 0;
            using (var db = new ngmdEntities())
            {
                using (WebClient client = new WebClient())
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        // Copy the image across
                        string url1 = dr["url"].ToString();
                        string partNo = dr["partNo"].ToString();
                        int websiteId = int.Parse(dr["websiteFK"].ToString());
                        int websiteInventoryId = int.Parse(dr["websiteInventoryId"].ToString());
                        if (partNo == tempPartNo)
                        {
                            imgNo++;
                        }
                        else
                        {
                            tempPartNo = partNo;
                            imgNo = 1;
                        }
                        if (Properties.Settings.Default.Environment == "Live")
                        {
                            switch (websiteId)
                            {
                                case 1:
                                    {
                                        directory = @"D:\IIS-Content\www.tonergiant.co.uk\cdn\Images\";
                                        break;
                                    }
                                case 2:
                                    {
                                        directory = @"D:\IIS-Content\www.cartridgemonkey.com\cdn\Images\";
                                        break;
                                    }
                                case 3:
                                    {
                                        directory = @"D:\IIS-Content\www.netgiant.com\cdn\Images\";
                                        break;
                                    }
                            }
                        }

                        string url2 = partNo + "-" + imgNo.ToString() + ".jpg";

                        client.DownloadFile(new Uri(url1), directory + "stock-main\\" + url2);
                        client.DownloadFile(new Uri(url1), directory + "stock-thumbnail\\" + url2);

                        // Insert entry into productImages table
                        if (Properties.Settings.Default.Environment == "Live")
                        {
                            try
                            {
                                productImage pi = new productImage
                                {
                                    websiteInventoryFK = websiteInventoryId,
                                    mainImage = true,
                                    thumbnailImage = false,
                                    URL = @"Images/stock-main/" + url2
                                };

                                db.Entry(pi).State = EntityState.Added;

                                pi = new productImage
                                {
                                    websiteInventoryFK = websiteInventoryId,
                                    mainImage = false,
                                    thumbnailImage = true,
                                    URL = @"Images/stock-thumbnail/" + url2
                                };

                                db.Entry(pi).State = EntityState.Added;
                            }
                            catch (Exception e)
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Could not insert product image for " + url2 });
                            }
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        private void LoadFiles()
        {
            bool firstTime = true;
            foreach (string folder in Folders)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processing " + folder });
                // Download .zip file
                try
                {
                    FtpUtilities.DownloadSFTPFiles(FTPAddress,
                                                FTPUsername,
                                                FTPPassword,
                                                WorkingPath,
                                                "/" + folder + "/" + SubType + "/",
                                                RequestedFile);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully downloaded " + RequestedFile + " from FTP" });
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "SQL Path: " + ExtractedPath });

                    StandardFunctions stnFunc = new StandardFunctions();
                    if (!stnFunc.ExtractZipFile(WorkingPath, ExtractedPath))
                    {
                        ErrorOccured = true;
                    }
                    stnFunc.ArchiveFile(WorkingPath, ArchivePath + DateTime.Now.ToString("yyyyMMdd_H_mm_ss"));
                    stnFunc.CleanupArchiveLocation(ArchivePath);
                    stnFunc = null;

                    if (ErrorOccured)
                    {
                        break;
                    }

                    try
                    {
                        using (var sqlUtil = new SQLUtilitiesDyn())
                        {
                            List<SqlParameter> sqlParms = new List<SqlParameter>();
                            SqlParameter sqlParm = new SqlParameter("@filePickupPath", SqlDbType.VarChar);
                            sqlParm.Value = SQLSPFilePath;
                            sqlParms.Add(sqlParm);
                            sqlParm = new SqlParameter("@isLive", SqlDbType.Bit);
                            sqlParm.Value = Properties.Settings.Default.Environment == "Live" ? 1 : 0;
                            sqlParms.Add(sqlParm);
                            sqlParm = new SqlParameter("@isFirstTime", SqlDbType.Bit);
                            sqlParm.Value = firstTime ? 1 : 0;
                            sqlParms.Add(sqlParm);
                            DataSet ds = sqlUtil.ExecuteReadStoredProcedure("netgiantBatchProcesses", SPName1, sqlParms);
                            foreach (string msg in sqlUtil.Messages)
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = msg });
                            }
                        }

                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Successfully executed stored procedure - {SPName1}" });

                        LoadAttributesFiles(firstTime);
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR executing stored procedure - {SPName1}", ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
                        ErrorOccured = true;
                    }

                }
                catch (Exception ex)
                {
                    ErrorOccured = true;
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR processing " + RequestedFile + " from FTP: " + ex.Message, ErrorCode = "ERROR" });
                }
                firstTime = false;
            }

            // Tidy up duplicates in the MediaLinks and Attributes tables
            //try
            //{
            //    using (var sqlUtil = new SQLUtilitiesDyn())
            //    {
            //        List<SqlParameter> sqlParms = new List<SqlParameter>();
            //        DataSet ds = sqlUtil.ExecuteReadStoredProcedure("netgiantBatchProcesses", SPName3, sqlParms);
            //        foreach (string msg in sqlUtil.Messages)
            //        {
            //            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = msg });
            //        }
            //    }

            //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Successfully executed stored procedure - {SPName3}" });
            //}
            //catch (Exception ex)
            //{
            //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR executing stored procedure - {SPName3}", ErrorCode = "ERROR" });
            //    StandardFunctions.WriteException(ex);
            //    ErrorOccured = true;
            //}

        }

        private void LoadAttributesFiles(bool firstTime)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            // Copy back the Attributes.txt file to the Working Directory
            if (!stnFunc.CopyFile(ExtractedPath + "Attributes.txt", WorkingPath + "AttributesFull.txt"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Error copying Attribute file from SQL Server", ErrorCode = "ERROR" });
                ErrorOccured = true;
                return;
            }

            // Create a list of valid prodID's from the (recently created) pim_products table
            List<string> pimIds = new List<string>();
            pimIds = EntityFunctions.GetPimberlyProduct(x => true)
                        .Select(x => x.prodID)
                        .ToList();

            // Now manually read each record on the Attributes.txt file and check if its for a valid prodID. If so write it out to a new Attributes.txt file
            using (StreamWriter sw = new StreamWriter(WorkingPath + "Attributes.txt"))
            {
                using (StreamReader sr = new StreamReader(WorkingPath + "AttributesFull.txt"))
                {
                    string line;
                    int counter = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (counter == 0)
                        {
                            sw.WriteLine(line);
                            counter = +1;
                            continue;
                        }
                        string prodId = line.Split('|')[0];
                        if (pimIds.Contains(prodId))
                        {
                            // Remove any encoded bullets
                            sw.WriteLine(line.Replace("•", ""));
                        }
                        counter = +1;
                    }
                }
            }

            // Copy the new Attributes.txt file back to the Extracted folder
            if (!stnFunc.CopyFile(WorkingPath + "Attributes.txt", ExtractedPath + "Attributes.txt"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Error copying Attribute file to SQL Server", ErrorCode = "ERROR" });
                ErrorOccured = true;
                return;
            }

            // Run the pimberlyUpdateFull2 to load the pim_attribute table
            try
            {
                int timeout = 20;
                if (Properties.Settings.Default.Environment != "Live")
                {
                    timeout = 120;
                }
                using (var sqlUtil = new SQLUtilitiesDyn())
                {
                    List<SqlParameter> sqlParms = new List<SqlParameter>();
                    SqlParameter sqlParm = new SqlParameter("@filePickupPath", SqlDbType.VarChar);
                    sqlParm.Value = SQLSPFilePath;
                    sqlParms.Add(sqlParm);
                    sqlParm = new SqlParameter("@isLive", SqlDbType.Bit);
                    sqlParm.Value = Properties.Settings.Default.Environment == "Live" ? 1 : 0;
                    sqlParms.Add(sqlParm);
                    sqlParm = new SqlParameter("@isFirstTime", SqlDbType.Bit);
                    sqlParm.Value = firstTime ? 1 : 0;
                    sqlParms.Add(sqlParm);
                    DataSet ds = sqlUtil.ExecuteReadStoredProcedure("netgiantBatchProcesses", SPName2, sqlParms, "defaultDataSet", timeout);
                    foreach (string msg in sqlUtil.Messages)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = msg });
                    }
                }

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"Successfully executed stored procedure - {SPName2}" });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR executing stored procedure - {SPName2}", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                ErrorOccured = true;
            }

            // Tidy up files
            File.Delete(WorkingPath + "Attributes.txt");
            File.Delete(WorkingPath + "AttributesFull.txt");
        }

        private void GenerateFeed()
        {
            string connName = "netgiantMasterData";
            char outputDelim = '\t';

            var sqlParams = new List<KeyValuePair<string, string>>();
            DataTable eqResults;
            try
            {
                eqResults = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ngmd.GetPimberlyFeed", sqlParams);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.GetPimberlyFeed", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            //Write the CSV file
            using (CsvFileWriter writer = new CsvFileWriter(WorkingPath + "NetgiantFeed.txt", outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    CsvRow firstRow = new CsvRow();

                    //Write details
                    foreach (DataColumn dc in eqResults.Columns)
                    {
                        firstRow.Add(dc.ColumnName);
                    }
                    writer.WriteRow(firstRow);

                    //Loop through the datatable and write a row in the csv file for each
                    foreach (DataRow dr in eqResults.Rows)
                    {
                        CsvRow newRow = new CsvRow();
                        foreach (DataColumn dc in eqResults.Columns)
                        {
                            newRow.Add(dr[dc.ColumnName].ToString());
                        }
                        writer.WriteRow(newRow);
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing Equipment CSV File: " + WorkingPath + "NetgiantFeed.txt: " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //FTP output file
            if (!SkipFtp)
            {

                try
                {
                    Tuple<bool, string> rtn = FtpUtilities.UploadFTPFile(WorkingPath + "NetgiantFeed.txt",
                         Parms["ftpsite"],
                         Parms["ftpusername"],
                         Parms["ftppassword"],
                         Parms["ftppath"] + "NetgiantFeed.txt",
                         false);
                    if (rtn.Item1)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                    }
                    else
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + WorkingPath + "NetgiantFeed.txt", ErrorCode = "ERROR" });
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + WorkingPath + "NetgiantFeed.txt: " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }                
            }

            // Tidy up files
            File.Delete(WorkingPath + "NetgiantFeed.txt");
        }
    }
}
