using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Searching;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace ngBatchProcesses.BusinessObjects.FileSystem
{
    public class FileFunctions
    {
        private static string _localPath;
        private static Dictionary<string, string> _parms;

        public FileFunctions(Dictionary<string, string> parms)
        {
            _parms = parms;
            _localPath = Properties.Settings.Default.LocalDirectory + "ProviderInventory\\Working\\";
        }

        public static void StoreMedia()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    string[] files = Directory.GetFiles("D:\\IIS-Content-VPC\\www.cartridgemonkey.com\\media\\archive");
                    //string[] files = Directory.GetFiles("C:\\yy");
                    Console.WriteLine("File list retrieved");
                    int counter = 0;
                    int batchCount = 0;
                    foreach (string file in files)
                    {
                        if (counter > 1000)
                        {
                            db.SaveChanges();
                            batchCount += 1;
                            Console.WriteLine("Batch Saved, batch = " + batchCount.ToString());
                            counter = 0;
                        }
                        //if (counter % 1000 == 0)
                        //{
                        //    Console.WriteLine("Interim Count = " + counter.ToString());
                        //}

                        DeleteTable dt = new DeleteTable()
                        {
                            FileName = Path.GetFileName(file).Replace(".pdf", ""),
                            IsDeleted = false
                        };
                        db.Entry(dt).State = EntityState.Added;
                        counter += 1;
                    }
                    Console.WriteLine("Loop completed");
                    db.SaveChanges();
                    Console.WriteLine("Final Batch Saved");
                }
            }
            catch { }
        }

        public static bool DeleteInvoiceFiles(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            bool isSuccess = true;
            int processedCount = 0;
            int deletionCount = 0;
            DateTime startTime = DateTime.Now;
            //DirectoryInfo dirInfo = new DirectoryInfo(parms["filepath"]);
            int fileNode = parms["filepath"].Split('\\').Length;
            int period = Int32.Parse(parms["period"]) * -1;
            int mins = Int32.Parse(parms["number"]);
            DateTime cutOffDate = DateTime.Now.AddDays(period); // Files older than n days
            int websiteId = Int32.Parse(parms["websiteid"]);
            configurationSetting nextInvoiceDeletion = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "NextInvoiceDeletion" && x.websiteFK == websiteId)
                .FirstOrDefault();
            //int currentIndex = Int32.Parse(nextInvoiceDeletion.settingValue);
            List<string> deletedInvoices = new List<string>();

            IEnumerable<string> files = Directory.EnumerateFiles(parms["filepath"]).OrderBy(f => f);
            int counter = 0;
            string filename = "";
            try
            {
                foreach (string file in files)   // <== Use and index enumeration for (int i = currentIndex; i < files.Count(); i++) XX
                {
                    filename = file.Split('\\')[fileNode];
                    if (nextInvoiceDeletion.settingValue != "New")
                    {
                        if (filename != nextInvoiceDeletion.settingValue)
                        {
                            continue;
                        }
                        else
                        {
                            nextInvoiceDeletion.settingValue = "New"; // <== Store the current index here instead of filename XX
                        }
                    }

                    if (counter == 100)
                    {
                        TimeSpan span = DateTime.Now.Subtract(startTime);
                        if (span.TotalMinutes > mins)
                        {
                            nextInvoiceDeletion.settingValue = filename;
                            break;
                        }
                        counter = 0;
                    }
                    counter += 1;
                    processedCount += 1;
                    switch (filename.Substring(0, 3))
                    {
                        case "INV":
                            {
                                // Check to see if this invoice exists in Axis
                                if (isOldOrder(filename.Substring(3, 5), cutOffDate))
                                {
                                    deletedInvoices.Add(filename);
                                    deletionCount += 1;
                                    File.Delete(file);
                                }
                                continue;
                            }
                        case "CRD":
                            {
                                // Do not delete credit notes
                                continue;
                            }
                        default:
                            {
                                continue;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to delete files: " + e.Message, ErrorCode = "ERROR" });
                isSuccess = false;
            }

            // Finish processing
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Deleted Files: ");
            foreach (string delFile in deletedInvoices)
            {
                sb.Append(delFile + " | ");
            }
            if (!EntityFunctions.SaveConfigurationSetting(nextInvoiceDeletion))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR updating next invoice deletion setting", ErrorCode = "ERROR" });
                isSuccess = false;
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = sb.ToString() + deletionCount + " files." });
            if (nextInvoiceDeletion.settingValue == "New")
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "File directory sweep completed. Sweep restarts tomorrow." });
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Invoices Processed: " + processedCount + ", Invoices Deleted: " + deletionCount + "." });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
            return isSuccess;
        }

        private static bool isOldOrder(string orderNumber, DateTime cutOffDate)
        {
            bool isOld = false;
            try
            {
                string sql = @"SELECT TOP 1 SOM.drf, SOM.odt FROM dbo.accsom00 SOM
                    WHERE SOM.drf = '" + orderNumber + "'";

                DataSet ds = SQLUtilities.ExecuteReadInline("AxisDiplomat", sql, "ds");
                if (ds.Tables[0].Rows.Count > 0)
                {
                    if (DateTime.Parse(ds.Tables[0].Rows[0]["odt"].ToString()) < cutOffDate)
                    {
                        isOld = true;
                    }
                }
            }
            catch { }

            return isOld;
        }

        public static bool CopyDirectory(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            bool errorHasOccurred = CopyDirectory(parms["input"], parms["output"], parms["filepath"], parms["subtype"]);
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });

            return errorHasOccurred;
        }
        public static bool CopyDirectory(string input, string output, string exclusions = "", string subtype = "1")
        {
            bool errorHasOccurred = false;

            Properties.Settings settings = Properties.Settings.Default;

            try
            {
                DirectoryInfo source = new DirectoryInfo(input);
                DirectoryInfo dest = new DirectoryInfo(output);

                List<string> exclude = new List<string>();

                exclude = exclusions.Split(',').ToList();

                bool copySubDirs = true;
                if (subtype == "2")
                {
                    copySubDirs = false;
                }
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Copying directories: " + input + " to " + output });
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Copying sub-directories: " + copySubDirs.ToString() });
                StandardFunctions stnFunc = new StandardFunctions();
                errorHasOccurred = !stnFunc.CopyDirectory(source, dest, exclude, copySubDirs);
                stnFunc = null;
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to copy directory: " + ex.Message, ErrorCode = "ERROR" });
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Copy directoy is completed" });

            return !errorHasOccurred;
        }

        public static void MoveFiles(Dictionary<string, string> parms)
        {
            string sourceDirectory = parms["input"];        // @"C:\Temp\INV";
            string destinationDirectory = parms["output"];  //@"C:\Temp\NewINV";
            string filePath = parms["filea"];               //@"C:\Temp\deleted invoices.txt";

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Loading order numbers from Excel..." });
            var orderNumbers = LoadOrderNumbersFromText(filePath);
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Loaded " + orderNumbers.Count + " order numbers." });

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processing files..." });
            ProcessFiles(sourceDirectory, destinationDirectory, orderNumbers);
        }

        private static HashSet<string> LoadOrderNumbersFromText(string filePath)
        {
            var orders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadLines(filePath))
            {
                var orderNumber = line.Trim();

                if (!string.IsNullOrEmpty(orderNumber))
                {
                    orders.Add(orderNumber);
                }
            }

            return orders;
        }

        private static void ProcessFiles(string sourceDir, string destDir, HashSet<string> orderNumbers)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            int processed = 0;
            int moved = 0;

            foreach (var filePath in Directory.EnumerateFiles(sourceDir))
            {
                processed++;

                string fileName = Path.GetFileName(filePath);

                // Extract order number from filename
                string orderNumber = fileName.Substring(0, 5);

                if (!string.IsNullOrEmpty(orderNumber) && orderNumbers.Contains(orderNumber))
                {
                    string destPath = Path.Combine(destDir, "INV" + orderNumber + "A.pdf");

                    try
                    {
                        File.Copy(filePath, destPath);
                        moved++;
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error moving file: " + fileName + " - " + ex.Message });
                    }
                }

                if (processed % 10000 == 0)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processed: " + processed + ", Moved: " + moved });
                }
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Final Count - Processed: " + processed + ", Moved: " + moved });
        }

        public static bool MSDeploy(string input, string output)
        {
            bool isSuccess = true;
            string msdeployPath = @"C:\Program Files\IIS\Microsoft Web Deploy V3\msdeploy.exe";
            // Overwite target from source
            // Only overwite where source date > target date
            // Retry 20 times with 5 second interval
            // disbale ability to change app/server settings (file deploy only)
            // verbose output
            string arguments = $"-verb:sync -source:contentPath=\"{input}\" -dest:contentPath=\"{output}\",includeAcls=\"False\" " +
                $"-useCheckSum -enableRule:DoNotDeleteRule -retryAttempts=20 -retryInterval=5 -disableLink:AppPoolExtension " +
                $"-disableLink:CertificateExtension -verbose";
            if (Global.Variable.ContainsKey("testmode"))
            {
                arguments += " -whatif";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = msdeployPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder sbOK = new StringBuilder();
            StringBuilder sbErr = new StringBuilder();
            using (var process = new Process { StartInfo = startInfo })
            {
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sbOK.AppendLine("OUT: " + e.Data + "<br>");
                    }
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        sbErr.AppendLine("ERR: " + e.Data + "<br>");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                process.WaitForExit(2000);
                if (!string.IsNullOrEmpty(sbOK.ToString())) StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = sbOK.ToString() });
                if (!string.IsNullOrEmpty(sbErr.ToString())) StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = sbErr.ToString(), ErrorCode = "ERROR" });

                if (process.ExitCode == 0)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MSDeploy is completed" });
                }
                else
                {
                    isSuccess = false;
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR with MSDeploy: " + process.ExitCode, ErrorCode = "ERROR" });
                }
            }
            return isSuccess;
        }

        //public static void RefreshSite(Dictionary<string, string> parms)
        //{
        //    int websiteid = int.Parse(parms["websiteid"]);
        //    StandardFunctions.WriteProcessStarted();
        //    Properties.Settings settings = Properties.Settings.Default;

        //    try
        //    {
        //        Website ws = EntityFunctions.GetWebsiteList(x => x.WebsiteID == websiteid).FirstOrDefault();
        //        configurationSetting cs = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" &&
        //                x.settingName == "RefreshSite" &&
        //                x.websiteFK == websiteid).FirstOrDefault();

        //        if (Convert.ToBoolean(cs.settingValue))
        //        {
        //            string websitePath = parms["websitepath"];
        //            string backupPath = parms["filepath"] + "Backup\\" + ws.Abbreviation;
        //            string newCodePath = parms["filepath"] + "Latest\\" + ws.Abbreviation;

        //            // Do Backup
        //            if (!CopyDirectory(websitePath, backupPath, "cdn,media"))
        //            {
        //                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR backup has failed", ErrorCode = "ERROR" });
        //            }
        //            else
        //            {
        //                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful backup of website" });
        //                if (!CopyDirectory(newCodePath, websitePath))
        //                {
        //                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR new code deployment has failed for website " + ws.Abbreviation, ErrorCode = "ERROR" });
        //                }
        //                else
        //                {
        //                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful code deployment for website " + ws.Abbreviation });
        //                    cs.settingValue = "False";
        //                    if (!EntityFunctions.SaveConfigurationSetting(cs))
        //                    {
        //                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Reset of RefreshSite boolean has failed", ErrorCode = "ERROR" });
        //                    }
        //                    else
        //                    {
        //                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful Reset of RefreshSite boolean" });
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR refresh site: " + ex.Message, ErrorCode = "ERROR" });
        //    }

        //    //Log in activity log
        //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        //}

        public static void RefreshSite(Dictionary<string, string> parms)
        {
            int websiteid = int.Parse(parms["websiteid"]);
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;


            try
            {
                Website ws = EntityFunctions.GetWebsiteList(x => x.WebsiteID == websiteid).FirstOrDefault();
                configurationSetting cs = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" &&
                        x.settingName == "RefreshSite" &&
                        x.websiteFK == websiteid).FirstOrDefault();

                if (Convert.ToBoolean(cs.settingValue))
                {
                    string websitePath = parms["websitepath"];
                    string backupPath = parms["filepath"] + "Backup\\" + ws.Abbreviation;
                    string newCodePath = parms["filepath"] + "Latest\\" + ws.Abbreviation;

                    // Do Backup
                    bool backupSuccess = true;
                    if (!parms.ContainsKey("testmode"))
                    {
                        backupSuccess = CopyDirectory(websitePath, backupPath, "cdn,media");
                        if (backupSuccess)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful backup of website" });
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR backup has failed", ErrorCode = "ERROR" });
                        }
                    }
                    if (backupSuccess)
                    {
                        // Deploy Code                        
                        if (!MSDeploy(newCodePath, websitePath))
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR new code deployment has failed for website " + ws.Abbreviation, ErrorCode = "ERROR" });
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful code deployment for website " + ws.Abbreviation });
                            if (!parms.ContainsKey("testmode"))
                            {
                                cs.settingValue = "False";
                                if (!EntityFunctions.SaveConfigurationSetting(cs))
                                {
                                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Reset of RefreshSite boolean has failed", ErrorCode = "ERROR" });
                                }
                                else
                                {
                                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successful Reset of RefreshSite boolean" });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR refresh site: " + ex.Message, ErrorCode = "ERROR" });
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }

    public static class StringExtensions
    {
        public static string ToSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString().Trim();
        }
    }

    //public class Spicers
    //{
    //    public string PartNo { get; set; }
    //    public string Mfpn { get; set; }
    //    public string Description { get; set; }
    //    public string Price { get; set; }
    //    public string Quantity { get; set; }
    //}
}
