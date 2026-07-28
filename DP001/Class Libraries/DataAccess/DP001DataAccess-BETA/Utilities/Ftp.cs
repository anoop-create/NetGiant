using FluentFTP;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.FtpClient;
using Microsoft.VisualBasic.FileIO;
using Renci.SshNet.Messages;
using static System.Net.WebRequestMethods;

namespace DP001DataAccess.Utilities
{
    public class Ftp
    {
        /// <summary>
        /// Downloads a single file from FTP Server. Also set FileLastModifiedDate
        /// </summary>
        public static FtpDownloadResult DownloadFTPFile(FtpHostDetails hostDetails)
        {
            var ftpResult = new FtpDownloadResult();
            var platform = CommonDataFunctions.GetPlatformType();
            ftpResult.Platform = platform;
            ftpResult.Host = hostDetails.FtpHost;
            ftpResult.FileName = hostDetails.FileName;
            var downloadPath = ConfigurationManager.AppSettings["LocalDirectory"] + @"\DP001\";
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath + "\\" + hostDetails.SavePath));
            ftpResult.Path = downloadPath + "\\" + hostDetails.SavePath;            
            ftpResult.IsSuccess = true;

            try
            {
                // Use FluentFTP
                FluentFTP.FtpClient client = new FluentFTP.FtpClient(hostDetails.FtpHost, hostDetails.FtpUser, hostDetails.FtpPassword);
                //client.Config.LogToConsole = true;
                if (hostDetails.Protocol == FTPProtocol.FTPS)
                {
                    client.Config.EncryptionMode = FluentFTP.FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.RetryAttempts = 3;
                client.Connect();
                // Check file exists
                if (client.FileExists("/" + hostDetails.FolderPath + "/" + hostDetails.FileName))
                {
                    // Get Date/Time stamp
                    FluentFTP.FtpListItem li = client.GetObjectInfo("/" + hostDetails.FolderPath + "/" + hostDetails.FileName);
                    ftpResult.FileCreated = li == null ? DateTime.Now : li.Modified;
                    // Download the file
                    client.DownloadFile(downloadPath + hostDetails.SavePath,
                        "/" + hostDetails.FolderPath + "/" + hostDetails.FileName,
                        FtpLocalExists.Overwrite, FtpVerify.Retry);
                }
                else
                {
                    ftpResult.IsSuccess = false;
                    ftpResult.ErrorMessage = "Could not find the file specified on the server";
                }
                client.Disconnect();
            }
            catch (Exception ex)
            {
                ftpResult.IsSuccess = false;
                ftpResult.ErrorMessage = ex.Message;
                ftpResult.ErrorInnerException = ex.InnerException?.ToString();
            }

            return ftpResult;
        }
        //public static FtpDownloadResult DownloadFTPFile(FtpHostDetails hostDetails)
        //{
        //    var ftpResult = new FtpDownloadResult();
        //    var platform = CommonDataFunctions.GetPlatformType();
        //    ftpResult.Platform = platform;
        //    ftpResult.Host = hostDetails.FtpHost;
        //    ftpResult.FileName = hostDetails.FileName;

        //    try
        //    {
        //        if (hostDetails.Protocol == FTPProtocol.FTP || hostDetails.Protocol == FTPProtocol.FTPS)
        //        {
        //            hostDetails.FtpHost = hostDetails.FtpHost + hostDetails.FolderPath;
        //            hostDetails.FtpHost = hostDetails.FtpHost.StartsWith("ftp://") ? hostDetails.FtpHost : string.Format("ftp://{0}", hostDetails.FtpHost).Trim();

        //            var requestDirectory = InitializeFtpConn(
        //                hostDetails.FtpHost,
        //                hostDetails.FtpUser,
        //                hostDetails.FtpPassword,
        //                WebRequestMethods.Ftp.ListDirectory,
        //                hostDetails.Protocol);

        //            var readerDirectory = GetFtpResponse(requestDirectory);

        //            string[] fileList;
        //            fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');
        //            string file = fileList.FirstOrDefault(x => x.Equals(hostDetails.FileName));

        //            readerDirectory.Close();

        //            if (!string.IsNullOrEmpty(file))
        //            {
        //                var requestFile = InitializeFtpConn(
        //                    string.Format("{0}/{1}", hostDetails.FtpHost, hostDetails.FileName),
        //                    hostDetails.FtpUser,
        //                    hostDetails.FtpPassword,
        //                    WebRequestMethods.Ftp.DownloadFile,
        //                    hostDetails.Protocol);

        //                using (var responseFile = (FtpWebResponse)requestFile.GetResponse())
        //                using (var responseStreamFile = responseFile.GetResponseStream())
        //                {
        //                    switch (platform)
        //                    {
        //                        case PlatformType.Server:

        //                            var downloadPath = ConfigurationManager.AppSettings["LocalDirectory"] + @"\DP001\";
        //                            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath + "\\" + hostDetails.SavePath));
        //                            FileStream fs = new FileStream(downloadPath + "\\" + hostDetails.SavePath, FileMode.Create, FileAccess.Write);
        //                            responseStreamFile.CopyTo(fs);
        //                            fs.Close();
        //                            ftpResult.Path = downloadPath + "\\" + hostDetails.SavePath;

        //                            break;

        //                        case PlatformType.Azure:

        //                            //using (var stream = new MemoryStream())
        //                            //using (var reader = new StreamReader(stream))
        //                            //{
        //                            //    responseFile.GetResponseStream().CopyTo(stream);
        //                            //    stream.Position = 0;

        //                            //    if (Path.GetExtension(hostDetails.FileName) == ".zip")
        //                            //    {
        //                            //        AzureFunctions.UploadToBlobStorage(hostDetails.BlobContainer, hostDetails.SavePath, stream);
        //                            //    }
        //                            //    else
        //                            //    {
        //                            //        AzureFunctions.UploadToBlobStorage(hostDetails.BlobContainer, hostDetails.SavePath, reader.ReadToEnd());
        //                            //    }
        //                            //}

        //                            //ftpResult.Path = hostDetails.SavePath;
        //                            //ftpResult.BlobContainer = hostDetails.BlobContainer;

        //                            break;

        //                    }

        //                    ftpResult.FileCreated = ExtractFeedTimestamp(hostDetails);
        //                    ftpResult.IsSuccess = true;
        //                }
        //            }
        //            else
        //            {
        //                ftpResult.IsSuccess = false;
        //                ftpResult.ErrorMessage = "Could not find the file specified on the server";
        //            }
        //        }
        //        else if (hostDetails.Protocol == FTPProtocol.SFTP)
        //        {
        //            DownloadSftp(hostDetails, ftpResult, platform);
        //            ftpResult.IsSuccess = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ftpResult.IsSuccess = false;
        //        ftpResult.ErrorMessage = ex.Message;
        //        ftpResult.ErrorInnerException = ex.InnerException?.ToString();
        //        ftpResult.Platform = platform;
        //    }

        //    return ftpResult;
        //}

        public static void UploadFTPFile(FtpHostDetails hostDetails, MemoryStream stream)
        {
            if (hostDetails.Protocol == FTPProtocol.FTP || hostDetails.Protocol == FTPProtocol.FTPS)
            {
                hostDetails.FolderPath = hostDetails.FolderPath != "" ? "/" + hostDetails.FolderPath : "";

                // Create a file from the memory stream
                //var tempFilePath = ConfigurationManager.AppSettings["LocalDirectory"] + @"DP001\temp\" + hostDetails.SavePath;
                //using (FileStream file = new FileStream(tempFilePath, FileMode.Create, System.IO.FileAccess.Write))
                //{
                //    byte[] bytes = new byte[stream.Length];
                //    stream.Read(bytes, 0, (int)stream.Length);
                //    file.Write(bytes, 0, bytes.Length);
                //    stream.Close();
                //}

                // Use FluentFTP
                FluentFTP.FtpClient client = new FluentFTP.FtpClient(hostDetails.FtpHost, hostDetails.FtpUser, hostDetails.FtpPassword);
                //client.Config.LogToConsole = true;
                if (hostDetails.Protocol == FTPProtocol.FTPS)
                {
                    client.Config.EncryptionMode = FluentFTP.FtpEncryptionMode.Auto;
                    client.Config.ValidateAnyCertificate = true;
                }
                client.Config.DataConnectionType = FluentFTP.FtpDataConnectionType.AutoPassive;
                client.Config.RetryAttempts = 3;
                client.Connect();
                //client.UploadFile(tempFilePath, hostDetails.FolderPath + "/" + hostDetails.FileName, FtpRemoteExists.OverwriteInPlace, false, FtpVerify.Retry);
                client.UploadStream(stream, hostDetails.FolderPath + "/" + hostDetails.FileName, FtpRemoteExists.OverwriteInPlace);
                client.Disconnect();

                //System.IO.File.Delete(tempFilePath);
            }
            else if (hostDetails.Protocol == FTPProtocol.SFTP)
            {
                UploadSftp(hostDetails, stream);
            }
        }

        //public static void UploadFTPFile(FtpHostDetails hostDetails, MemoryStream stream)
        //{
        //    if (hostDetails.Protocol == FTPProtocol.FTP || hostDetails.Protocol == FTPProtocol.FTPS)
        //    {
        //        hostDetails.FolderPath = hostDetails.FolderPath != "" ? "/" + hostDetails.FolderPath : "";
        //        hostDetails.FtpHost = hostDetails.FtpHost.StartsWith("ftp://") ? hostDetails.FtpHost : string.Format("ftp://{0}", hostDetails.FtpHost).Trim();
        //        var ftpHostFolder = hostDetails.FtpHost + hostDetails.FolderPath + "/" + hostDetails.FileName;

        //        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpHostFolder);
        //        request.Method = WebRequestMethods.Ftp.UploadFile;
        //        request.Credentials = new NetworkCredential(hostDetails.FtpUser, hostDetails.FtpPassword);
        //        request.KeepAlive = false;

        //        if (hostDetails.Protocol == FTPProtocol.FTPS)
        //        {
        //            request.EnableSsl = true;
        //        }

        //        request.KeepAlive = false;

        //        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

        //        using (Stream requestStream = request.GetRequestStream())
        //        {
        //            byte[] fileContents = stream.ToArray();
        //            request.ContentLength = fileContents.Length;
        //            requestStream.Write(fileContents, 0, fileContents.Length);
        //            requestStream.Close();
        //        }

        //        request = null;
        //    }
        //    else if (hostDetails.Protocol == FTPProtocol.SFTP)
        //    {
        //        UploadSftp(hostDetails, stream);
        //    }
        //}

        public static bool TestFTPConnection(FtpHostDetails hostDetails)
        {
            try
            {
                if (hostDetails.Protocol == FTPProtocol.FTP || hostDetails.Protocol == FTPProtocol.FTPS)
                {
                    hostDetails.FtpHost = hostDetails.FtpHost;
                    hostDetails.FtpHost = hostDetails.FtpHost.StartsWith("ftp://") ? hostDetails.FtpHost : string.Format("ftp://{0}", hostDetails.FtpHost).Trim();

                    var client = InitializeFtpConn(
                        hostDetails.FtpHost,
                        hostDetails.FtpUser,
                        hostDetails.FtpPassword,
                        hostDetails.Protocol);
                    client.Connect();
                    FluentFTP.FtpListItem[] fli = client.GetListing(hostDetails.FolderPath);
                    client.Disconnect();
                }
                else if (hostDetails.Protocol == FTPProtocol.SFTP)
                {
                    using (var sftp = new SftpClient(hostDetails.FtpHost, hostDetails.FtpUser, hostDetails.FtpPassword))
                    {
                        sftp.Connect();
                        sftp.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                var e = ex;
                return false;
            }

            return true;
        }

        public static char DetectDelimiter(TextReader reader, int rowCount)
        {
            char[] delimiters = new char[] { ',', ';', '\t', '.', ':', '|' };

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

        public static FtpDownloadResult CopyAllFilesFromFtpToFtp(FtpCopyDetails ftpDetails, List<string> excludeFiles = null)
        {
            // Used by SITC code. So no longer used.

            var result = new FtpDownloadResult();

            //if (excludeFiles == null)
            //    excludeFiles = new List<string>();

            //try
            //{
            //    if (ftpDetails.SourceProtocol == FTPProtocol.FTP || ftpDetails.SourceProtocol == FTPProtocol.FTPS)
            //    {
            //        string sourceHost = ftpDetails.SourceHost.StartsWith("ftp://") ? ftpDetails.SourceHost : string.Format("ftp://{0}", ftpDetails.SourceHost).Trim();
            //        string destinationHost = ftpDetails.DestinationHost.StartsWith("ftp://") ? ftpDetails.DestinationHost : string.Format("ftp://{0}", ftpDetails.DestinationHost).Trim() + "/";

            //        using (var ftpClient = new System.Net.FtpClient.FtpClient())
            //        {
            //            ftpClient.Host = ftpDetails.SourceHost;
            //            ftpClient.Credentials = new NetworkCredential(ftpDetails.SourceUsername, ftpDetails.SourcePassword);
            //            ftpClient.DataConnectionType = System.Net.FtpClient.FtpDataConnectionType.PASV;
            //            ftpClient.SetWorkingDirectory(ftpDetails.SourceFolderPath);

            //            if (ftpDetails.SourceProtocol == FTPProtocol.FTPS)
            //            {
            //                ftpClient.EncryptionMode = System.Net.FtpClient.FtpEncryptionMode.Explicit;
            //                ftpClient.ValidateCertificate += new System.Net.FtpClient.FtpSslValidation(CertificateValidation);
            //            }

            //            ftpClient.Connect();

            //            foreach (var file in ftpClient.GetListing())
            //            {
            //                if (!excludeFiles.Contains(file.Name) && file.Type != FtpFileSystemObjectType.Directory)
            //                {
            //                    using (var ftpStream = ftpClient.OpenRead(file.FullName))
            //                    {
            //                        using (var ms = new MemoryStream())
            //                        {
            //                            ftpStream.CopyTo(ms);
            //                            ms.Position = 0;

            //                            var hostDetails = new FtpHostDetails()
            //                            {
            //                                BlobContainer = "tenantfolders",
            //                                FileName = file.Name,
            //                                FtpHost = ftpDetails.DestinationHost,
            //                                FtpUser = ftpDetails.DestinationUsername,
            //                                FtpPassword = ftpDetails.DestinationPassword,
            //                                Protocol = ftpDetails.DestinationProtocol,
            //                                FolderPath = ftpDetails.DestinationFolderPath
            //                            };

            //                            UploadFTPFile1(hostDetails, ms);
            //                        }
            //                    }
            //                }
            //            }

            //            ftpClient.Disconnect();
            //        }

            //        result.IsSuccess = true;
            //    }
            //    else if (ftpDetails.SourceProtocol == FTPProtocol.SFTP)
            //    {
            //        using (var sftp = new SftpClient(ftpDetails.SourceHost, ftpDetails.SourceUsername, ftpDetails.SourcePassword))
            //        {
            //            sftp.Connect();

            //            foreach (var file in sftp.ListDirectory(ftpDetails.SourceFolderPath ?? "/"))
            //            {
            //                if (!excludeFiles.Contains(file.Name) && !file.IsDirectory)
            //                {
            //                    using (var ftpStream = sftp.OpenRead(file.FullName))
            //                    {
            //                        using (var ms = new MemoryStream())
            //                        {
            //                            ftpStream.CopyTo(ms);
            //                            ms.Position = 0;

            //                            var hostDetails = new FtpHostDetails()
            //                            {
            //                                BlobContainer = "tenantfolders",
            //                                FileName = file.Name,
            //                                FtpHost = ftpDetails.DestinationHost,
            //                                FtpUser = ftpDetails.DestinationUsername,
            //                                FtpPassword = ftpDetails.DestinationPassword,
            //                                Protocol = ftpDetails.DestinationProtocol,
            //                                FolderPath = ftpDetails.DestinationFolderPath
            //                            };

            //                            UploadFTPFile1(hostDetails, ms);
            //                        }
            //                    }
            //                }
            //            }

            //            result.IsSuccess = true;
            //            sftp.Disconnect();
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    result.IsSuccess = false;
            //    result.ErrorMessage = e.Message;
            //}

            return result;
        }

        //public static FtpDownloadResult RenameAllFtpFiles(string host, string user, string password, string prefix)
        //{
        //    // Not used.
        //    var result = new FtpDownloadResult();

        //    try
        //    {
        //        string sourceHost = host.StartsWith("ftp://") ? host : string.Format("ftp://{0}", host).Trim();

        //        //Create the ftp web request
        //        FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(sourceHost);
        //        requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
        //        requestDirectory.Credentials = new NetworkCredential(user, password);
        //        requestDirectory.KeepAlive = false;

        //        //if the ftp request is accepted by the provider, get the response
        //        FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
        //        Stream responseStreamDirectory = responseDirectory.GetResponseStream();

        //        StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

        //        string[] fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');

        //        responseDirectory.Close();
        //        readerDirectory.Close();

        //        foreach (var file in fileList)
        //        {
        //            if (!string.IsNullOrEmpty(file))
        //            {
        //                FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(string.Format("{0}/{1}", sourceHost, file));
        //                requestFile.Method = WebRequestMethods.Ftp.Rename;
        //                requestFile.Credentials = new NetworkCredential(user, password);
        //                requestFile.KeepAlive = false;
        //                requestFile.RenameTo = prefix + file;
        //                var response = (FtpWebResponse)requestFile.GetResponse();
        //                response.Dispose();
        //            }
        //        }

        //        result.IsSuccess = true;
        //    }
        //    catch (Exception e)
        //    {
        //        result.IsSuccess = false;
        //        result.ErrorMessage = e.Message;
        //    }

        //    return result;
        //}

        private static void CertificateValidation(System.Net.FtpClient.FtpClient control, System.Net.FtpClient.FtpSslValidationEventArgs e)
        {
            e.Accept = true;
        } 

        public class FtpHostDetails
        {
            public string FtpHost { get; set; }
            public string FolderPath { get; set; }
            public string FtpUser { get; set; }
            public string FtpPassword { get; set; }
            public string FileName { get; set; }
            public string SavePath { get; set; }
            public FTPProtocol Protocol { get; set; }
            public string BlobContainer { get; set; }
        }

        public enum FTPProtocol
        {
            FTP,
            FTPS,
            SFTP
        }

        private static FluentFTP.FtpClient InitializeFtpConn(
            string host,
            string username,
            string password,
            FTPProtocol protocol)
        {
            // Use FluentFTP
            FluentFTP.FtpClient client = new FluentFTP.FtpClient(host, username, password);
            //client.Config.LogToConsole = true;
            if (protocol == FTPProtocol.FTPS)
            {
                client.Config.EncryptionMode = FluentFTP.FtpEncryptionMode.Auto;
                client.Config.ValidateAnyCertificate = true;
            }
            client.Config.DataConnectionType = FluentFTP.FtpDataConnectionType.AutoPassive;
            client.Config.RetryAttempts = 3;
            return client;
        }

        //private static StreamReader GetFtpResponse(FtpWebRequest request)
        //{
        //    // Not used.
        //    var responseDirectory = (FtpWebResponse)request.GetResponse();
        //    var responseStream = responseDirectory.GetResponseStream();
        //    var readerDirectory = new StreamReader(responseStream);

        //    return readerDirectory;
        //}

        private static void DownloadSftp(FtpHostDetails hostDetails, FtpDownloadResult ftpResult, PlatformType platform)
        {
            using (var sftp = new SftpClient(hostDetails.FtpHost, hostDetails.FtpUser, hostDetails.FtpPassword))
            {
                sftp.Connect();

                ftpResult.FileCreated = sftp.GetLastWriteTime(hostDetails.FolderPath + hostDetails.FileName);

                switch (platform)
                {
                    case PlatformType.Server:

                        var downloadPath = ConfigurationManager.AppSettings["LocalDirectory"] + @"\DP001\";
                        Directory.CreateDirectory(Path.GetDirectoryName(downloadPath + "\\" + hostDetails.SavePath));
                        FileStream fs = new FileStream(downloadPath + "\\" + hostDetails.SavePath, FileMode.Create, FileAccess.Write);
                        sftp.DownloadFile(hostDetails.FolderPath + hostDetails.FileName, fs);
                        fs.Close();
                        ftpResult.Path = downloadPath + "\\" + hostDetails.SavePath;

                        break;

                    case PlatformType.Azure:

                        //using (var stream = new MemoryStream())
                        //using (var reader = new StreamReader(stream))
                        //{
                        //    sftp.DownloadFile(hostDetails.FolderPath + "\\" + hostDetails.FileName, stream);
                        //    stream.Position = 0;

                        //    if (Path.GetExtension(hostDetails.FileName) == ".zip")
                        //    {
                        //        AzureFunctions.UploadToBlobStorage(hostDetails.BlobContainer, hostDetails.SavePath, stream);
                        //    }
                        //    else
                        //    {
                        //        AzureFunctions.UploadToBlobStorage(hostDetails.BlobContainer, hostDetails.SavePath, reader.ReadToEnd());
                        //    }
                        //}

                        //ftpResult.Path = hostDetails.SavePath;
                        //ftpResult.BlobContainer = hostDetails.BlobContainer;

                        break;
                }

                sftp.Disconnect();
            }
        }

        private static void UploadSftp(FtpHostDetails hostDetails, MemoryStream stream)
        {
            using (var sftp = new SftpClient(hostDetails.FtpHost, hostDetails.FtpUser, hostDetails.FtpPassword))
            {
                sftp.Connect();

                var filePath = hostDetails.FolderPath != "" ? "/" + hostDetails.FolderPath + "/" + hostDetails.FileName : hostDetails.FileName;

                stream.Position = 0;
                sftp.UploadFile(stream, filePath);

                sftp.Disconnect();
            }
        }

        //private static DateTime? ExtractFeedTimestamp(FtpHostDetails hostDetails)
        //{
        //    // Not used.
        //    DateTime? lastModifiedDate = null;
        //    var client = InitializeFtpConn(
        //                    hostDetails.FtpHost,
        //                    hostDetails.FtpUser,
        //                    hostDetails.FtpPassword,
        //                    hostDetails.Protocol);
        //    client.Connect();
        //    FluentFTP.FtpListItem li = client.GetObjectInfo("/" + hostDetails.FolderPath + "/" + hostDetails.FileName);
        //    lastModifiedDate = li == null ? DateTime.Now : li.Modified;
        //    client.Disconnect();

        //    return lastModifiedDate;
        //}
    }

    public class FtpDownloadResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = null;
        public string ErrorInnerException { get; set; } = null;
        public PlatformType? Platform { get; set; } = null;
        public string Path { get; set; } = null;
        public string BlobContainer { get; set; } = null;
        public string FileName { get; set; }
        public string Host { get; set; }
        public DateTime? FileCreated { get; set; }
    }

    public class FtpCopyDetails
    {
        public string SourceHost { get; set; }
        public string SourceUsername { get; set; }
        public string SourcePassword { get; set; }
        public string SourceFolderPath { get; set; }
        public Ftp.FTPProtocol SourceProtocol { get; set; }
        public string DestinationHost { get; set; }
        public string DestinationUsername { get; set; }
        public string DestinationPassword { get; set; }
        public string DestinationFolderPath { get; set; }
        public Ftp.FTPProtocol DestinationProtocol { get; set; }
    }
}
