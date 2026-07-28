using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.Services;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Collections;
using ngBatchProcesses.BusinessObjects.Shared;
using System.Text.RegularExpressions;
using ngBatchProcesses.BusinessObjects.Axis;
using netGiant.Intranet.DataLayer;
using System.Data.Entity;

namespace ngBatchProcesses.BusinessObjects.Provider
{
    public class ProviderInventory
    {
        /// <summary>
        /// Step 1: Download csv/xlsx file for the supplier using it's ftp details and field mappings into working folder
        /// Step 2: Upload data from all files into one merged (provider.csv) file in merged folder
        /// Step 3: Insert each row in provider.csv into PMS tables if they don't exists
        /// *************************************************************************************************************
        /// To download provider's file and data, machine should have the following folder structure:
        /// C:\\ProviderInventory
        /// C:\\ProviderInventory\Working
        /// C:\\ProviderInventory\Archive
        /// C:\\ProviderInventory\Merged
        /// </summary>
        /// 

        public ProviderInventory()
        {
            hasErrorOccured = false;
        }

        public static bool hasErrorOccured;

        public static void PopulateProviderInventory(Dictionary<string, string> parms)
        {
            //If an integer value is passed in subtype, then a single provider has been requested.
            int singleProvider = 0;
            if (parms["subtype"] != string.Empty)
            {
                int.TryParse(parms["subtype"], out singleProvider);
            }

            DataTable dtAllSuppliersData = new DataTable();
            dtAllSuppliersData.Columns.Add("spn", typeof(string));
            dtAllSuppliersData.Columns.Add("mfpn", typeof(string));
            dtAllSuppliersData.Columns.Add("price", typeof(double));
            dtAllSuppliersData.Columns.Add("quantity", typeof(double));
            dtAllSuppliersData.Columns.Add("description", typeof(string));
            dtAllSuppliersData.Columns.Add("providerFK", typeof(Int32));
            dtAllSuppliersData.Columns.Add("manufacturerFK", typeof(string));
            dtAllSuppliersData.Columns.Add("supManuRef", typeof(string));
            dtAllSuppliersData.Columns.Add("barcode", typeof(string));

            DataTable dtSupplierData = new DataTable();
            ProviderServices svcProvider = new ProviderServices(Convert.ToInt32(parms["input"]));
            StandardFunctions stnFunc = new StandardFunctions();
            Properties.Settings settings = Properties.Settings.Default;

            stnFunc.AddToActivityLog("Started Batch Program with switch: updateproviderinventory - "
                                        + settings.Environment + Environment.NewLine);

            string providerWorkingPath = settings.LocalDirectory + "ProviderInventory\\Working\\";
            string providerArchivePath = settings.LocalDirectory + "ProviderInventory\\Archive\\";
            string providerMergedPath = settings.LocalDirectory + "ProviderInventory\\Merged\\";

            //Get providers Data
            GetProviderData(svcProvider, stnFunc, settings, dtSupplierData, dtAllSuppliersData, singleProvider);

            //Create merged csv file for all providers
            string mergeCSVFilePath = CreateMergedCSV(dtAllSuppliersData, settings.Environment, settings.SQLServerFilePath, stnFunc);

            //Populate provider inventory
            PopulateProviderInventory(settings.SQLServerLocalDirectory, svcProvider, stnFunc);

            //Archive file
            stnFunc.CopyFileAndDelete(mergeCSVFilePath, providerArchivePath);

            //Clear working directory
            stnFunc.ClearExtractedFiles(providerWorkingPath);

            //Clear archive folder, keep only the lates 5 files
            stnFunc.CleanupArchiveLocationByNumber(providerArchivePath, 5);

            //Populate potential new products
            PopulatePotentialNewProds(stnFunc);

            UpdateSupplierStock(stnFunc);

            SetProviderAlertStatus(stnFunc);

            stnFunc.AddToActivityLog("Finished Batch Program switch: updateproviderinventory - " + settings.Environment);
            string acitivityLogFileName = stnFunc.LogActivity("updateproviderinventory");

            if (hasErrorOccured && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail("Updated providers data", acitivityLogFileName, additionalEmails);
            }

            stnFunc = null;

            //Calculate the latest prices
            //PricingEngine.PriceRule.ProcessPricingRules();
            //PricingEngine.PriceRule.ProcessPricingRules();

            //Run the Axis Queue
            //RunAxisQueue(settings, parms, stnFunc);
        }

        private static void RunAxisQueue(Properties.Settings settings,
            Dictionary<string, string> piParams, StandardFunctions stnFunc)
        {
            try
            {
                XMLFeedV2.ProcessAxisQueue();
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error runing axis queue - " + ex.Message);
            }
        }

        private static void GetProviderData(ProviderServices svcProvider, StandardFunctions stnFunc, Properties.Settings settings,
            DataTable dtSupplierData, DataTable dtAllSuppliersData, int singleProvider)
        {
            string workingFolder = settings.LocalDirectory + "ProviderInventory\\Working\\";

            DataTable dtMfpnExtensions = new DataTable();
            dtMfpnExtensions.Columns.Add("manuID", typeof(int));
            dtMfpnExtensions.Columns.Add("extension", typeof(string));

            foreach (NGBP.DataAccessLayer.SCOM.SimpleEntities.mfpnExtensions exten in svcProvider.GetAllMFPNExtensions())
            {
                DataRow mfpnRow = dtMfpnExtensions.NewRow();
                mfpnRow[0] = exten.ManuID;
                mfpnRow[1] = exten.Extension;
                dtMfpnExtensions.Rows.Add(mfpnRow);
            }

            DataTable dtManufacturers = new DataTable();
            dtManufacturers.Columns.Add("manuID", typeof(int));
            dtManufacturers.Columns.Add("manufacturerName", typeof(string));

            foreach (NGBP.DataAccessLayer.SCOM.SimpleEntities.manufacturer manu in svcProvider.GetAllManufacturers())
            {
                DataRow manufacturerRow = dtManufacturers.NewRow();
                manufacturerRow[0] = manu.ManuID;
                manufacturerRow[1] = manu.ManufacturerName;
                dtManufacturers.Rows.Add(manufacturerRow);
            }

            DataTable dtSupplierManuMappings = new DataTable();
            dtSupplierManuMappings.Columns.Add("supplierManuRef", typeof(string));
            dtSupplierManuMappings.Columns.Add("manufacturerFK", typeof(int));
            dtSupplierManuMappings.Columns.Add("providerFK", typeof(int));

            foreach (SupplierManuMapping map in svcProvider.GetAllSupplierManuMappings())
            {
                DataRow supRow = dtSupplierManuMappings.NewRow();
                supRow[0] = map.SupplierManuRef;
                supRow[1] = map.ManufacturerFK;
                supRow[2] = map.ProviderFK;
                dtSupplierManuMappings.Rows.Add(supRow);
            }

            //Check whether the user has requested just one provider inventory update.
            List<ProviderSE> provList = new List<ProviderSE>();
            if (singleProvider > 0)
            {
                provList = svcProvider.GetAllProviders().Where(x => x.ProviderID == singleProvider).ToList();
            }
            else
            {
                provList = svcProvider.GetAllProviders();
            }

            foreach (ProviderSE prov in provList)
            {
                //Fetch data for those providers that have the ftp details
                if (prov.RelatedFtpDetails.FtpDetailID > 0)
                {
                    stnFunc.AddToActivityLog("Started processing feed for provider: " + prov.ProviderName);

                    string workingFilePath = string.Format("{0}{1}", workingFolder, prov.RelatedFtpDetails.FtpFilename);
                    string providerDelimiter = string.Empty;
                    string providerFtpFolder = !string.IsNullOrEmpty(prov.RelatedFtpDetails.FtpFolder.Trim()) ?
                        string.Format("//{0}//", prov.RelatedFtpDetails.FtpFolder.Trim()) : string.Empty;
                    string providerFtpDirectory = prov.RelatedFtpDetails.FtpHost + providerFtpFolder;

                    //Get provider file[s]
                    List<string> providerFilePaths = new List<string>();
                    if (!string.IsNullOrEmpty(prov.RelatedFtpDetails.FtpZipFilename))
                    {
                        providerFilePaths.Add(string.Format("{0}{1}", workingFolder, prov.RelatedFtpDetails.FtpZipFilename));
                    }
                    providerFilePaths.Add(workingFilePath);

                    #region Step 1: Download ftp file

                    try
                    {
                        bool fileExists = false;

                        foreach (string path in providerFilePaths)
                        {
                            if (File.Exists(path)) fileExists = true;
                        }

                        if (!fileExists)
                        {
                            string fileNameToUse = string.IsNullOrEmpty(prov.RelatedFtpDetails.FtpZipFilename) ?
                                                    prov.RelatedFtpDetails.FtpFilename : prov.RelatedFtpDetails.FtpZipFilename;

                            FtpUtilities.DownloadFTPFiles(providerFtpDirectory, prov.RelatedFtpDetails.FtpUser,
                                                prov.RelatedFtpDetails.FtpPassword, workingFolder, fileNameToUse);
                            fileExists = true;
                            stnFunc.AddToActivityLog("Successfully downloaded FTP file: " + fileNameToUse);

                            //Update the provider feed DateTime 
                            ProviderServices.UpdateProviderFeedDateTime(prov.ProviderID.ToString(),
                                                                FtpUtilities.FileLastModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"));

                            stnFunc.AddToActivityLog("Successfully updated the provider feed file datetime");
                        }

                        //Exract zip file
                        if (fileExists)
                        {
                            foreach (string path in providerFilePaths)
                            {
                                //check if it's already not been extracted
                                if (Path.GetExtension(path).Equals(".zip", StringComparison.InvariantCultureIgnoreCase) && File.Exists(path))
                                {
                                    ZipFile.ExtractToDirectory(path, workingFolder);
                                    File.Delete(path);
                                    stnFunc.AddToActivityLog("Successfully extracted ZIP file: " + Path.GetFileName(path));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        stnFunc.AddToActivityLog(string.Format("Unable to download the FTP file for host:{0} - user:{1} \r\n errorMessage:{2}",
                            prov.RelatedFtpDetails.FtpHost, prov.RelatedFtpDetails.FtpUser, ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace));
                        hasErrorOccured = true;
                    }

                    #endregion

                    #region Step 2: Download data from file

                    if (File.Exists(workingFilePath))
                    {
                        using (StreamReader reader = new StreamReader(workingFilePath))
                        {
                            //Detect delimiter
                            try
                            {
                                providerDelimiter = FtpUtilities.DetectDelimiter(reader, File.ReadAllLines(workingFilePath).Count()).ToString();
                                stnFunc.AddToActivityLog("Successfully identified delimeter as: " + providerDelimiter);
                            }
                            catch (Exception ex)
                            {
                                stnFunc.AddToActivityLog(string.Format("Unable to identify the delimiter for {0}. \r\n Error: {1}",
                                    workingFilePath, ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace));
                                stnFunc.AddToActivityLog("Provider: " + prov.ProviderName);
                                hasErrorOccured = true;
                            }
                        }

                        try
                        {
                            ////Loading into a temp datatable here fixes column type issues as we define the types for each column
                            DataTable tempDt = new DataTable();
                            dtSupplierData.Columns.Add("spn", typeof(string));
                            dtSupplierData.Columns.Add("mfpn", typeof(string));
                            dtSupplierData.Columns.Add("price", typeof(double));
                            dtSupplierData.Columns.Add("quantity", typeof(double));
                            dtSupplierData.Columns.Add("description", typeof(string));
                            dtSupplierData.Columns.Add("providerFK", typeof(Int32));
                            dtSupplierData.Columns.Add("manufacturerFK", typeof(string));
                            dtSupplierData.Columns.Add("supManuRef", typeof(string));
                            dtSupplierData.Columns.Add("barcode", typeof(string));

                            switch (prov.ProviderType.ProviderTypeID)
                            {
                                case 1:

                                    writeSchema(workingFolder, Path.GetFileName(workingFilePath), providerDelimiter.Equals("\t") ? "TabDelimited" :
                                        "CSVDelimited", prov.RelatedFtpDetails.FileColumnHeader, stnFunc);
                                    tempDt = xlsData(workingFilePath, Path.GetFileName(workingFilePath), prov.ProviderID, stnFunc, prov, dtManufacturers);

                                    break;
                                case 2:

                                    bool hasQuotes = CheckForFieldQuotes(workingFilePath, prov.RelatedFtpDetails.FileColumnHeader, stnFunc, prov);
                                    tempDt = DownloadCSVData(workingFilePath, prov.ProviderID,
                                                        prov.RelatedFtpDetails.FileColumnHeader, stnFunc, providerDelimiter, hasQuotes, prov,
                                                        dtMfpnExtensions, dtSupplierManuMappings);

                                    break;
                                case 4:

                                    bool hasQuotes2 = CheckForFieldQuotes(workingFilePath, prov.RelatedFtpDetails.FileColumnHeader, stnFunc, prov);
                                    tempDt = ProcessMultiCompetitorFile(workingFilePath, Path.GetFileName(workingFilePath),
                                                prov.ProviderID, stnFunc, prov, dtManufacturers, providerDelimiter, hasQuotes2, prov.RelatedFtpDetails.FileColumnHeader);

                                    break;
                            }

                            ////Fetch xls data
                            //if (Path.GetExtension(workingFilePath).Equals(".xls", StringComparison.InvariantCultureIgnoreCase))
                            //{
                            //    writeSchema(workingFolder, Path.GetFileName(workingFilePath), providerDelimiter.Equals("\t") ? "TabDelimited" :
                            //        "CSVDelimited", prov.RelatedFtpDetails.FileColumnHeader, stnFunc);
                            //    tempDt = xlsData(workingFilePath, Path.GetFileName(workingFilePath), prov.ProviderID, stnFunc, prov, dtManufacturers);
                            //}
                            ////Fetch csv data
                            //else
                            //{
                            //    bool hasQuotes = CheckForFieldQuotes(workingFilePath, prov.RelatedFtpDetails.FileColumnHeader, stnFunc, prov);
                            //    tempDt = DownloadCSVData(workingFilePath, prov.ProviderID,
                            //                    prov.RelatedFtpDetails.FileColumnHeader, stnFunc, providerDelimiter, hasQuotes, prov,
                            //                    dtMfpnExtensions, dtSupplierManuMappings);
                            //}

                            dtSupplierData.Load(tempDt.CreateDataReader(), LoadOption.OverwriteChanges);
                            tempDt = null;
                        }
                        catch (Exception ex)
                        {
                            stnFunc.AddToActivityLog(string.Format("Unable to fetch data from csv/xls file {0}. \r\n Error: {1}",
                                    workingFilePath, ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace));
                            stnFunc.AddToActivityLog("Provider: " + prov.ProviderName);
                            hasErrorOccured = true;
                        }

                        bool mappingError = false;

                        try
                        {
                            //Re-arrange columns
                            //dtSupplierData.Rows[0].Delete();
                            dtSupplierData.Columns["spn"].SetOrdinal(0);
                            dtSupplierData.Columns["mfpn"].SetOrdinal(1);
                            dtSupplierData.Columns["price"].SetOrdinal(2);
                            dtSupplierData.Columns["quantity"].SetOrdinal(3);
                            dtSupplierData.Columns["providerFK"].SetOrdinal(4);
                            dtSupplierData.Columns["manufacturerFK"].SetOrdinal(5);
                            dtSupplierData.Columns["description"].SetOrdinal(6);
                            dtSupplierData.Columns["supManuRef"].SetOrdinal(7);
                            dtSupplierData.Columns["barcode"].SetOrdinal(8);
                        }
                        catch (Exception ex)
                        {
                            stnFunc.AddToActivityLog(string.Format("**Error** Field mapping error has occurred - Filename: {0} " +
                                                    "Provider {1}. {2} Error: {3}", prov.RelatedFtpDetails.FtpFilename,
                                prov.ProviderName, ex.Message, ex.StackTrace));
                            mappingError = true;
                            hasErrorOccured = true;
                        }

                        try
                        {
                            if (!mappingError)
                            {
                                dtAllSuppliersData.Merge(dtSupplierData);
                                dtSupplierData = new DataTable();
                            }
                        }
                        catch (Exception ex)
                        {
                            stnFunc.AddToActivityLog(string.Format("**Error** Unable to merge all providers data." +
                                                    " - Merge error for Filename: {0} " +
                                                    "Provider {1}. {2} Error: {3}", prov.RelatedFtpDetails.FtpFilename,
                                prov.ProviderName, ex.Message, ex.StackTrace));
                            hasErrorOccured = true;
                        }
                    }

                    stnFunc.AddToActivityLog("Finished processing feed for provider: " + prov.ProviderName +
                                                "\r\n\r\n---------------------------------------------------\r\n");
                    #endregion
                }
            }

            provList = null;
        }

        public static void writeSchema(string workingPath, string filename, string format, bool hasColumnHeader, StandardFunctions stnFunc)
        {
            try
            {
                using (FileStream fsOutput = new FileStream(workingPath + "\\schema.ini", FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter srOutput = new StreamWriter(fsOutput))
                    {
                        string charSet = "CharacterSet=OEM";

                        srOutput.WriteLine(string.Format(
                            "[{0}]\nColNameHeader={1}\nFormat={2}\nMaxScanRows={3}\n{4}", filename, hasColumnHeader ? "True" : "False",
                            format, 0, charSet));
                    }
                }
                stnFunc.AddToActivityLog("Successfully written schema file");
            }

            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error writing schema for provider's csv - " + ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace);
                hasErrorOccured = true;
            }
        }

        public static DataTable xlsData(string downloadedFilePath, string fileName, int providerID, StandardFunctions stnFunc,
                                        ProviderSE prov, DataTable dtManus)
        {
            DataTable dt = new DataTable();
            ProviderServices svcProvider = new ProviderServices();
            List<string> mappedColumns = new List<string>();
            string[] desiredPMSColumns = { "partNo", "price", "quantity", "description", "providerFK" };
            List<KeyValuePair<string, string>> fieldMappings = svcProvider.GetFieldMappingsByProvider(providerID);
            //var connectionString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0; data source={0}; Extended Properties=Excel 8.0;", downloadedFilePath);
            var connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0; data source={0}; Extended Properties=Excel 8.0;", downloadedFilePath);

            try
            {
                var adapter = new OleDbDataAdapter(string.Format("SELECT DISTINCT {0}, {1}, {2} FROM [MatchedProductsPriceStatus$] WHERE LEN(RTRIM(LTRIM({1}))) > 0 ORDER BY {1} DESC",
                    fieldMappings.FirstOrDefault(x => x.Key == "mfpn").Value, fieldMappings.FirstOrDefault(x => x.Key == "price").Value,
                    fieldMappings.FirstOrDefault(x => x.Key == "description").Value), connectionString);

                adapter.Fill(dt);

                dt = new DataView(dt).ToTable(false, mappedColumns.ToArray());

                //Add relevant provider
                DataColumn provider = new DataColumn("providerFK", typeof(Int32));
                provider.DefaultValue = providerID;
                dt.Columns.Add(provider);

                //Add quantity column in case of competitors and set to zero for merging purposes
                if (!dt.Columns.Contains("quantity"))
                {
                    DataColumn quantity = new DataColumn("quantity", typeof(string));
                    quantity.DefaultValue = 0;
                    dt.Columns.Add(quantity);
                }

                //Create mapping list
                foreach (KeyValuePair<string, string> mapp in fieldMappings)
                {
                    mappedColumns.Add(mapp.Value);
                }

                DataColumn manufacturerFK = new DataColumn("manufacturerFK", typeof(string));
                manufacturerFK.DefaultValue = string.Empty;
                dt.Columns.Add(manufacturerFK);

                DataColumn supManuRef = new DataColumn("supManuRef", typeof(string));
                supManuRef.DefaultValue = string.Empty;
                dt.Columns.Add(supManuRef);

                DataColumn providerPartNo = new DataColumn("spn", typeof(string));
                providerPartNo.DefaultValue = string.Empty;
                dt.Columns.Add(providerPartNo);

                //Change the column name to the pms columns
                foreach (string mapp in mappedColumns)
                {
                    dt.Columns[mapp].ColumnName = fieldMappings.FirstOrDefault(x => x.Value == mapp).Key;
                }

                //Store Duplicates
                Hashtable hTable = new Hashtable();
                List<DataRow> duplicatesList = new List<DataRow>();

                foreach (DataRow xlsRow in dt.Rows)
                {
                    xlsRow["spn"] = xlsRow["mfpn"];
                    RemoveSpecialCharacters(xlsRow);

                    if (hTable.Contains(xlsRow["spn"]))
                    {
                        duplicatesList.Add(xlsRow);
                    }
                    else
                    {
                        hTable.Add(xlsRow["spn"], string.Empty);
                    }
                }

                hTable = null;

                foreach (DataRow dRow in duplicatesList)
                {
                    stnFunc.AddToActivityLog("**Warning** This Ref - " + dRow["mfpn"] +
                                            " exists multiple times in this file: " + prov.RelatedFtpDetails.FtpFilename +
                                            " for supplier: " + prov.ProviderName);
                    //hasErrorOccured = true;
                    dt.Rows.Remove(dRow);
                }

                duplicatesList = null;

            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** reading provider's excel file: " + prov.RelatedFtpDetails.FtpFilename +
                                            " Provider: " + prov.ProviderName);
                stnFunc.AddToActivityLog(ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace);
                hasErrorOccured = true;
            }

            return dt;
        }

        public static string CreateMergedCSV(DataTable dt, string evnvironment, string sqlFilePath, StandardFunctions stnFunc)
        {
            string mergedCSVFilename = "provider.csv";
            string mergeCSVFilePath = sqlFilePath + "Provider\\" + mergedCSVFilename;

            try
            {
                StringBuilder sb = new StringBuilder();

                IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dt.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(mergeCSVFilePath, sb.ToString());
                stnFunc.AddToActivityLog("Successfully created merged csv file: " + mergeCSVFilePath + "");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Unable to merge data into one provider.csv file.\r\nFailed to create merged provider.csv" + ex.Message);
                hasErrorOccured = true;
            }

            return mergeCSVFilePath;
        }

        private static void PopulateProviderInventory(string sqlLocalDirectory, ProviderServices svcProvider, StandardFunctions stnFunc)
        {
            try
            {
                svcProvider.CopyProviderData(sqlLocalDirectory + "Provider\\provider.csv");
                stnFunc.AddToActivityLog("Executed stored procedure to populate provider inventory");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Unable to copy providers data into PMS. CopyProviderData failed from merged provider.csv file. \r\n" + ex.Message);
                hasErrorOccured = true;
            }
        }

        private static void PopulatePotentialNewProds(StandardFunctions stnFunc)
        {
            try
            {
                SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", "ngmd.PopulatePotentialNewProds", 180);
                stnFunc.AddToActivityLog("Successfully executed stored procedure to populate Potential New Products");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Unable to populate potential new products into PMS. Populate potentail new products failed. \r\n" + ex.Message + ex.InnerException + ex.StackTrace);
                hasErrorOccured = true;
            }
        }

        private static void UpdateSupplierStock(StandardFunctions stnFunc)
        {
            try
            {
                ProviderServices.UpdateSupplierStockQuantity();
                stnFunc.AddToActivityLog("Successfully updated the stock quantities in the product table");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Unable to update the supplier stock column for products - " + ex.Message);
                stnFunc.AddToActivityLog(ex.StackTrace);
                hasErrorOccured = true;
            }
        }

        private static void SetProviderAlertStatus(StandardFunctions stnFunc)
        {
            try
            {
                ProviderServices.SetProvidersAlertStatus();
                stnFunc.AddToActivityLog("Successfully set the provider alert status for products");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Unable to set the alert status for products - " + ex.Message);
                stnFunc.AddToActivityLog(ex.StackTrace);
                hasErrorOccured = true;
            }
        }

        public static DataTable DownloadCSVData(string workingPath, int providerID, bool fileColumnHeader, StandardFunctions stnFunc,
            string delimiter, bool hasFieldsInQuotes, ProviderSE prov, DataTable dtManusExtensions,
            DataTable dtSupManMappings)
        {
            List<KeyValuePair<string, string>> mappedColumns = new List<KeyValuePair<string, string>>();

            //Get field mappings for the provider
            ProviderServices svcProvider = new ProviderServices();
            List<KeyValuePair<string, string>> fieldMappings = svcProvider.GetFieldMappingsByProvider(providerID);

            //Desired columns which pms needs from the provider's file
            string[] desiredPMSColumns = { "spn", "mfpn", "price", "quantity", "description", "supManuRef", "providerFK", "barcode" };

            //Download csv data
            DataTable csvData = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(workingPath))
                {
                    csvReader.SetDelimiters(new string[] { delimiter });
                    csvReader.HasFieldsEnclosedInQuotes = hasFieldsInQuotes;
                    csvReader.TrimWhiteSpace = true;

                    //read column headers
                    string[] colFields = csvReader.ReadFields();
                    if (fileColumnHeader)
                    {
                        //add first row as column headers
                        foreach (string column in colFields)
                        {
                            DataColumn datecolumn = new DataColumn(column);
                            datecolumn.AllowDBNull = true;
                            csvData.Columns.Add(datecolumn);
                        }
                    }
                    else
                    {
                        //add first row with column indexes
                        int columnIndex = 0;
                        foreach (string column in colFields)
                        {
                            DataColumn datecolumn = new DataColumn("C" + columnIndex);
                            csvData.Columns.Add(datecolumn);
                            columnIndex++;
                        }

                        //add first row
                        csvData.Rows.Add(colFields);
                    }

                    //column data
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            string[] fieldData = csvReader.ReadFields();
                            //Making empty value as null
                            for (int i = 0; i < fieldData.Length; i++)
                            {
                                if (fieldData[i] == "")
                                {
                                    fieldData[i] = null;
                                }
                            }
                            csvData.Rows.Add(fieldData);
                        }
                        catch (Exception ex)
                        {
                            stnFunc.AddToActivityLog("Could not read a line in file: " + ex.Message);
                        }
                    }
                }

                //Add relevant provider column into the datatable
                DataColumn provider = new DataColumn("providerFK", typeof(Int32));
                provider.DefaultValue = providerID;
                csvData.Columns.Add(provider);

                bool createManuRef = false;
                bool containsManuRef = fieldMappings.Contains(fieldMappings.FirstOrDefault(m => m.Key == "supManuRef"));

                if (containsManuRef)
                {
                    if (string.IsNullOrEmpty(fieldMappings.First(x => x.Key == "supManuRef").Value))
                    {
                        createManuRef = true;
                    }
                }
                else
                {
                    createManuRef = true;
                }

                if (createManuRef)
                {
                    DataColumn manuRef = new DataColumn("supManuRef", typeof(string));

                    //If badger or Jettec or Inkjet Direct
                    if (prov.ProviderID == 5 || prov.ProviderID == 9 || prov.ProviderID == 25)
                    {
                        manuRef.DefaultValue = "Own Brand";
                    }
                    else
                    {
                        manuRef.DefaultValue = string.Empty;
                    }

                    csvData.Columns.Add(manuRef);
                }

                bool spn_mfpn_same = false;
                if (fieldMappings.First(x => x.Key == "spn").Value == fieldMappings.First(x => x.Key == "mfpn").Value)
                {
                    fieldMappings.Remove(fieldMappings.First(x => x.Key == "mfpn"));
                    DataColumn mfpnColumn = new DataColumn("mfpn", typeof(string));
                    mfpnColumn.DefaultValue = string.Empty;
                    csvData.Columns.Add(mfpnColumn);

                    spn_mfpn_same = true;
                }

                //retrieve the desired columns
                DataColumnCollection columns = csvData.Columns;
                foreach (string col in desiredPMSColumns)
                {
                    if (columns.Contains(col))
                    {
                        mappedColumns.Add(new KeyValuePair<string, string>(col, col));
                    }
                    else
                    {
                        try
                        {
                            int colHeader = 0;
                            string colName = fieldMappings.First(x => x.Key == col).Value;
                            bool tryConvert = int.TryParse(colName, out colHeader);

                            if (tryConvert)
                            {
                                csvData.Columns[colHeader].ColumnName = col;
                                mappedColumns.Add(new KeyValuePair<string, string>(col, col));
                            }
                            else
                            {
                                mappedColumns.Add(new KeyValuePair<string, string>(col, fieldMappings.First(x => x.Key == col).Value));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (col != "barcode")
                            {
                                stnFunc.AddToActivityLog(
                                    $"Error: mapping does not exist for column {col} and provider {svcProvider.GetAllProviders().FirstOrDefault(x => x.ProviderID == providerID).ProviderName}\r\n");
                                stnFunc.AddToActivityLog("Provider: " + prov.ProviderName);
                                stnFunc.AddToActivityLog(ex.InnerException.ToString());
                                hasErrorOccured = true;
                            }
                        }
                    }
                }

                //Get only desired columns
                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());

                //Change column names to PMS columns
                foreach (DataColumn col in csvData.Columns)
                {
                    csvData.Columns[col.ColumnName].ColumnName = mappedColumns.FirstOrDefault(x => x.Value.Equals(col.ColumnName, StringComparison.InvariantCultureIgnoreCase)).Key;
                }

                csvData.Columns.Add("manufacturerFK");

                //This is here because not every supplier has a barcode, so adds the column if it's not mapped
                if (mappedColumns.FirstOrDefault(x => x.Key == "barcode").Value == null)
                    csvData.Columns.Add("barcode");

                //Store Duplicates
                Hashtable hTable = new Hashtable();
                List<DataRow> duplicatesList = new List<DataRow>();

                foreach (DataRow csvRow in csvData.Rows)
                {
                    //if (csvRow["mfpn"].ToString().Contains("46M0001"))
                    //{
                    //    var test = "";
                    //}

                    int manuFK = ReplaceManuRef(csvRow, dtSupManMappings, prov);
                    RemovePartNumberExtensions(csvRow, spn_mfpn_same, dtManusExtensions, manuFK, csvData);
                    RemoveSpecialCharacters(csvRow);

                    if (hTable.Contains(csvRow["spn"]))
                    {
                        duplicatesList.Add(csvRow);
                    }
                    else
                    {
                        hTable.Add(csvRow["spn"], string.Empty);
                    }
                }

                hTable = null;

                foreach (DataRow dRow in duplicatesList)
                {
                    stnFunc.AddToActivityLog("**Warning** This Ref - " + dRow["mfpn"] +
                                            " exists multiple times in this file: " + prov.RelatedFtpDetails.FtpFilename +
                                            " for supplier: " + prov.ProviderName);
                    //hasErrorOccured = true;
                    csvData.Rows.Remove(dRow);
                }

                duplicatesList = null;
                //csvData.Columns["supManuRef"].ColumnName = "manufacturerFK";

            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** has ocurred whilst reading filename: " + prov.RelatedFtpDetails.FtpFilename +
                                            " Provider: " + prov.ProviderName);
                stnFunc.AddToActivityLog(ex.Message + ex.StackTrace);
                hasErrorOccured = true;
            }

            return csvData;
        }

        private static int ReplaceManuRef(DataRow dr, DataTable dtSupManMappings, ProviderSE prov)
        {
            string supManuRef = dr["supManuRef"].ToString();
            string mfpn = dr["mfpn"].ToString();
            int manuFK = 0;

            if (supManuRef.Length > 0)
            {
                try
                {
                    var selectManuFK = dtSupManMappings.Select("supplierManuRef = '" + supManuRef + "'AND providerFK = " +
                                                prov.ProviderID + "").FirstOrDefault();

                    if (selectManuFK != null)
                    {
                        manuFK = Convert.ToInt32(selectManuFK[1].ToString());
                    }
                }
                catch (Exception ex)
                {
                    var mess = ex.Message;
                }
            }

            if (manuFK > 0)
            {
                dr["manufacturerFK"] = manuFK;
            }
            else
            {
                dr["manufacturerFK"] = string.Empty;
            }

            return manuFK;
        }

        private static void RemovePartNumberExtensions(DataRow dr, bool spn_mfpn_same, DataTable dtManusExtensions, int manuFK,
                                                        DataTable csvData)
        {
            string spn = dr["spn"].ToString();

            if (spn_mfpn_same)
            {
                dr["mfpn"] = spn;
            }

            string mfpn = dr["mfpn"].ToString();

            foreach (DataRow extenRow in dtManusExtensions.Rows)
            {
                int manuID = Convert.ToInt32(extenRow[0]);
                string extension = extenRow[1].ToString();

                if (manuID == manuFK)
                {
                    if (mfpn.Contains(extension))
                    {
                        dr["mfpn"] = mfpn.Replace(extension, "");
                    }
                }
            }
        }

        private static void RemoveSpecialCharacters(DataRow dr)
        {
            dr["description"] = dr["description"].ToString().Replace(",", " ");
            dr["mfpn"] = dr["mfpn"].ToString().Replace(",", "");
            dr["price"] = Regex.Replace(dr["price"].ToString(), @"[^\w\.@-]", "",
                                RegexOptions.None, TimeSpan.FromSeconds(1.5));

            if (dr["price"].ToString().Trim().Length == 0)
            {
                dr["price"] = 0;
            }

        }

        public static bool CheckForFieldQuotes(string filePath, bool fileColumnHeaders, StandardFunctions stnFun, ProviderSE prov)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                string line = "";

                line = fileColumnHeaders ? lines[1] : lines[0];

                bool containsQuote = false;
                foreach (string item in lines)
                {
                    if (item.Contains('"'))
                    {
                        containsQuote = true;
                        break;
                    }
                }

                lines = null;
                //return line.Contains('"') ? true : false;
                return containsQuote;
            }
            catch
            {
                stnFun.AddToActivityLog("**Error** checking if fields contain quotes for filename: " +
                                        prov.RelatedFtpDetails.FtpFilename + " Provider: " + prov.ProviderName);
                hasErrorOccured = true;
                return false;
            }

        }

        public static DataTable ProcessMultiCompetitorFile(string downloadedFilePath, string fileName, int providerID, StandardFunctions stnFunc,
                                        ProviderSE prov, DataTable dtManus, string delimiter, bool hasFieldsInQuotes, bool fileColumnHeader)
        {
            ProviderServices svcProvider = new ProviderServices();
            List<KeyValuePair<string, string>> mappedColumns = new List<KeyValuePair<string, string>>();
            string[] desiredPMSColumns = { "mfpn", "price", "description", "competitor", "reviewtotal", "reviewrating" };
            List<KeyValuePair<string, string>> fieldMappings = svcProvider.GetFieldMappingsByProvider(providerID);
            DataTable dt = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(downloadedFilePath))
                {
                    csvReader.SetDelimiters(new string[] { delimiter });
                    csvReader.HasFieldsEnclosedInQuotes = hasFieldsInQuotes;
                    csvReader.TrimWhiteSpace = true;

                    //read column headers
                    string[] colFields = csvReader.ReadFields();
                    if (fileColumnHeader)
                    {
                        //add first row as column headers
                        foreach (string column in colFields)
                        {
                            DataColumn datecolumn = new DataColumn(column);
                            datecolumn.AllowDBNull = true;
                            dt.Columns.Add(datecolumn);
                        }
                    }
                    else
                    {
                        //add first row with column indexes
                        int columnIndex = 0;
                        foreach (string column in colFields)
                        {
                            DataColumn datecolumn = new DataColumn("C" + columnIndex);
                            dt.Columns.Add(datecolumn);
                            columnIndex++;
                        }

                        //add first row
                        dt.Rows.Add(colFields);
                    }

                    //column data
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            string[] fieldData = csvReader.ReadFields();
                            //Making empty value as null
                            for (int i = 0; i < fieldData.Length; i++)
                            {
                                if (fieldData[i] == "")
                                {
                                    fieldData[i] = null;
                                }
                            }
                            dt.Rows.Add(fieldData);
                        }
                        catch (Exception ex)
                        {
                            stnFunc.AddToActivityLog("Could not read a line in file: " + ex.Message);
                        }
                    }
                }

                //retrieve the desired columns
                DataColumnCollection columns = dt.Columns;
                foreach (string col in desiredPMSColumns)
                {
                    if (columns.Contains(col))
                    {
                        mappedColumns.Add(new KeyValuePair<string, string>(col, col));
                    }
                    else
                    {
                        try
                        {
                            int colHeader = 0;
                            string colName = fieldMappings.First(x => x.Key == col).Value;
                            bool tryConvert = int.TryParse(colName, out colHeader);

                            if (tryConvert)
                            {
                                dt.Columns[colHeader].ColumnName = col;
                                mappedColumns.Add(new KeyValuePair<string, string>(col, col));
                            }
                            else
                            {
                                mappedColumns.Add(new KeyValuePair<string, string>(col, fieldMappings.First(x => x.Key == col).Value));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (col != "barcode")
                            {
                                stnFunc.AddToActivityLog(
                                    $"Error: mapping does not exist for column {col} and provider {svcProvider.GetAllProviders().FirstOrDefault(x => x.ProviderID == providerID).ProviderName}\r\n");
                                stnFunc.AddToActivityLog("Provider: " + prov.ProviderName);
                                stnFunc.AddToActivityLog(ex.InnerException.ToString());
                                hasErrorOccured = true;
                            }
                        }
                    }
                }

                //Get only desired columns
                dt = new DataView(dt).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());

                //Change column names to PMS columns
                foreach (DataColumn col in dt.Columns)
                {
                    dt.Columns[col.ColumnName].ColumnName = mappedColumns.FirstOrDefault(x => x.Value.Equals(col.ColumnName, StringComparison.InvariantCultureIgnoreCase)).Key;
                }

                //Add relevant provider
                DataColumn provider = new DataColumn("providerFK", typeof(Int32));
                provider.DefaultValue = providerID;
                dt.Columns.Add(provider);

                //Add quantity column in case of competitors and set to zero for merging purposes
                if (!dt.Columns.Contains("quantity"))
                {
                    DataColumn quantity = new DataColumn("quantity", typeof(string));
                    quantity.DefaultValue = 0;
                    dt.Columns.Add(quantity);
                }

                DataColumn manufacturerFK = new DataColumn("manufacturerFK", typeof(string));
                manufacturerFK.DefaultValue = string.Empty;
                dt.Columns.Add(manufacturerFK);

                DataColumn supManuRef = new DataColumn("supManuRef", typeof(string));
                supManuRef.DefaultValue = string.Empty;
                dt.Columns.Add(supManuRef);

                DataColumn providerPartNo = new DataColumn("spn", typeof(string));
                providerPartNo.DefaultValue = string.Empty;
                dt.Columns.Add(providerPartNo);

                //Update competitors table and return latest from database
                string[] cols = {"competitor", "reviewtotal", "reviewrating"};
                var view = new DataView(dt);
                var distinctValues = view.ToTable(true, cols);
                var competitors = InsertNewCompetitors(distinctValues);

                foreach (DataRow xlsRow in dt.Rows)
                {
                    xlsRow["spn"] = xlsRow["mfpn"];
                    RemoveSpecialCharacters(xlsRow);

                    var competitorName = xlsRow["competitor"].ToString().Length > 45 ? xlsRow["competitor"].ToString().Substring(0, 45) : xlsRow["competitor"].ToString();

                    xlsRow["providerFK"] = competitors
                        .Where(x => x.providerName.ToLower() ==
                        competitorName.ToLower()).First().providerID;
                }

                //Store Duplicates
                var hTable=  new Hashtable();
                var duplicatesList = new List<DataRow>();

                foreach (DataRow csvRow in dt.Rows)
                {
                    if (hTable.Contains(csvRow["spn"].ToString() + csvRow["providerFK"]))
                    {
                        duplicatesList.Add(csvRow);
                    }
                    else
                    {
                        hTable.Add(csvRow["spn"] + csvRow["providerFK"].ToString(), string.Empty);
                    }
                }

                foreach (DataRow dRow in duplicatesList)
                {
                    stnFunc.AddToActivityLog("**Warning** This Ref - " + dRow["mfpn"] +
                                            " exists multiple times in this file: " + prov.RelatedFtpDetails.FtpFilename +
                                            " for supplier: " + prov.ProviderName);
                    //hasErrorOccured = true;
                    dt.Rows.Remove(dRow);
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** reading provider's excel file: " + prov.RelatedFtpDetails.FtpFilename +
                                            " Provider: " + prov.ProviderName);
                stnFunc.AddToActivityLog(ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace);
                hasErrorOccured = true;
            }

            dt.Columns.Remove("competitor");
            dt.Columns.Remove("reviewtotal");
            dt.Columns.Remove("reviewrating");
            
            return dt;
        }

        private static List<provider> InsertNewCompetitors(DataTable dt)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (DataRow item in dt.Rows)
                {
                    var competitor = item["competitor"].ToString();
                    var reviewTotal = item["reviewtotal"].ToString();
                    var reviewRating = item["reviewrating"].ToString();

                    var comp = db.provider
                        .Where(x => x.providerDesc.ToLower() == competitor.ToLower() &&
                        x.providerType.providerTypeID == 5)
                        .FirstOrDefault();

                    int total = 0;
                    int.TryParse(reviewTotal, out total);

                    decimal rating = 0;
                    decimal.TryParse(reviewRating, out rating);

                    if (comp == null)
                    {
                        var provider = new provider()
                        {
                            providerName = competitor.Length > 45 ? competitor.Substring(0, 45) : competitor,
                            providerDesc = competitor,
                            providerTypeFK = 5,
                            dateLastUpdate = DateTime.Now,
                            active = false,
                            reviewTotal = total,
                            reviewRating = rating
                        };

                        db.provider.Add(provider);
                        db.SaveChanges();
                    }
                    else
                    {
                        comp.reviewTotal = total;
                        comp.reviewRating = rating;

                        db.Entry(comp).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }

                return db.provider.Where(x => x.providerTypeFK == 5).ToList();
            }
        }
    }
}
