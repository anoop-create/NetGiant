using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using netGiant.Intranet.DataLayer;
using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public class StandardFunctions
    {
        private List<string> ActivityLogArrayList { get; } = new List<string>();

        public static void SetGlobalUserVariables()
        {
            Properties.Settings.Default.LocalDirectory = GetMachineConfigAppSetting("LocalDirectory"); ;
            Properties.Settings.Default.SQLServerFilePath = GetMachineConfigAppSetting("SQLServerFilePath");
            Properties.Settings.Default.Environment = GetMachineConfigAppSetting("Environment");
            Properties.Settings.Default.SQLServerLocalDirectory = GetMachineConfigAppSetting("SQLServerLocalDirectory");
        }

        internal static void SetPropertySettings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                Dictionary<string, string> configSettings = db.configurationSetting
                                                             .Where(x => x.sectionName == "BatchProgramSetting")
                                                             .ToDictionary(x => x.settingName, x => x.settingValue);

                foreach(KeyValuePair<string,string> item in configSettings)
                {
                    SettingsProperty property = new SettingsProperty(item.Key);
                    property.Provider = Properties.Settings.Default.Providers["LocalFileSettingsProvider"];
                    property.DefaultValue = item.Value;
                    property.PropertyType = typeof(string);
                    property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
                    Properties.Settings.Default.Properties.Add(property);
                }

                Properties.Settings.Default.Save();
                Properties.Settings.Default.Reload();
            }
        }

        public void ExtractZipFile(string src, string dest)
        {
            try
            {
                ClearExtractedFiles(dest);
                string[] directoryList = Directory.GetFiles(src);
                foreach (string file in directoryList)
                {
                    ZipFile.ExtractToDirectory(file, dest);
                    AddToActivityLog("Successfully extracted zip file - " + file + " to - " + dest);
                }
            }
            catch (Exception ex)
            {
                AddToActivityLog("*Error* extracting zip file - " + ex.Message);
            }
        }

        public void ArchiveFile(string src, string dest)
        {
            try
            {
                string[] directoryList = Directory.GetFiles(src);
                foreach (string file in directoryList)
                {
                    File.Move(file, dest + "_" + Path.GetFileName(file));
                    AddToActivityLog("Successfully archived file - " + file);
                }
            }
            catch (Exception ex)
            {
                AddToActivityLog("*Error* archiving file - " + ex.Message);
            }
        }

        public static void ArchiveFile(string path, ref List<string> ActivityLogArrayList, int archiveType)
        {
            try
            {
                string archivePath;
                string logMessage = "";
                switch (archiveType)
                {
                    case 1:
                        archivePath = (string)Properties.Settings.Default["ArchivedFilePath"];
                        logMessage = "Successfully archived file - ";
                        break;
                    case 2:
                        archivePath = (string)Properties.Settings.Default["ErrorFilePath"];
                        logMessage = "Successfully moved file to error directory - ";
                        break;
                    default:
                        archivePath = (string)Properties.Settings.Default["ErrorFilePath"];
                        logMessage = "Successfully moved file to error directory - ";
                        break;
                }

                File.Move(path, archivePath + "\\" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + "_" + Path.GetFileName(path));
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + logMessage + path);
            }
            catch (Exception ex)
            {
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + "**Error** - Could not archive file" +
                                         path + " - Detailed Error = " + ex.Message);
            }
        }

        public void CopyFile(Dictionary<string, string> parms)
        {
            try
            {
                File.Copy(parms["input"],
                    parms["output"]);
            }
            catch (Exception ex)
            {
                AddToActivityLog("*Error* copying file - " + ex.Message);
                LogActivity(parms["type"]);
            }
        }

        public bool CopyDirectory(DirectoryInfo source, DirectoryInfo destination, List<string> exclude, bool copySubDirs = true)
        {
                bool isSuccess = true;
                if (source.Exists)
                {
                    try
                    {
                        if (!destination.Exists)
                        {
                            destination.Create();
                        }

                        // Copy all files.
                        FileInfo[] files = source.GetFiles();
                        foreach (FileInfo file in files)
                        {
                            string destfile = Path.Combine(destination.FullName, file.Name);
                            if (File.Exists(destfile))
                            {
                                FileAttributes att = File.GetAttributes(destfile);
                                if ((att & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                {
                                    // Make the file RW
                                    att = RemoveAttribute(att, FileAttributes.ReadOnly);
                                    File.SetAttributes(destfile, att);
                                }
                            }
                            file.CopyTo(Path.Combine(destination.FullName, file.Name), true);
                        }

                        if (copySubDirs)
                        {
                            // Process subdirectories.
                            DirectoryInfo[] dirs = source.GetDirectories();
                            foreach (DirectoryInfo dir in dirs)
                            {
                                if (exclude != null && exclude.Contains(dir.Name))
                                    continue;
                                // Get destination directory.
                                string destinationDir = Path.Combine(destination.FullName, dir.Name);

                                // Call CopyDirectory() recursively.
                                CopyDirectory(dir, new DirectoryInfo(destinationDir), null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AddToActivityLog("*Error* copydirectory:  - " + ex.Message);
                    }
                }
                else
                {
                    isSuccess = false;
                    AddToActivityLog("Source directory not found: " + source.FullName);
                }

            return isSuccess;
        }

        private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        public void CopyFileAndDelete(string srcPath, string destPath)
        {
            try
            {
                File.Copy(srcPath,
                    destPath + string.Format("{0}_{1}_{2}.csv",
                    Path.GetFileNameWithoutExtension(srcPath),
                    DateTime.Now.ToString("ddMMMy"),
                    DateTime.Now.ToString("HH.mm.ss")), false);
                File.Delete(srcPath);
                AddToActivityLog("Successfully copied file from: " + srcPath + " to: " + destPath);
            }
            catch (Exception ex)
            {
                AddToActivityLog("*Error* archiving file - " + ex.Message);
            }
        }

        public void ClearExtractedFiles(string src)
        {
            try
            {
                string[] extractedDirectory = Directory.GetFiles(src);
                foreach (string filePath in extractedDirectory)
                {
                    File.Delete(filePath);
                }
                AddToActivityLog("Successfully cleared extracted files");
            }
            catch (Exception ex)
            {
                AddToActivityLog("*Error* clearing extracted files - " + ex.Message);
            }
        }

        public string LogActivity(string runType = "")
        {
            string fileName = "ActivityLog_" + runType + "_" + Guid.NewGuid() + ".txt";
            string filePath = (string)Properties.Settings.Default["LogFilePath"];

            bool ftplogs = ((string)Properties.Settings.Default["FTPLogFrom"])
                                                       .Split(',')
                                                       .Contains(Environment.MachineName.Replace("\\", ""));

            if (ftplogs) filePath = Properties.Settings.Default.LocalDirectory + (string)Properties.Settings.Default["FTPLogDirectory"];

            if (Debugger.IsAttached) filePath = @"C:\Program Files\Netgiant\BatchProcesses\Logs\";

            using (StreamWriter sw = new StreamWriter(filePath + fileName))
            {
                foreach (string item in ActivityLogArrayList)
                {
                    sw.WriteLine(item);
                }
            }

            if (ftplogs)
            {
                FtpUtilities.UploadFTPFile(filePath + fileName,
                    (string)Properties.Settings.Default["FTPAddress"] + "/" + fileName,
                    (string)Properties.Settings.Default["FTPLogUsername"],
                    (string)Properties.Settings.Default["FTPLogPassword"]);

                if (File.Exists(filePath + fileName)) File.Delete(filePath + fileName);
            }

            CleanupActivityLogLocation(filePath);

            return filePath + fileName;
        }

        public void ProcessException(Exception ex)
        {
            var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "";

            AddToActivityLog($"{Environment.NewLine}{Environment.NewLine}" +
                             $"MESSAGE: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                             $"INNER EXCEPTION: {innerException}{Environment.NewLine}{Environment.NewLine}" +
                             $"STACK TRACE: {ex.StackTrace}{Environment.NewLine}");
        }

        public static void LogActivity(ref List<string> ActivityLogArrayList, string activityLogFilePath)
        {
            StreamWriter activityLogFile = new StreamWriter(activityLogFilePath + ".txt", true);
            foreach (string item in ActivityLogArrayList)
            {
                activityLogFile.WriteLine(item);
            }
            activityLogFile.Close();
            ActivityLogArrayList.Clear();

            //Email the log to the admin email address
            string adminEmailAddress = (string)Properties.Settings.Default["AdministratorEmail"];
            string fromAddress = (string)Properties.Settings.Default["DefaultEmailFromAddress"];
            List<string> emailTo = new List<string>();
            emailTo.Add(adminEmailAddress);

            string subject = "Activity Log - " + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss");
            string body = "Activity Log - " + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + " attached to this email.";

            Email.SendEmail(emailTo, fromAddress, subject, body, false, activityLogFilePath + ".txt");
        }

        public void CleanupActivityLogLocation(string activityLogPath)
        {
            string[] files = Directory.GetFiles(activityLogPath);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (DateTime.UtcNow - fi.CreationTimeUtc > TimeSpan.FromDays(14))
                {
                    File.Delete(fi.FullName);
                }
            }
        }

        public void CleanupArchiveLocation(string archivePath)
        {
            string[] files = Directory.GetFiles(archivePath);

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (DateTime.UtcNow - fi.CreationTimeUtc > TimeSpan.FromDays(7))
                {
                    File.Delete(fi.FullName);
                }
            }
        }

        public void CleanupArchiveLocationByNumber(string archivePath, int numberToKeep)
        {
            var directory = new DirectoryInfo(archivePath);
            var files = directory.GetFiles().OrderByDescending(f => f.LastWriteTime);
            int loopCount = 1;

            foreach (FileInfo fi in files)
            {
                if (loopCount > numberToKeep)
                {
                    File.Delete(fi.FullName);
                }
                loopCount ++;
            }
        }

        public void SendSimpleEmail(string switchUsed, string filePath, 
                                    List<string> additionalToEmails = null)
        {
            string emailSubject = "NG Batch Executed";
            string emailBody = "Netgiant Batch Processes ran with this switch - " + switchUsed + "\n\nActivity Log Attached";
            List<string> emailAddresses = new List<string>();
            emailAddresses.Add("chris.dunne@netgiant.com");
            emailAddresses.Add("glen.dale@netgiant.com");

            if (additionalToEmails != null)
            {
                additionalToEmails.ForEach(m => emailAddresses.Add(m));
            }

            Email.SendEmail(switchUsed, emailBody, emailSubject, emailAddresses, filePath);
        }

        public void AddToActivityLog(string message)
        {
            ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + message);
        }

        public static string GetMachineConfigAppSetting(string setting)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.AppSettings.Settings[setting].Value;
        }
        
        public void ClearFilesBasedOnDays(int days, string srcPath)
        {
            try
            {
                string[] extractedDirectory = Directory.GetFiles(srcPath);
                foreach (string filePath in extractedDirectory)
                {
                    DateTime fileCreatedDate = File.GetCreationTime(filePath);
                    if ((DateTime.Now - fileCreatedDate).Days > days)
                    {
                        File.Delete(filePath);
                    }
                }
                AddToActivityLog("Successfully cleaned archive");
            }
            catch (Exception ex)
            {
                AddToActivityLog($"**ERROR** clearing {days} days old files - {ex.Message}");
            }
        }

        public static void GeneratePrdGrpXMLs(string siteRoot) 
        {
            StandardFunctions sf = new StandardFunctions();
            sf.ActivityLogArrayList.Add("Batch Started with switch - generateprdgrpsxml");

            string URL = "http://" + siteRoot + "productByGroupGenerateXML.asp";
            string responseMessage = "";

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(URL);
                request.Timeout = 300000;

                if (siteRoot.Contains("beta"))
                {
                    request.Credentials = new NetworkCredential("webadmin", "Innovation2020");
                }

                Stream stream = request.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(stream);
                responseMessage = sr.ReadToEnd();
                
                sf.ActivityLogArrayList.Add("Successfully execute URL - " + URL);
                sf.ActivityLogArrayList.Add("Response - " + responseMessage);

            }
            catch (Exception ex)
            {
                responseMessage = ex.Message;

                List<string> emailTo = new List<string>();
                emailTo.Add("devteam@netgiant.com");
                sf.ActivityLogArrayList.Add("*Error* executing URL - " + responseMessage);
                string logPath = sf.LogActivity();
                sf = null;

                Email.SendEmail("generateprdgrpsxml", responseMessage, "*Error* Generating Product Grid XMLs - " + URL,
                                emailTo, logPath, MailPriority.High);

                return;
            }

            sf.LogActivity();
            sf = null;
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        public static string ToSafeString(object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        public static string GetConfigurationSetting(string settingName)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.settingName.ToLower() == settingName.ToLower()).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static string GetConfigurationSetting(string sectionName, string settingName)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower()).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static string GetConfigurationSetting(string sectionName, string settingName, int websiteFK)
        {
            string settingValue = string.Empty;

            using (ngmdEntities db = new ngmdEntities())
            {
                configurationSetting cs = db.configurationSetting
                    .Where(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower() &&
                        x.websiteFK == websiteFK).FirstOrDefault();

                if (cs != null)
                    settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static Dictionary<string, string> GetConfigurationSettings(Expression<Func<configurationSetting, bool>> where)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.configurationSetting
                         .Where(where)
                         .ToDictionary(x => x.settingName, x => x.settingValue);
            }
        }

        public static configurationSetting GetConfigSetting(string sectionName, string settingName, int websiteFK)
        {
            configurationSetting cs;

            using (ngmdEntities db = new ngmdEntities())
            {
                cs = db.configurationSetting
                    .Where(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower() &&
                        x.websiteFK == websiteFK).FirstOrDefault();
            }

            return cs;
        }

        public static bool SaveConfigSetting(configurationSetting cs)
        {
            bool success = true;
            cs.dateLastUpdate = DateTime.Now;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (cs.configurationSettingID > 0)
                    {
                        db.Entry(cs).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(cs).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public static List<configurationSetting> WebsiteConfigSettings {get; set;}

        public static void SetSiteConfigSettings(int websiteId)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    WebsiteConfigSettings = db.configurationSetting
                            .Where(x => x.websiteFK == websiteId &&
                                x.sectionName == "Website Application Variables")
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }

        public static string GetNgmdCMSEntry(int websiteId, string sectionName, string entryName, Dictionary<string, string> replacements = null)
        {
            cmsEntry entry = new cmsEntry();
            string s = "";

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    int sectionId = db.cmsSection                        
                        .Where(x => x.sectionName == sectionName && x.websiteFK == websiteId).FirstOrDefault().cmsSectionID;
                    entry = db.cmsEntry
                        .Include(x => x.cmsEntry2)
                        .Where(x => x.entryName == entryName && x.cmsSectionFK == sectionId)
                        .FirstOrDefault();
                    s = entry.cmsContent;
                    if (entry.redirectIsActive)
                    {
                        if (entry.redirectFrom < DateTime.Now && entry.redirectUntil > DateTime.Now)
                        {
                            s = entry.cmsEntry2.cmsContent;
                        }
                    }
                }
                if (replacements != null)
                {
                    foreach (KeyValuePair<string, string> kvp in replacements)
                    {
                        s = s.Replace(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            return s;
        }

        public static string GetCMSEntry(int websiteId, int seriesId, int entryId, string entryType)
        {
            //Retrieve website variables
            string siteRoot;
            string siteRootSecure;
            string siteRootPlusVn;

            try
            {
                if (WebsiteConfigSettings == null)
                {
                    SetSiteConfigSettings(websiteId);
                }
                siteRoot = "http://" + WebsiteConfigSettings.Where(x => x.settingName == "siteRoot").FirstOrDefault().settingValue;
                siteRootSecure = siteRoot.Replace("http://", "https://");
                siteRootPlusVn = siteRoot + "version" + WebsiteConfigSettings.Where(x => x.settingName == "ResourceVersion").FirstOrDefault().settingValue + "/";
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            string html = "";
            List<string> cmsResults;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    cmsResults = db.GetCMSEntry(
                        WebsiteConfigSettings.Where(x => x.settingName == "site").FirstOrDefault().settingValue,
                        seriesId,
                        entryId,
                        entryType
                    ).ToList();
                }

                //Loop through the string array and merge the records
                foreach (string cmsRow in cmsResults)
                {
                    html += cmsRow;
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            //Deal with replacements
            html = ReplacePlaceholders(siteRoot, siteRootSecure, siteRootPlusVn, html);

            return html;
        }

        public static List<CMS_SE> GetAllCMSEntries(int websiteId, string entryType)
        {
            List<CMS_SE> cmsList = new List<CMS_SE>();

            try
            {
                if (WebsiteConfigSettings == null)
                {
                    SetSiteConfigSettings(websiteId);
                }
                var siteRoot = "http://" + WebsiteConfigSettings.Where(x => x.settingName == "siteRoot").FirstOrDefault().settingValue;
                var siteRootSecure = siteRoot.Replace("http://", "https://");
                var siteRootPlusVn = siteRoot + "version" + WebsiteConfigSettings.Where(x => x.settingName == "ResourceVersion").FirstOrDefault().settingValue + "/";

                var sqlParams = new List<KeyValuePair<string, string>>();
                sqlParams.Add(new KeyValuePair<string, string>("SiteCode", WebsiteConfigSettings.Where(x => x.settingName == "site").FirstOrDefault().settingValue));
                sqlParams.Add(new KeyValuePair<string, string>("EntryType", entryType));

                DataTable cmsResults = SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.GetAllCMSEntries", sqlParams);

                int previousSeriesID = 0;
                int previousTextID = 0;

                foreach (DataRow dr in cmsResults.Rows)
                {
                    int currentSeriesID = (int)dr["SeriesID"];
                    int currentTextID = (int)dr["TextID"];
                    string cmsContent = (string)dr["Text"];

                    if (previousSeriesID == currentSeriesID && previousTextID == currentTextID)
                    {
                        CMS_SE lastCMS = cmsList.Last();
                        lastCMS.CMSContent += cmsContent;
                    }
                    else
                    {
                        CMS_SE newCMS = new CMS_SE();
                        newCMS.SeriesID = currentSeriesID;
                        newCMS.TextID = currentTextID;
                        newCMS.CMSContent = cmsContent;
                        cmsList.Add(newCMS);   
                    }

                    previousSeriesID = currentSeriesID;
                    previousTextID = currentTextID;
                }

                foreach (CMS_SE cms in cmsList)
                {
                    cms.CMSContent = ReplacePlaceholders(siteRoot, siteRootSecure, siteRootPlusVn, cms.CMSContent);
                }

            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message, ex.InnerException));
            }

            return cmsList;
        }

        //public static string GetRandomText(int websiteId, int prdTypeId, bool isOwnBrand, bool isAssembly, bool isMaintenance, int entry1, int entry2, int entry3, int entry4, int entry5, List<int> entries, int start)
        public static string GetRandomText(int websiteId, int prdTypeId, bool isOwnBrand, bool isAssembly, bool isMaintenance, List<int> entries, int start)
        {
            string html = "";
            List<GetProductSeoText_Result> randomText;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    randomText = db.GetProductSeoText(
                        websiteId,
                        prdTypeId,
                        isOwnBrand,
                        isAssembly,
                        isMaintenance,
                        entries[0],
                        entries[1],
                        entries[2],
                        entries[3],
                        entries[4],
                        entries[5],
                        entries[6],
                        entries[7],
                        entries[8],
                        entries[9],
                        start
                    ).ToList();
                }

                //Loop through the string array and merge the records
                foreach (GetProductSeoText_Result textRow in randomText)
                {
                    html += "<div class=\"g-m-t-5\">" + textRow.paraText + "</div>";
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            return html;
        }

        private static string ReplacePlaceholders(string siteRoot, string siteRootSecure, string siteRootPlusVn, string html)
        {
            if (html.Contains("{"))
            {
                string pattern1 = @"\{ResourceURL,(.*)\}";
                string replacement1 = "$1";

                html = html.Replace("{SiteRoot}", siteRoot);
                html = html.Replace("{SiteRootSecure}", siteRootSecure);
                html = Regex.Replace(html, pattern1, siteRootPlusVn + replacement1);
            }
            return html;
        }

        public static string CleanupURL(string url)
        {
            string newUrl = Regex.Replace(url, @"[\,\(\)\[\]']", "");
            newUrl = Regex.Replace(newUrl, @"[\/\s\+\.]", "-");
            newUrl = newUrl.Replace("&amp;", "-");
            newUrl = Regex.Replace(newUrl, @"\&", "-");
            newUrl = Regex.Replace(newUrl, @"\-+", "-");

            return newUrl;
        }

        public static bool checkFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public static List<Website> GetAllWebsites()
        {
            using (var db = new ngmdEntities())
            {
                return db.Websites.ToList();
            }
        }

        public static List<provider> GetProviderList(Expression<Func<provider, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.provider.Where(where).OrderBy(x => x.providerName).ToList();
            }
        }

        public static List<websiteInventory> GetWebsiteInventoryList(Expression<Func<websiteInventory, bool>> where, bool shortlist = false)
        {
            using (var db = new ngmdEntities())
            {
                if (shortlist)
                {
                    return db.websiteInventory
                        .Where(where)
                        .ToList();
                }
                else
                {
                    db.Database.CommandTimeout = (3 * 60);
                    return db.websiteInventory
                        .Include(x => x.productPrice)
                        .Include(x => x.product.manufacturer)
                        .Include(x => x.product.AxisFields.AxisFieldsAdditional)
                        .Include(x => x.product.productGroup)
                        .Include(x => x.product.crossSellingLink)
                        .Include(x => x.product.crossSellingLink1)
                        .Where(where)
                        .ToList();
                }
            }
        }

        public static List<Website> GetWebsiteList(Expression<Func<Website, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.Websites
                    .Where(where)
                    .OrderBy(x => x.WebsiteID)
                    .ToList();
            }
        }

        public static List<eqEquipment> GetEquipmentList(Expression<Func<eqEquipment, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.eqEquipment
                    .Include(x => x.eqCartridgeType)
                    .Where(where)
                    .ToList();
            }
        }

        public static List<eqProductMembership> GetProductMembershipList(Expression<Func<eqProductMembership, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.eqProductMembership
                    .Include(x => x.product.websiteInventory)
                    .Where(where)
                    .ToList();
            }
        }

        public static List<categoryCode> GetCategoryCodeList(Expression<Func<categoryCode, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.categoryCode
                    .Where(where)
                    .ToList();
            }
        }

        public static List<secondaryCategoryLookup> GetSecondaryCategoryList(Expression<Func<secondaryCategoryLookup, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.secondaryCategoryLookup
                    .Where(where)
                    .ToList();
            }
        }

        public static List<websiteInventory> GetCategoryMembershipList(Expression<Func<websiteInventory, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.websiteInventory
                    .Where(where)
                    .ToList();
            }
        }

        public static List<Searchable> GetAttribute(string manufacturer, string partNo)
        {
            using (var db = new ngmdEntities())
            {
                // combine or and ng searchables
                return
                (from t1 in db.or_products
                    join t2 in db.or_searchables
                        on t1.prodID equals t2.prodID
                    where t1.manufacturer == manufacturer && t1.partno == partNo
                    select new Searchable()
                    {
                        Name = t2.name,
                        Value = t2.value
                    })
                .Union
                (from t1 in db.ng_products
                    join t2 in db.ng_searchables
                        on t1.prodID equals t2.prodID
                 where t1.manufacturer == manufacturer && t1.partno == partNo
                 select new Searchable()
                    {
                        Name = t2.name,
                        Value = t2.value
                    })
                .ToList();
            }
        }

        public static List<or_products> GetOrAttribute(Expression<Func<or_products, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.or_products
                    .Include(x => x.or_attributes)
                    .Where(where)
                    .ToList();
            }
        }

        public static List<AxisValueLookup> GetAxisValueLookup(Expression<Func<AxisValueLookup, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.AxisValueLookup
                    .Where(where)
                    .ToList();
            }
        }

        public static void NoFilesInPickupDirectory(ref List<string> ActivityLogArrayList)
        {
            List<string> toAddresses = new List<string>();
            toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

            //string subject = "Delivery Tracking Information - **WARNING**";
            string message = "The delivery tracking program ran successfully, but no supplier files were found in the pickup directory.";
            //string from = Properties.Settings.Default.DefaultEmailFromAddress;

            //Email.SendEmail(toAddresses, from, subject, message, false);
            ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
        }

        public static string FormatStringFromCSV(string s)
        {
            return s.Replace("\"", string.Empty).Replace(" ", string.Empty);
        }

        public static void CheckValidColumns(int columnsMatched, int totalRequired)
        {
            if (columnsMatched != totalRequired)
            {
                throw new Exception();
            }

        }

        public static void EmailCriteriaProblem(string custRef, string custEmail, string trackLink,
                                                bool customerExcluded, ref List<string> ActivityLogArrayList)
        {
            string noEmailReason;

            if (string.IsNullOrEmpty(custRef))
            {
                noEmailReason = "*no customer ref*";
            }
            else if (string.IsNullOrEmpty(trackLink))
            {
                noEmailReason = "*no tracking link*";
            }
            else if (customerExcluded)
            {
                noEmailReason = "*custRef - " + custRef + ", customer opted out of tracking emails*";
            }
            else
            {
                noEmailReason = "*unknown issue in ProcessLines function*";
            }

            List<string> toAddresses = new List<string>();
            toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

            //string subject = "Delivery Tracking Information";
            string body = "Email not sent to this user because " + noEmailReason + " - " + custEmail;
            //string from = Properties.Settings.Default.DefaultEmailFromAddress;

            //Email.SendEmail(toAddresses, from, subject, body, false);
            ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + body);

        }

        public static string GenerateTrackingLink(string carrierName, string consignmentNo, ref List<string> ActivityLogArrayList)
        {
            string generatedTrackingLink = "";
            string[] trackingLinksArray = Convert.ToString(Properties.Settings.Default["TrackingAddresses"]).Split('$');

            //Check if the carrier has a match in the config array
            int trackingLinksArrayIndex = Array.FindIndex(trackingLinksArray, row => carrierName.ToLower().Contains(row.Split('~')[0].ToLower()));

            if (trackingLinksArrayIndex != -1 && consignmentNo.Length > 0)
            {
                generatedTrackingLink = trackingLinksArray[trackingLinksArrayIndex].Split('~')[1].Replace("[PLACEHOLDER]", consignmentNo);
            }
            else
            {
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                //string subject = "Delivery Tracking Information - **Error**";
                string message = "Could not generate tracking link - " + carrierName + " : " + consignmentNo;
                if (consignmentNo.Length == 0)
                {
                    message += " ConsignmentNo was blank";
                }
                //string from = Properties.Settings.Default.DefaultEmailFromAddress;

                //Email.SendEmail(toAddresses, from, subject, message, false);
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return generatedTrackingLink;
        }

        public static string GenerateTrackingLinkNoCarrier(string supplierName, string consignmentNo, ref List<string> ActivityLogArrayList)
        {
            string generatedTrackingLink = "";

            if (supplierName == "beta")
            {
                string[] trackingLinksArray = Convert.ToString(Properties.Settings.Default["TrackingAddresses"]).Split('$');

                int trackingLinksArrayIndex = 2; //This is the position of DPDBeta in the trackingAddress array.

                generatedTrackingLink = trackingLinksArray[trackingLinksArrayIndex].Split('~')[1].Replace("[BetaDocNumber]", consignmentNo);
            }
            else if (supplierName == "vow")
            {
                string[] trackingLinksArray = Convert.ToString(Properties.Settings.Default["TrackingAddresses"]).Split('$');

                int trackingLinksArrayIndex = 4; //This is the position of UPS in the trackingAddress array.

                generatedTrackingLink = trackingLinksArray[trackingLinksArrayIndex].Split('~')[1].Replace("[PLACEHOLDER]", consignmentNo);
            }
            else
            {
                List<string> toAddresses = new List<string>();
                toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

                //string subject = "Delivery Tracking Information - **Error**";
                string message = "Could not generate tracking link - " + supplierName + " : " + consignmentNo;
                //string from = Properties.Settings.Default.DefaultEmailFromAddress;

                //Email.SendEmail(toAddresses, from, subject, message, false);
                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return generatedTrackingLink;
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static bool CheckFileValid(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            bool valid;

            switch (extension.ToLower())
            {
                case ".csv":
                    valid = true;
                    break;
                default:
                    valid = false;
                    break;
            }

            return valid;

        }

        public static void FTPFile(Dictionary<string,string> parms)
        {
            FtpUtilities.UploadFTPFile(parms["input"],
                        parms["ftpsite"] + "/" + parms["ftppath"] + parms["output"],
                        parms["ftpusername"],
                        parms["ftppassword"],
                        parms.FirstOrDefault(x => x.Key == "subtype").Value == "usessl");

            bool deleteFile = parms.ContainsKey("delete") ? Convert.ToBoolean(parms["delete"]) : false;

            if (deleteFile && File.Exists(parms["input"]))
            {
                File.Delete(parms["input"]);
            }
        }
    }

    public class Searchable
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }
}
