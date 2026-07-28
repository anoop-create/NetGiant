using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using System.IO.Compression;
using netGiant.Intranet.DataLayer;

namespace ngBatchProcesses.BusinessObjects.FileSystem
{
    public class FileFunctions
    {
        public FileFunctions(Dictionary<string, string> parms)
        {
            _parms = parms;
            _localPath = Properties.Settings.Default.LocalDirectory + "ProviderInventory\\Working\\";
        }

        private static string _localPath;
        private static Dictionary<string, string> _parms;

        public void MergeSpicersCsvFiles()
        {
            GetFilesFromFtp();

            var fileAContent = File.ReadAllLines(_localPath + _parms["filea"]);
            var fileBContent = File.ReadAllLines(_localPath + _parms["fileb"]);
            var fileCContent = File.ReadAllLines(_localPath + _parms["filec"]);
            var list = new List<Spicers>();

            var mainContents =
                (from contC in fileCContent
                let cFields = contC.Split(',')
                select new Spicers() { PartNo = cFields[0], Description = cFields[4], Mfpn = cFields[16] }).ToList();

            foreach (var line in fileBContent)
            {
                var fields = line.Split(',');
                var record = mainContents.Where(x => x.PartNo == fields[0]).FirstOrDefault();

                if (record != null)
                    record.Quantity = fields[10];
            }

            foreach (var line in fileAContent)
            {
                var fields = line.Split(',');
                var record = mainContents.Where(x => x.PartNo == fields[0]).FirstOrDefault();

                if (record != null)
                    record.Price = fields[1];
            }

            mainContents.RemoveAt(0);

            SaveSpicersFile(mainContents);
            DeleteFiles();
        }

        private void DeleteFiles()
        {
            if (File.Exists(_localPath + _parms["filea"]))
                File.Delete(_localPath + _parms["filea"]);

            if (File.Exists(_localPath + _parms["fileb"]))
                File.Delete(_localPath + _parms["fileb"]);

            if (File.Exists(_localPath + _parms["filec"]))
                File.Delete(_localPath + _parms["filec"]);

            if (File.Exists(_localPath + _parms["input"]))
                File.Delete(_localPath + _parms["input"]);

            if (File.Exists(_localPath + "finalSpicersFile.csv"))
                File.Delete(_localPath + "finalSpicersFile.csv");
        }

        private static void GetFilesFromFtp()
        {
            FtpUtilities.DownloadFTPFiles(_parms["ftpsite"] + "pickup/",
                _parms["ftpusername"],
                _parms["ftppassword"],
                _localPath,
                _parms["input"],
                ".zip");

            FtpUtilities.DownloadFTPFiles(_parms["ftpsite"] + "pickup/",
                _parms["ftpusername"],
                _parms["ftppassword"],
                _localPath,
                _parms["filea"]);

            FtpUtilities.DownloadFTPFiles(_parms["ftpsite"] + "ukecommfeed/",
                _parms["ftpusername"],
                _parms["ftppassword"],
                _localPath,
                _parms["fileb"]);

            //Extract Zip
            if (!File.Exists(_localPath + _parms["filec"]))
                ZipFile.ExtractToDirectory(_localPath + _parms["input"], _localPath);
        }

        static void SaveSpicersFile(List<Spicers> products)
        {
            using (CsvFileWriter writer = new CsvFileWriter(_parms["output"] + "finalSpicersFile.csv", ','))
            {
                var firstRow = new CsvRow();
                firstRow.Add("SupplierPartNo");
                firstRow.Add("Mfpn");
                firstRow.Add("Description");
                firstRow.Add("Price");
                firstRow.Add("Quantity");
                writer.WriteRow(firstRow);

                foreach (var item in products)
                {
                    var newRow = new CsvRow();
                    newRow.Add(item.PartNo.ToSafeString());
                    newRow.Add(item.Mfpn.ToSafeString());
                    newRow.Add(item.Description.ToSafeString());
                    newRow.Add(item.Price.ToSafeString());
                    newRow.Add(String.IsNullOrEmpty(item.Quantity) ? "0" : item.Quantity);
                    writer.WriteRow(newRow);
                }
            }
        }

        public static bool CopyDirectory(Dictionary<string, string> parms, StandardFunctions stnFunc)
        {
            bool errorHasOccurred = false;
            bool writelog = false;

            if (stnFunc == null)
            {
                stnFunc = new StandardFunctions();
                writelog = true;
            }
            stnFunc.AddToActivityLog("copydirectory" + " i:" + parms["input"] + " o:" + parms["output"] + " st:" + parms["subtype"] + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;

            try
            {               
                DirectoryInfo source = new DirectoryInfo(parms["input"]);
                DirectoryInfo dest = new DirectoryInfo(parms["output"]);

                List<string> exclude = new List<string>();

                if (parms.ContainsKey("filepath"))
                {
                    exclude = parms["filepath"].Split(',').ToList();
                }

                bool copySubDirs = true;
                if (parms["type"] == "2")
                {
                    copySubDirs = false;
                }
                stnFunc.AddToActivityLog("Copying sub-directories: " + copySubDirs.ToString());
                errorHasOccurred = !stnFunc.CopyDirectory(source, dest, exclude, copySubDirs);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Attempting to copy directory: " + ex.Message);
            }

            //Log in activity log
            stnFunc.AddToActivityLog("copydirectory Process Finished");
            if (writelog)
            {
                string activityLogFileName = stnFunc.LogActivity(parms["type"]);
                if (errorHasOccurred && settings.Environment == "Live")
                {
                    List<string> additionalEmails = new List<string>();
                    additionalEmails.Add("Daniel.whittaker@netgiant.com");
                    additionalEmails.Add("stuart.deavall@netgiant.com");
                    stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
                }

                stnFunc = null;
            }            

            return !errorHasOccurred;
        }

        public static void RefreshSite(Dictionary<string, string> parms)
        {
            bool errorHasOccurred = false;
            int websiteid = int.Parse(parms["websiteid"]);
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " ws:" + parms["websiteid"] + " i:" + parms["input"] + " o:" + parms["output"] + " st:" + parms["subtype"] + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;

            try
            {
                configurationSetting cs = StandardFunctions.GetConfigSetting("BatchProgram", "RefreshSite", websiteid);
                if (Convert.ToBoolean(cs.settingValue))
                {
                    if (!CopyDirectory(parms, stnFunc))
                    {
                        stnFunc.AddToActivityLog("** ERROR ** Directory copy has failed.");
                        errorHasOccurred = true;
                    }
                    else
                    {
                        stnFunc.AddToActivityLog("Successful copy of directory.");
                        cs.settingValue = "False";
                        if (!StandardFunctions.SaveConfigSetting(cs))
                        {
                            stnFunc.AddToActivityLog("** ERROR ** Reset of RefreshSite boolean has failed.");
                            errorHasOccurred = true;
                        }
                        else
                        {
                            stnFunc.AddToActivityLog("Successful Reset of RefreshSite boolean.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** refresh site: " + ex.Message);
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }

            stnFunc = null;
        }
    }

    public static class StringExtensions
    {
        public static string ToSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString().Trim();
        }
    }

    public class Spicers
    {
        public string PartNo { get; set; }
        public string Mfpn { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string Quantity { get; set; }
    }
}
