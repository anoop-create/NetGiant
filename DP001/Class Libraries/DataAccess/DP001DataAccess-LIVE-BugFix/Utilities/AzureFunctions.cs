using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.StorageClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Utilities
{
    public class AzureFunctions
    {
        public static bool UploadToBlobStorage(string blobContainer, string fileName, Stream stream)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var success = true;

            try
            {
                var account = CloudStorageAccount.Parse(StorageDP001String);
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference(blobContainer);
                container.CreateIfNotExists();
                
                var blob = container.GetBlockBlobReference(fileName);

                blob.UploadFromStream(stream);
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static bool UploadToBlobStorage(string blobContainer, string fileName, string content)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var success = true;

            try
            {
                var account = CloudStorageAccount.Parse(StorageDP001String);
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference(blobContainer);
                container.CreateIfNotExists();

                var blob = container.GetBlockBlobReference(fileName);

                blob.UploadText(content);
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static Dictionary<string, string> ListBlobContainerFilesAndContent(string blobContainer)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var account = CloudStorageAccount.Parse(StorageDP001String);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(blobContainer);
            var files = container.ListBlobs().OfType<CloudBlob>().Where(b => b.Name.EndsWith(".txt"));
            var results = new Dictionary<string, string>();

            foreach (var file in files)
            {
                results.Add(file.Name, file.DownloadText());
            }

            return results;
        }

        public static List<CloudBlobDetail> ListBlobContainerFileDetails(string blobContainer)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var account = CloudStorageAccount.Parse(StorageDP001String);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(blobContainer);
            var files = container.ListBlobs();
            var results = new List<CloudBlobDetail>();

            foreach (var file in files)
            {
                var blob = (Microsoft.WindowsAzure.Storage.Blob.CloudPageBlob)file;
                var detail = new CloudBlobDetail();
                detail.Filename = blob.Name;
                detail.DateLastModified = CommonDataFunctions.ConvertDateTimeToCurrentTimeZone(blob.Properties.LastModified ?? DateTime.Now);
                results.Add(detail);
            }

            return results;
        }

        public static bool DeleteFilesInBlobContianer(string blobContainer, List<string> filesToDelete)
        {
            var success = true;

            try
            {
                var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
                var account = CloudStorageAccount.Parse(StorageDP001String);
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference(blobContainer);
                var blobContainerFiles = container.ListBlobs().OfType<CloudBlob>();

                foreach (var file in filesToDelete)
                {
                    var lookupFile = blobContainerFiles.FirstOrDefault(x => x.Name == file);

                    if (lookupFile != null)
                    {
                        lookupFile.DeleteIfExists();
                    }
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static bool DeleteSingleFileInBlobContainer(string blobContainer, string fileName)
        {
            var success = true;

            try
            {
                var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
                var memoryStream = new MemoryStream();
                var account = CloudStorageAccount.Parse(StorageDP001String);
                var client = account.CreateCloudBlobClient();
                var container = client.GetContainerReference(blobContainer);
                var blob = container.GetBlockBlobReference(fileName);

                blob.DeleteIfExists();
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        public static string DownloadFileFromBlobToString(string blobContainer, string fileName)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var memoryStream = new MemoryStream();
            var account = CloudStorageAccount.Parse(StorageDP001String);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(blobContainer);
            var blob = container.GetBlockBlobReference(fileName);

            return blob.DownloadText();
        }

        public static Stream DownloadFileFromBlobToStream(string blobContainer, string fileName)
        {
            var StorageDP001String = ConfigurationManager.AppSettings["AzureStorageConnectionString"];
            var memoryStream = new MemoryStream();
            var account = CloudStorageAccount.Parse(StorageDP001String);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(blobContainer);
            var blob = container.GetBlockBlobReference(fileName);

            blob.DownloadToStream(memoryStream);

            memoryStream.Position = 0;

            return memoryStream;
        }

        public class CloudBlobDetail
        {
            public string Filename { get; set; }
            public DateTimeOffset? DateLastModified { get; set; }
        }
    }
}
