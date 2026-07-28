using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using DP001DataAccess.Entities;
using System.Data.Entity;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DP001BusinessLogic.Shared
{
    public class CommonFunctions
    {
        public static ExtractZipFileResult ExtractZipFile(
            string zipFilePath,
            string destination,
            string blobContainer = "",
            string zipFileExtractName = "",
            string uniqueId = "")
        {
            var result = new ExtractZipFileResult();

            try
            {
                var platform = CommonDataFunctions.GetPlatformType();
                var localDrive = ConfigurationManager.AppSettings["LocalDirectory"] + "\\DP001\\";

                switch (platform)
                {
                    case PlatformType.Server:

                        using (var zipToOpen = new FileStream(zipFilePath, FileMode.Open))
                        {
                            using (var archive1 = new ZipArchive(zipToOpen))
                            {
                                archive1.ExtractToDirectory(localDrive + destination, true);
                                result.IsSuccess = File.Exists(localDrive + destination + zipFileExtractName);
                                result.ErrorException = new ApplicationException("Zip does not contain specified file.");
                            }
                        }

                        if (File.Exists(zipFilePath))
                            File.Delete(zipFilePath);

                        result.Path = string.Format("{0}{1}{2}", localDrive, destination, zipFileExtractName);

                        break;

                    case PlatformType.Azure:

                        localDrive = "";
                        var zipStream = AzureFunctions.DownloadFileFromBlobToStream(blobContainer, zipFilePath);
                        MemoryStream data = new MemoryStream();

                        var archive = new ZipArchive(zipStream);
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName == zipFileExtractName)
                            {
                                entry.Open().CopyTo(data);
                                data.Position = 0;
                            }
                        }

                        using (var reader = new StreamReader(data))
                        {
                            AzureFunctions.UploadToBlobStorage(blobContainer, destination + uniqueId + "_" + zipFileExtractName, reader.ReadToEnd());
                        }

                        AzureFunctions.DeleteSingleFileInBlobContainer("tenantfolders", zipFilePath);

                        result.Path = string.Format("{0}{1}{2}", localDrive, destination, uniqueId + "_" + zipFileExtractName);

                        break;
                }
            }
            catch (Exception e)
            {
                result.IsSuccess = false;
                result.ErrorException = e;
            }

            return result;
        }

        public static bool IsFileInUse(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read)) { }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        public static void CopyFile(string source, string destination)
        {
            File.Copy(source, destination);
        }

        public static void DeleteFile(string source)
        {
            File.Delete(source);
        }

        public static string GetMachineConnectionString(string name)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[name].ConnectionString.ToString();
        }

        public static string GetMachineAppSetting(string key)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.AppSettings.Settings[key].Value.ToString();
        }

        public static bool DataTableColExists(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName);
        }

        public static string ToSafeString(object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        public static char DetectTextFileDelimiter(TextReader reader, int rowCount)
        {
            char[] delimiters = new char[] { ',', ';', '\t', '.', ':' };

            IList<char> separators = delimiters.ToArray();
            IList<int> separatorsCount = new int[separators.Count];

            int character;

            int row = 0;

            bool quoted = false;
            bool firstChar = true;

            while (row < rowCount)
            {
                character = reader.Read();

                switch (character)
                {
                    case '"':
                        if (quoted)
                        {
                            if (reader.Peek() != '"') // Value is quoted and current character is " and next character is not ".
                                quoted = false;
                            else
                                reader.Read(); // Value is quoted and current and next characters are "" - read (skip) peeked qoute.
                        }
                        else
                        {
                            if (firstChar) // Set value as quoted only if this quote is the first char in the value.
                                quoted = true;
                        }
                        break;
                    case '\n':
                        if (!quoted)
                        {
                            ++row;
                            firstChar = true;
                            continue;
                        }
                        break;
                    case -1:
                        row = rowCount;
                        break;
                    default:
                        if (!quoted)
                        {
                            int index = separators.IndexOf((char)character);
                            if (index != -1)
                            {
                                ++separatorsCount[index];
                                firstChar = true;
                                continue;
                            }
                        }
                        break;
                }

                if (firstChar)
                    firstChar = false;
            }

            int maxCount = separatorsCount.Max();

            return maxCount == 0 ? '\0' : separators[separatorsCount.IndexOf(maxCount)];
        }

        public static string DictToString<T, V>(IEnumerable<KeyValuePair<T, V>> items, string format)
        {
            format = String.IsNullOrEmpty(format) ? "{0}='{1}' " : format;

            StringBuilder itemString = new StringBuilder();
            foreach (var item in items)
                itemString.AppendFormat(format, item.Key, item.Value);

            return itemString.ToString();
        }

        public static bool CreateTextFile(string filePath, string content)
        {
            var success = true;

            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    sw.WriteLine(content);
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static string ReplaceSpecialCharacters(string originalString)
        {
            var newString = Regex.Replace(originalString, @"[\,\(\)\[\]\*']", "%");
            newString = Regex.Replace(newString, @"[\/\s\+\.]", "%");
            newString = Regex.Replace(newString, @"\-+", "%");

            return newString;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string ReadFileToString(string filePath, string blobContainer = "")
        {
            var fileContent = string.Empty;
            var platform = CommonDataFunctions.GetPlatformType();

            switch (platform)
            {
                case PlatformType.Server:

                    fileContent = File.ReadAllText(filePath);

                    break;
                case PlatformType.Azure:

                    fileContent = AzureFunctions.DownloadFileFromBlobToString(blobContainer, filePath);

                    break;
            }

            return fileContent;
        }

        public static void EmailNotifications(Dictionary<string, string> parms, List<string> notificationsList)
        {
            var crudChannel = new CrudChannel();
            var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));

            if (!string.IsNullOrEmpty(channel.NotificationsEmailAddress))
            {
                var environment = ConfigurationManager.AppSettings["Environment"];

                var body = "Please find attached Priceology notifications regarding your recent pricing calculations." +
                    "<br/>" +
                    "Alternatively, you can view your notifications here " +
                    (environment == "Dev" ? "http://beta.priceology.io/Log" : "http://www.priceology.io/Log");

                var subject = "Priceology Notifications - " + channel.ChannelName;
                var emailTo = new List<string>() { channel.NotificationsEmailAddress };

                var stream = new MemoryStream();
                var sw = new StreamWriter(stream);
                var counter = 1;

                foreach (var item in notificationsList)
                {
                    sw.Write(counter + ": " + item + Environment.NewLine);
                    counter++;
                }

                sw.Flush();
                stream.Position = 0;

                Email.SendEmail(body, subject, emailTo, "noreply@priceology.io", stream, "Notifications");
            }
        }

        public static bool DeleteFile(string filePath, string blobContainer = "")
        {
            var success = true;

            try
            {
                var platform = CommonDataFunctions.GetPlatformType();

                switch (platform)
                {
                    case PlatformType.Server:

                        if (File.Exists(filePath))
                            File.Delete(filePath);

                        break;
                    case PlatformType.Azure:

                        AzureFunctions.DeleteSingleFileInBlobContainer(blobContainer, filePath);

                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static void CleanupSqlBackupFiles()
        {
            try
            {
                if (ConfigurationManager.AppSettings["Environment"] == "Live")
                {
                    var sqlBackupFiles = AzureFunctions.ListBlobContainerFileDetails("sqlbackups");

                    foreach (var file in sqlBackupFiles)
                    {
                        var t = (CommonDataFunctions.GetCurrentDateTime() - file.DateLastModified) ?? new TimeSpan();
                        if (t.TotalDays > 14)
                        {
                            AzureFunctions.DeleteSingleFileInBlobContainer("sqlbackups", file.Filename);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException("Error in CleanupSqlBackupFiles + Error: " + e.Message + " Stack: " + e.StackTrace);
            }
        }

        public static Ftp.FTPProtocol LookupFtpProtocol(int ftpProtocolFK)
        {
            var protocol = Ftp.FTPProtocol.FTP;

            try
            {
                var crud = new CrudLookup();
                var lookupRecord = crud.Read(x => x.LookupID == ftpProtocolFK).FirstOrDefault().LookupName;

                switch (lookupRecord)
                {
                    case "FTP":

                        protocol = Ftp.FTPProtocol.FTP;
                        break;

                    case "FTPS":

                        protocol = Ftp.FTPProtocol.FTPS;
                        break;

                    case "SFTP":

                        protocol = Ftp.FTPProtocol.SFTP;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Could not find FTP protocol for ftpProtocolFK: " + ftpProtocolFK + ". Message: " + ex.Message);
            }

            return protocol;
        }

        public static string GetRowFieldData(string[] row, int columnIndex)
        {
            string fieldData = "";

            if (columnIndex != -1)
            {
                fieldData = string.IsNullOrEmpty(row[columnIndex]) ? "" : row[columnIndex];
            }

            return fieldData;
        }
    }

    //Public Shared Classes
    public class SaveReturn
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string InnerException { get; set; }
        public string EntityValidationError { get; set; }
    }

    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
        }
    }

    public class ExtractZipFileResult
    {
        public bool IsSuccess { get; set; } = true;
        public Exception ErrorException { get; set; }
        public string Path { get; set; }
    }

    public class PriceRuleInfo
    {
        public PriceRule Rule { get; set; }
        public int ProductCount { get; set; }
    }
}