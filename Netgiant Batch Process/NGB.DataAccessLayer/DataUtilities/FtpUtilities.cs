using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Renci.SshNet;
using FluentFTP;
using System.Security.Authentication;
using static System.Net.WebRequestMethods;
using Renci.SshNet.Messages;
using FluentFTP.Rules;

namespace NGBP.DataAccessLayer.DataUtilities
{
    // TIDYUP
    public class FtpUtilities
    {
        public static DateTime FileLastModifiedDate { get; set; }

        /// <summary>
        /// This function will firstly get a list of files for a directory and download each file in the list with the given extension
        /// </summary>
        //public static void DownloadFTPFiles(string FTPDirectoryAddress, string FTPUsername, string FTPPassword, string localPath,
        //    string fileNameQuery, string fileExtension, bool ftpSsl = false)
        //{
        //    FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(FTPDirectoryAddress);
        //    requestDirectory.Timeout = -1;
        //    requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
        //    requestDirectory.Credentials = new NetworkCredential(FTPUsername, FTPPassword);
        //    requestDirectory.EnableSsl = ftpSsl;

        //    FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
        //    Stream responseStreamDirectory = responseDirectory.GetResponseStream();

        //    StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

        //    string[] fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');

        //    responseDirectory.Close();
        //    readerDirectory.Close();

        //    foreach (string file in fileList)
        //    {
        //        if (Path.GetFileName(file).ToLower().Contains(fileNameQuery.ToLower()) && Path.GetExtension(file) == ".zip")
        //        {
        //            FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(FTPDirectoryAddress + "/" + file);
        //            requestFile.Timeout = -1;
        //            requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
        //            requestFile.Credentials = new NetworkCredential(FTPUsername, FTPPassword);
        //            requestFile.EnableSsl = ftpSsl;

        //            FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
        //            Stream responseStreamFile = responseFile.GetResponseStream();

        //            //Name the file appropriately, for identification later
        //            FileStream fs = new FileStream(localPath + "\\" + file, FileMode.Create, FileAccess.Write);
        //            responseStreamFile.CopyTo(fs);
        //            fs.Close();

        //            responseFile.Close();
        //            responseStreamFile.Close();
        //        }
        //    }
        //}        

        /// <summary>
        /// This function will firstly get a list of files for a directory. [Next it will take the first file in the list
        /// and determine the last modified date???] and then download the file
        /// </summary>
        //public static void DownloadFTPFiles(string ftpHost, string ftpUser, string ftpPassword, string localPath, string filename,
        //    bool ftpSsl = false, string outputFilename = "")
        //{
        //    string host = ftpHost.StartsWith("ftp://") ? ftpHost : string.Format("ftp://{0}", ftpHost).Trim();
        //    outputFilename = string.IsNullOrEmpty(outputFilename) ? filename : outputFilename;

        //    //Create the ftp web request
        //    FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(host);
        //    requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
        //    requestDirectory.Credentials = new NetworkCredential(ftpUser, ftpPassword);
        //    requestDirectory.EnableSsl = ftpSsl;

        //    //if the ftp request is accepted by the provider, get the response
        //    FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
        //    Stream responseStreamDirectory = responseDirectory.GetResponseStream();

        //    StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

        //    string[] fileList;
        //    fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');
        //    string file = fileList.FirstOrDefault(x => x.Contains(filename.Trim()));
        //    if (!string.IsNullOrEmpty(file) && file.Contains("/"))
        //    {
        //        string[] nodes = file.Split('/');
        //        file = nodes[nodes.Length - 1];
        //    }

        //    responseDirectory.Close();
        //    readerDirectory.Close();

        //    FileLastModifiedDate = DateTime.Now;
        //    if (!string.IsNullOrEmpty(file))
        //    {
        //        FileLastModifiedDate = ExtractFileTimeStamp(host, ftpUser, ftpPassword, file);

        //        FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(string.Format("{0}/{1}", host, file));
        //        requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
        //        requestFile.Credentials = new NetworkCredential(ftpUser, ftpPassword);
        //        requestFile.EnableSsl = ftpSsl;

        //        FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
        //        Stream responseStreamFile = responseFile.GetResponseStream();

        //        //Name the file appropriately, for identification later
        //        FileStream fs = new FileStream(localPath + "\\" + outputFilename, FileMode.Create, FileAccess.Write);
        //        responseStreamFile.CopyTo(fs);
        //        fs.Close();

        //        responseFile.Close();
        //        responseStreamFile.Close();
        //    }
        //}               

        public static DateTime ExtractFileTimeStamp(string ftpAddress, string ftpUN, string ftpPW, string filePath, bool ftpSsl = false)
        {
            DateTime rtnDate;

            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                client.Config.TimeConversion = FtpDate.LocalTime;
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                client.Config.RetryAttempts = 3;
                client.Connect();
                FtpListItem li = client.GetObjectInfo(filePath);
                rtnDate = client.GetModifiedTime(filePath);
                rtnDate = li == null ? DateTime.Now : li.Modified;
                client.Disconnect();
            }
            catch (Exception e)
            {
                rtnDate = DateTime.Now.AddYears(-1);  // On error, return a really old date to stop processing
            }

            return rtnDate;
        }

        private static bool CheckFileExists(string ftpAddress, string ftpUN, string ftpPW, string filePath, bool ftpSsl = false)
        {
            bool rtn;

            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                client.Config.RetryAttempts = 3;
                client.Connect();
                rtn = client.FileExists(filePath);
                client.Disconnect();
            }
            catch (Exception e)
            {
                rtn = false;
            }

            return rtn;
        }

        public static List<string> GetFTPFolderNames(string ftpAddress, string ftpUN, string ftpPW, string path, bool ftpSsl = false)
        {
            List<string> folders = new List<string>();
            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                client.Config.RetryAttempts = 3;
                client.Connect();
                FtpListItem[] li = client.GetListing(path);
                client.Disconnect();

                foreach (FtpListItem liItem in li)
                {
                    folders.Add(liItem.Name);
                }
            }
            catch (Exception e)
            {
                // Do nothing
            }

            return folders;
        }        

        public static Tuple<bool, string> UploadFTPFile(string filePath, string ftpAddress, string ftpUN, string ftpPW, string ftpPath, bool ftpSsl = false)
        {
            bool isSuccess = true;
            string message = "";
            string fileName = filePath.Contains("\\") ? filePath.Split('\\').Last() : filePath;

            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
                client.Config.RetryAttempts = 3;
                client.Connect();
                client.UploadFile(filePath, ftpPath, FtpRemoteExists.OverwriteInPlace, false, FtpVerify.Retry);
                client.Disconnect();
            }
            catch (Exception e)
            {
                isSuccess = false;
                message = e.Message + (e.InnerException == null ? "" : e.InnerException.ToString());
            }

            return Tuple.Create(isSuccess, message);
        }

        public static void UploadSFTPFiles(string sftpHost, string sftpUser, string sftpPassword, string folderPath,
            string filename, int sftpPort = 22)
        {
            using (var sftp = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPassword))
            {
                sftp.Connect();

                if (folderPath != "")
                {
                    sftp.ChangeDirectory(folderPath);
                }
                using (var fileStream = new FileStream(filename, FileMode.Open))
                {
                    sftp.BufferSize = 4 * 1024; // bypass Payload error large files
                    sftp.UploadFile(fileStream, Path.GetFileName(filename));
                }

                sftp.Disconnect();
            }
        }

        /// <summary>
        /// Downloads a single file from FTP Server. Also set FileLastModifiedDate
        /// </summary>
        public static Tuple<bool, string> DownloadFTPFile(string ftpAddress, string ftpUN, string ftpPW, string ftpPath, string localPath,
            bool ftpSsl = false)
        {
            bool isSuccess = true;
            string message = "";

            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.RetryAttempts = 3;
                client.Connect();
                client.DownloadFile(localPath, ftpPath, FtpLocalExists.Overwrite, FtpVerify.None);
                FileLastModifiedDate = ExtractFileTimeStamp(ftpAddress, ftpUN, ftpPW, ftpPath);
                client.Disconnect();
            }
            catch (Exception e)
            {
                isSuccess = false;
                message = e.Message + (e.InnerException == null ? "" : e.InnerException.ToString());
            }

            return Tuple.Create(isSuccess, message);
        }

        /// <summary>
        /// Downloads all files from an FTP Folder into the given local folder whose filename contains the 
        /// queryString AND has the given file extension.
        /// </summary>
        public static Tuple<bool, string> DownloadFTPFiles(string ftpAddress, string ftpUN, string ftpPW, string ftpPath, string localPath,
            string queryStr = "", string fileExtn = "", bool ftpSsl = false)
        {
            bool isSuccess = true;
            string message = "";

            var rules = new List<FtpRule>();
            if (!String.IsNullOrEmpty(queryStr))
            {
                rules.Add(new FtpFileNameRegexRule(true, new List<string> { "^.*" + queryStr + ".*$" }));
            }
            if (!String.IsNullOrEmpty(fileExtn))
            {
                rules.Add(new FtpFileExtensionRule(true, new List<string> { fileExtn }));
            }

            try
            {
                // Use FluentFTP
                FtpClient client = new FtpClient(ftpAddress, ftpUN, ftpPW);
                //client.Config.LogToConsole = true;
                if (ftpSsl)
                {
                    client.Config.EncryptionMode = FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.RetryAttempts = 3;
                client.Connect();
                client.DownloadDirectory(localPath, ftpPath, FtpFolderSyncMode.Update, FtpLocalExists.Overwrite, FtpVerify.None, rules);
                client.Disconnect();
            }
            catch (Exception e)
            {
                isSuccess = false;
                message = e.Message + (e.InnerException == null ? "" : e.InnerException.ToString());
            }

            return Tuple.Create(isSuccess, message);
        }

        public static void DownloadSFTPFiles(string sftpHost, string sftpUser, string sftpPassword, string localPath, string folderPath,
            string filename, int sftpPort = 22)
        {
            using (var sftp = new SftpClient(sftpHost, sftpPort, sftpUser, sftpPassword))
            {
                sftp.Connect();

                using (var file = System.IO.File.OpenWrite(localPath + filename))
                {
                    sftp.DownloadFile(folderPath + filename, file);
                }

                sftp.Disconnect();
            }
        }        

        public static char DetectDelimiter(TextReader reader, int rowCount)
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
    }
}
