using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using System.Net;
using ngSBSBatchProcesses.BusinessObjects;
using System.Net.Mail;

namespace ngSBSBatchProcesses.BusinessObjects.Shared
{
    public class StandardFunctions
    {
        public List<string> ActivityLogArrayList = new List<string>();

        public static void SetGlobalUserVariables()
        {
            Properties.Settings.Default.LocalDirectory = StandardFunctions.GetMachineConfigAppSetting("LocalDirectory");
            Properties.Settings.Default.SQLServerFilePath = StandardFunctions.GetMachineConfigAppSetting("SQLServerFilePath");
            Properties.Settings.Default.Environment = StandardFunctions.GetMachineConfigAppSetting("Environment");
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
                    continue;
                }
            }
            catch (Exception e)
            {
                AddToActivityLog("*Error* extracting zip file - " + e.Message);
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
                    continue;
                }
            }
            catch (Exception e)
            {
                AddToActivityLog("*Error* archiving file - " + e.Message);
            }
        }
        public void CopyFileAndDelete(string srcPath, string destPath)
        {
            try
            {
                System.IO.File.Copy(srcPath,
                    destPath + string.Format("{0}_{1}_{2}.csv",
                    Path.GetFileNameWithoutExtension(srcPath),
                    DateTime.Now.ToString("ddMMMy"),
                    DateTime.Now.ToString("HH.mm.ss")), false);
                System.IO.File.Delete(srcPath);
                AddToActivityLog("Successfully copied file from: " + srcPath + " to: " + destPath);
            }
            catch (Exception e)
            {
                AddToActivityLog("*Error* archiving file - " + e.Message);
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
            catch (Exception e)
            {
                AddToActivityLog("*Error* clearing extracted files - " + e.Message);
            }
        }
        public string LogActivity()
        {
            string filePath = Properties.Settings.Default.LogFolder;
            string fileName = "ActivityLog_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".txt";
            StreamWriter sw = new StreamWriter(filePath + fileName);

            foreach (string item in ActivityLogArrayList)
            {
                sw.WriteLine(item);
            }

            sw.Close();

            CleanupActivityLogLocation(filePath);

            return filePath + fileName;
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
                loopCount++;
            }
        }
        public void SendSimpleEmail(string switchUsed, string filePath)
        {
            string emailSubject = "NG Batch Executed";
            string emailBody = "Netgiant Batch Processes ran with this switch - " + switchUsed + "\n\nActivity Log Attached";
            List<string> emailAddresses = new List<string>();
            emailAddresses.Add(Properties.Settings.Default.smtpTo);
            emailAddresses.Add("glen.dale@netgiant.com");
            emailAddresses.Add("atif.baig@netgiant.com");

            Email.SendEmail(switchUsed, emailBody, emailSubject, emailAddresses, filePath);
        }
        public void AddToActivityLog(string message)
        {
            ActivityLogArrayList.Add(DateTime.Now.ToString("dd-MM-yyyy H:mm:ss") + " " + message);
        }
        public static string GetMachineConfigAppSetting(string setting)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.AppSettings.Settings[setting].Value.ToString();
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
                AddToActivityLog(string.Format("Successfully cleaned archive"));
            }
            catch (Exception e)
            {
                AddToActivityLog(string.Format("*Error* clearing {0} days old files - {1}", days, e.Message));
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
                WebClient webClient = new WebClient();

                if (siteRoot.Contains("beta"))
                {
                    webClient.Credentials = new NetworkCredential("webadmin", "shadow");
                }

                Stream stream = webClient.OpenRead(URL);
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
    }
}

