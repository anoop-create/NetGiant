using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;

namespace ngBatchProcesses.BusinessObjects.TrackingEmails
{
    class FTP
    {
        public static void GetFTPFiles()
        {
            try
            {
                RetrieveFiles();
                RetrieveVowFile();
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Retrieving FTP Files", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        private static void RetrieveFiles()
        {
            //Set variables from App.config
            string FTPAD = (string)Properties.Settings.Default["FTPAddress"];
            string FTPUN = (string)Properties.Settings.Default["FTPUsername"];
            string FTPPW = (string)Properties.Settings.Default["FTPPassword"];
            string localPath = (string)Properties.Settings.Default["FilePath"];
            if (ConfigurationManager.AppSettings["Environment"] == "Local")
            {
                localPath = "C:\\DeliveryTracking\\New";
            }
            string[] directoryList;

            //Generate a list of the directories within the Delivery folder
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FTPAD + "Delivery/");
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(FTPUN, FTPPW);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(responseStream);
            directoryList = reader.ReadToEnd().Replace("\r", "").Split('\n');

            response.Close();
            responseStream.Close();

            //Iterate each directory, creating a list of filenames within that directory
            foreach (string directory in directoryList)
            {
                if (directory.Length > 0)
                {
                    string[] fileList;

                    FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(FTPAD + "Delivery/" + directory + "/");
                    requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
                    requestDirectory.Credentials = new NetworkCredential(FTPUN, FTPPW);

                    FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
                    Stream responseStreamDirectory = responseDirectory.GetResponseStream();

                    StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

                    fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');

                    responseDirectory.Close();
                    readerDirectory.Close();

                    //Iterate the filenames list, copying each file to the server using a filestream
                    foreach (string file in fileList)
                    {
                        if (file.Length > 0)
                        {
                            FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(FTPAD + "Delivery/" + directory + "/" + file);
                            requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
                            requestFile.Credentials = new NetworkCredential(FTPUN, FTPPW);

                            FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
                            Stream responseStreamFile = responseFile.GetResponseStream();

                            //Name the file appropriately, for identification later
                            FileStream fs = new FileStream(localPath + "\\" + directory + "_" + file, FileMode.Create, FileAccess.Write);
                            responseStreamFile.CopyTo(fs);
                            fs.Close();

                            responseFile.Close();
                            responseStreamFile.Close();

                            //Check if the file has been successfully downloaded from the FTP, delete from FTP if it has
                            if (File.Exists(localPath + "\\" + directory + "_" + file))
                            {
                                //Delete the file from the FTP server, only if the file now successfully exists on the server
                                FtpWebRequest requestDeleteFile = (FtpWebRequest)WebRequest.Create(FTPAD + "/Delivery/" + directory + "/" + file);
                                requestDeleteFile.Method = WebRequestMethods.Ftp.DeleteFile;
                                requestDeleteFile.Credentials = new NetworkCredential(FTPUN, FTPPW);

                                FtpWebResponse responseDeleteFile = (FtpWebResponse)requestDeleteFile.GetResponse();
                                Stream responseStreamDeleteFile = responseDeleteFile.GetResponseStream();

                                responseDeleteFile.Close();
                                responseStreamDeleteFile.Close();
                            }
                        }
                    }
                }
            }
        }
        private static void RetrieveVowFile()
        {
            string[] fileList;
            string vowFTPAddress = (string)Properties.Settings.Default["VowFTPAddress"];
            string vowFTPUsername = (string)Properties.Settings.Default["VowFTPUsername"];
            string vowFTPPassword = (string)Properties.Settings.Default["VowFTPPassword"];
            string localPath = (string)Properties.Settings.Default["FilePath"];

            FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(vowFTPAddress);
            requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
            requestDirectory.Credentials = new NetworkCredential(vowFTPUsername, vowFTPPassword);

            FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
            Stream responseStreamDirectory = responseDirectory.GetResponseStream();

            StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

            fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');

            responseDirectory.Close();
            readerDirectory.Close();

            //Iterate the filenames list, copying each file to the server using a filestream
            foreach (string file in fileList)
            {
                if (file.Length > 0)
                {
                    FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(vowFTPAddress + "/" + file);
                    requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
                    requestFile.Credentials = new NetworkCredential(vowFTPUsername, vowFTPPassword);

                    FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
                    Stream responseStreamFile = responseFile.GetResponseStream();

                    //Name the file appropriately, for identification later
                    FileStream fs = new FileStream(localPath + "\\Vow_" + file, FileMode.Create, FileAccess.Write);
                    responseStreamFile.CopyTo(fs);
                    fs.Close();

                    responseFile.Close();
                    responseStreamFile.Close();

                    //Check if the file has been successfully downloaded from the FTP, delete from FTP if it has
                    if (File.Exists(localPath + "\\Vow_" + file))
                    {
                        //Delete the file from the FTP server, only if the file now successfully exists on the server
                        FtpWebRequest requestDeleteFile = (FtpWebRequest)WebRequest.Create(vowFTPAddress + "/" + file);
                        requestDeleteFile.Method = WebRequestMethods.Ftp.DeleteFile;
                        requestDeleteFile.Credentials = new NetworkCredential(vowFTPUsername, vowFTPPassword);

                        FtpWebResponse responseDeleteFile = (FtpWebResponse)requestDeleteFile.GetResponse();
                        Stream responseStreamDeleteFile = responseDeleteFile.GetResponseStream();

                        responseDeleteFile.Close();
                        responseStreamDeleteFile.Close();
                    }
                }
            }
        }
    }
}
