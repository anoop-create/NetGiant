using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.IO;
using System.Threading;

namespace ngBatchProcesses.BusinessObjects.FileSystem
{
    public class Watcher
    {
        /// <summary>
        /// Utilizes the FileSysmtemWatcher class to monitor a single specified folder for changes.
        /// </summary>
        public static void WatchFolder(string folderPath)
        {
            try
            {
                StandardFunctions.WriteProcessStarted();

                FileSystemWatcher watcher = new FileSystemWatcher(folderPath);
                watcher.Filter = "*.txt";
                watcher.Created += new FileSystemEventHandler(OnFileCreated);
                watcher.EnableRaisingEvents = true;

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully started File System Watcher for folder: " + folderPath });
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Waiting for new files in folder: " + folderPath });
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR stating File System Watcher for folder: " + folderPath, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
            }
        }

        private static void OnFileCreated(object source, FileSystemEventArgs e)
        {
            while (IsFileInUse(new FileInfo(e.FullPath)))
            {
                Thread.Sleep(500);
            }

            string fileName = Path.GetFileName(e.FullPath).ToLower();
            string fileText = "";

            try
            {
                fileText = File.ReadAllText(e.FullPath);
                string[] args = fileText.Split(null);

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Attempted to start task via FileSystemWatcher: " + fileName });
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Arguments are: " + fileText });

                SwitchDetection.DetectSwitch(args);

                if (File.Exists(e.FullPath))
                    File.Delete(e.FullPath);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR starting task from FileSystemWatcher: " + fileName, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        private static bool IsFileInUse(FileInfo file)
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
    }
}
