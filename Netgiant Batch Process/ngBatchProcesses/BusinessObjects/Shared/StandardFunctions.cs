using Google.Protobuf.WellKnownTypes;
using Nest;
using netGiant.Intranet.DataLayer.CustomerData;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using NGBP.DataAccessLayer.DataUtilities;
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class Global
    {
        public static Dictionary<string, string> Variable = new Dictionary<string, string>();
    }
    public class StandardFunctions
    {
        private List<string> ActivityLogArrayList { get; } = new List<string>();

        public static void SetGlobalUserVariables()
        {
            Properties.Settings.Default.LocalDirectory = GetMachineConfigAppSetting("LocalDirectory");
            Properties.Settings.Default.SQLServerFilePath = GetMachineConfigAppSetting("SQLServerFilePath");
            Properties.Settings.Default.Environment = GetMachineConfigAppSetting("Environment");
            Properties.Settings.Default.SQLServerLocalDirectory = GetMachineConfigAppSetting("SQLServerLocalDirectory");
        }

        internal static void SetPropertySettings()
        {
            List<configurationSetting> lcs = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgramSetting");

            foreach (configurationSetting cs in lcs)
            {
                SettingsProperty property = new SettingsProperty(cs.settingName);
                property.Provider = Properties.Settings.Default.Providers["LocalFileSettingsProvider"];
                property.DefaultValue = cs.settingValue;
                property.PropertyType = typeof(string);
                property.Attributes.Add(typeof(UserScopedSettingAttribute), new UserScopedSettingAttribute());
                Properties.Settings.Default.Properties.Add(property);
            }

            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();
        }

        public bool ExtractZipFile(string src, string dest)
        {
            try
            {
                ClearExtractedFiles(dest);
                string[] directoryList = Directory.GetFiles(src);
                foreach (string file in directoryList)
                {
                    ZipFile.ExtractToDirectory(file, dest);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully extracted zip file - " + file + " to - " + dest });
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR extracting a zip file from " + src, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                return false;
            }
            return true;
        }

        public void ArchiveFile(string src, string dest)
        {
            try
            {
                string[] directoryList = Directory.GetFiles(src);
                foreach (string file in directoryList)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Archiving " + file + " to " + dest + "_" + Path.GetFileName(file) });
                    File.Move(file, dest + "_" + Path.GetFileName(file));
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully archived file - " + file });
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Archiving file " + src, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        // Alternative version of ArchiveFile which makes 5 attempts before failing
        //public void ArchiveFile(string src, string dest)
        //{
        //    string[] directoryList = Directory.GetFiles(src);
        //    foreach (string file in directoryList)
        //    {
        //        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Archiving " + file + " to " + dest + "_" + Path.GetFileName(file) });
        //        for (int i = 0; i < 5; i++)
        //        {
        //            try
        //            {
        //                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Attempt " + (i+1).ToString() });
        //                //File.Copy(file, dest + "_" + Path.GetFileName(file));
        //                //File.Delete(file);
        //                File.Move(file, dest + "_" + Path.GetFileName(file));
        //                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully archived file - " + file });
        //                break;
        //            }
        //            catch (Exception ex)
        //            {
        //                if (i == 4)
        //                {
        //                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Archiving file " + src, ErrorCode = "ERROR" });
        //                    StandardFunctions.WriteException(ex);
        //                    break;
        //                }
        //                Thread.Sleep(500);
        //            }
        //        }
        //    }
        //}

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
                        logMessage = "Successfully archived file - " + path;
                        break;
                    case 2:
                        archivePath = (string)Properties.Settings.Default["ErrorFilePath"];
                        logMessage = "Successfully moved file to error directory - " + path;
                        break;
                    default:
                        archivePath = (string)Properties.Settings.Default["ErrorFilePath"];
                        logMessage = "Successfully moved file to error directory - " + path;
                        break;
                }

                File.Move(path, archivePath + "\\" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + "_" + Path.GetFileName(path));
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = logMessage });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Could not archive file" + path, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        public void CopyFile(Dictionary<string, string> parms)
        {
            CopyFile(parms["input"], parms["output"]);
        }

        public bool CopyFile(string input, string output, bool overwrite = true)
        {
            bool isSuccess = true;
            try
            {
                File.Copy(input,
                    output, overwrite);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Copying file " + input, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }

            return isSuccess;
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
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Copying directory", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }
            else
            {
                isSuccess = false;
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Source directory not found: " + source.FullName, ErrorCode = "ERROR" });
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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully copied file from: " + srcPath + " to: " + destPath });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Archiving file", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully cleared extracted files" });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Clearing extracted files", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        public static bool IsFileLocked(string file)
        {
            FileInfo f = new FileInfo(file);
            try
            {
                using (FileStream stream = f.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "File is locked or doesn't exist - " + file, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);

                return true;
            }

            //file is not locked
            return false;
        }

        public static bool WriteProcessStarted()
        {
            return StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process started. " + ConfigurationManager.AppSettings["CPU"] + ". " + ConfigurationManager.AppSettings["Environment"] + "." });
        }

        public static bool WriteBatchLog(BatchLogDetail detail)
        {
            if (Int32.Parse(Global.Variable["bypassmessages"]) == 1)
            {
                return true;
            }
            bool success = true;
            int id = Int32.Parse(Global.Variable["BatchLogId"]);

            try
            {
                // Attempt to retieve log
                BatchLog log = EntityFunctions.GetBatchLog(x => x.BatchLogId == id).FirstOrDefault();

                // If log doesn't exist then set up root
                if (log == null)
                {
                    log = new BatchLog()
                    {
                        Command = Global.Variable.ContainsKey("command") ? Global.Variable["command"] : "",
                        Type = Global.Variable.ContainsKey("type") ? Global.Variable["type"] : null,
                        SubType = Global.Variable.ContainsKey("subtype") ? Global.Variable["subtype"] : null,
                        WebsiteFk = Global.Variable.ContainsKey("websiteid") ? Int32.Parse(Global.Variable["websiteid"]) : (int?)null,
                        DateTime = DateTime.Now
                    };
                    EntityFunctions.SaveBatchLog(log);

                    Global.Variable["BatchLogId"] = log.BatchLogId.ToString();
                }

                // Insert log details
                detail.BatchLogFk = Int32.Parse(Global.Variable["BatchLogId"]);
                detail.DateTime = DateTime.Now;
                EntityFunctions.SaveBatchLogDetail(detail);
            }
            catch (Exception)
            {
                success = false;
                //throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public static void WriteException(Exception ex, string errCode = "ERROR")
        {
            var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "";

            WriteBatchLog(new BatchLogDetail()
            {
                Message = $"{Environment.NewLine}{Environment.NewLine}" +
                             $"MESSAGE: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                             $"INNER EXCEPTION: {innerException}{Environment.NewLine}{Environment.NewLine}" +
                             $"STACK TRACE: {ex.StackTrace}{Environment.NewLine}",
                ErrorCode = errCode
            });
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
                loopCount++;
            }
        }

        public static string GetMachineConfigAppSetting(string setting)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.AppSettings.Settings[setting].Value;
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

        public static bool BulkInsertMailingList(List<StagingMailingList> lml)
        {
            // The following bulk insertion pattern Saves Changes and recreates the db.Context after an optimum number of inserts
            // The same pattern can be reused but the optimumSaveCount should be adjusted each time to suit

            int optimumSaveCount = 20000;
            bool success = true;

            customerEntities db = new customerEntities();
            db.Configuration.AutoDetectChangesEnabled = false;
            int counter = 0;
            foreach (StagingMailingList ml in lml)
            {
                // All Inserts (no updates)
                db.Entry(ml).State = EntityState.Added;
                //counter += 1;
                //if (counter > optimumSaveCount)
                //{
                //    db.SaveChanges();
                //    db.Dispose();
                //    db = new customerEntities();
                //    db.Configuration.AutoDetectChangesEnabled = false;
                //    counter = 0;
                //}
            }

            db.SaveChanges();
            db.Dispose();

            return success;
        }

        public static void NoFilesInPickupDirectory(ref List<string> ActivityLogArrayList)
        {
            List<string> toAddresses = new List<string>();
            toAddresses.Add((string)Properties.Settings.Default["AdministratorEmail"]);

            //string subject = "Delivery Tracking Information - **WARNING**";
            string message = "The delivery tracking program ran successfully, but no supplier files were found in the pickup directory.";
            //string from = Properties.Settings.Default.DefaultEmailFromAddress;

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

                ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " - " + message);
            }

            return generatedTrackingLink;
        }

        public static DataTable RationaliseTable(DataTable dtOld, DataTable dtNew, List<fieldMapping> lfm)
        {
            foreach (DataRow dr in dtOld.Rows)
            {
                DataRow drNew = dtNew.NewRow();
                foreach (fieldMapping fm in lfm)
                {
                    System.Type type = dtNew.Columns[fm.fieldMappingTo].DataType;
                    if (!string.IsNullOrEmpty(dr[fm.fieldMappingWith].ToString()))
                    {
                        if (type.Equals(typeof(System.DateTime)))
                        {
                            DateTime? dt1 = StandardFunctions.ConvertStringToNullableDate(dr[fm.fieldMappingWith].ToString());
                            if (dt1 != null)
                            {
                                drNew[fm.fieldMappingTo] = dt1;
                            }
                        }
                        if (type.Equals(typeof(System.Int32)))
                        {
                            int num = new int();
                            if (decimal.TryParse(dr[fm.fieldMappingWith].ToString(), out decimal dec))
                            {
                                num = (int)dec;
                            }
                            //Int32.TryParse(dr[fm.fieldMappingWith].ToString(), out num);
                            drNew[fm.fieldMappingTo] = num;
                        }
                        if (type.Equals(typeof(System.String)))
                        {
                            drNew[fm.fieldMappingTo] = dr[fm.fieldMappingWith];
                        }
                    }
                }
                dtNew.Rows.Add(drNew);
            }

            return dtNew;
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

        public static DateTime? ConvertStringToNullableDate(string s)
        {
            DateTime date;
            return DateTime.TryParse(s, out date) ? date : (DateTime?)null;
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

        public static void FTPFile(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Upload Attempt: " + parms["input"] + " to " + parms["ftpsite"] });
            Tuple<bool, string> res = FtpUtilities.UploadFTPFile(parms["input"],
                parms["ftpsite"],
                parms["ftpusername"],
                parms["ftppassword"],
                parms["ftppath"] + parms["output"],
                parms.FirstOrDefault(x => x.Key == "subtype").Value == "usessl");
            if (res.Item1)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
            }
            else
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + parms["input"] + ". Message: " + res.Item2, ErrorCode = "ERROR" });
            }

            bool deleteFile = parms.ContainsKey("delete") ? Convert.ToBoolean(parms["delete"]) : false;

            if (deleteFile && File.Exists(parms["input"]))
            {
                File.Delete(parms["input"]);
            }
        }

        public static string HashSha256String(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            StringBuilder sbHash = new StringBuilder();
            foreach (byte x in hash)
            {
                sbHash.Append(x.ToString("X2"));
            }
            return sbHash.ToString();
        }

        public static string GenerateVoucherCode(string number)
        {
            char[] letters = { 'W', 'H', 'T', 'Y', 'E', 'A', 'P', 'L', 'F', 'D' };
            string voucherCode = "";
            foreach (char c in number)
            {
                voucherCode += letters[int.Parse(c.ToString())];
            }

            return voucherCode;
        }

        public static void CWrite(string s)
        {
            // Used only during testing hence only when local
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
            {
                Console.WriteLine(s);
            }
        }

        public static void CReadKey()
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
            {
                Console.ReadKey();
            }
        }
        public static void SetTlsVersion()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
        }
    }

    public class Searchable
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }

    public class MenuManu
    {
        public string Item1 { get; set; }
        public string Item2 { get; set; }
        public int Item3 { get; set; }
    }
}
