using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class FtpUtilities
    {
        public static DateTime FileLastModifiedDate { get; set; }

        public static void DownloadFTPFiles(string FTPDirectoryAddress,
                                            string FTPUsername,
                                            string FTPPassword,
                                            string localPath,
                                            string fileNameQuery,
                                            string fileExtension)
        {
            FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(FTPDirectoryAddress);
            requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
            requestDirectory.Credentials = new NetworkCredential(FTPUsername, FTPPassword);

            FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
            Stream responseStreamDirectory = responseDirectory.GetResponseStream();

            StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

            string[] fileList;
            fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');

            responseDirectory.Close();
            readerDirectory.Close();

            foreach (string file in fileList)
            {
                if (Path.GetFileName(file).ToLower().Contains(fileNameQuery.ToLower()) && Path.GetExtension(file) == ".zip")
                {
                    FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(FTPDirectoryAddress + file);
                    requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
                    requestFile.Credentials = new NetworkCredential(FTPUsername, FTPPassword);

                    FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
                    Stream responseStreamFile = responseFile.GetResponseStream();

                    //Name the file appropriately, for identification later
                    FileStream fs = new FileStream(localPath + "\\" + file, FileMode.Create, FileAccess.Write);
                    responseStreamFile.CopyTo(fs);
                    fs.Close();

                    responseFile.Close();
                    responseStreamFile.Close();
                }
            }
        }

        public static void DownloadFTPFiles(string ftpHost, string ftpUser, string ftpPassword, string localPath, string filename)
        {
            string host = ftpHost.StartsWith("ftp://") ? ftpHost : string.Format("ftp://{0}", ftpHost).Trim();

            //Create the ftp web request
            FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(host);
            requestDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
            requestDirectory.Credentials = new NetworkCredential(ftpUser, ftpPassword);

            //if the ftp request is accepted by the provider, get the response
            FtpWebResponse responseDirectory = (FtpWebResponse)requestDirectory.GetResponse();
            Stream responseStreamDirectory = responseDirectory.GetResponseStream();

            StreamReader readerDirectory = new StreamReader(responseStreamDirectory);

            string[] fileList;
            fileList = readerDirectory.ReadToEnd().Replace("\r", "").Split('\n');
            string file = fileList.FirstOrDefault(x => x.Contains(filename.Trim()));

            responseDirectory.Close();
            readerDirectory.Close();

            FileLastModifiedDate = ExtractFileTimeStamp(host, ftpUser, ftpPassword, file);

            if (!string.IsNullOrEmpty(file))
            {
                FtpWebRequest requestFile = (FtpWebRequest)WebRequest.Create(string.Format("{0}/{1}", host, file));
                requestFile.Method = WebRequestMethods.Ftp.DownloadFile;
                requestFile.Credentials = new NetworkCredential(ftpUser, ftpPassword);

                FtpWebResponse responseFile = (FtpWebResponse)requestFile.GetResponse();
                Stream responseStreamFile = responseFile.GetResponseStream();

                //Name the file appropriately, for identification later
                FileStream fs = new FileStream(localPath + "\\" + file, FileMode.Create, FileAccess.Write);
                responseStreamFile.CopyTo(fs);
                fs.Close();

                responseFile.Close();
                responseStreamFile.Close();
            }
        }

        private static DateTime ExtractFileTimeStamp(string host, string ftpUser, string ftpPassword, string file)
        {
            DateTime returnValue = DateTime.MinValue;

            //Create the ftp web request
            FtpWebRequest requestDirectory = (FtpWebRequest)WebRequest.Create(string.Format("{0}/{1}", host, file));
            requestDirectory.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            requestDirectory.Credentials = new NetworkCredential(ftpUser, ftpPassword);

            //if the ftp request is accepted by the provider, get the response
            FtpWebResponse responseFile = (FtpWebResponse)requestDirectory.GetResponse();
            returnValue = responseFile.LastModified;

            responseFile.Close();

            return returnValue;
        }

        public static char DetectDelimiter(TextReader reader, int rowCount)
        {
            char[] delimiters = new char[] { ',', '\t' };

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

        public static void UploadFTPFile(string filePath, string ftpAddress, string ftpUN, string ftpPW)
        {

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAddress);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            request.Credentials = new NetworkCredential(ftpUN, ftpPW);

            StreamReader sourceStream = new StreamReader(filePath);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

        }
    }
}
