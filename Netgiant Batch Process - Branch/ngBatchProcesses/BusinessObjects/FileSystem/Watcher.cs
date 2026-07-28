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
            StandardFunctions stnFunc = new StandardFunctions();

            try
            {
                stnFunc.AddToActivityLog("Attempting to Start File System Watcher for folder: " + folderPath);

                FileSystemWatcher watcher = new FileSystemWatcher(folderPath);
                watcher.Filter = "*.txt";
                watcher.Created += new FileSystemEventHandler(OnFileCreated);
                watcher.EnableRaisingEvents = true;

                stnFunc.AddToActivityLog("Successfully started File System Watcher for folder: " + folderPath);
                stnFunc.AddToActivityLog("Waiting for new files in folder: " + folderPath);
                stnFunc.LogActivity("filesystemwatcher");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error stating File System Watcher for folder: " + folderPath);
                stnFunc.AddToActivityLog("Error: " + ex.Message);
                string logFilePath = stnFunc.LogActivity("filesystemwatcher");
                stnFunc.SendSimpleEmail("Error Attempting to start File System Watcher for folder: " + folderPath, logFilePath);
            }
        }

        private static void OnFileCreated(object source, FileSystemEventArgs e)
        {
            while (IsFileInUse(new FileInfo(e.FullPath)))
            {
                Thread.Sleep(500);
            }

            StandardFunctions stnFunc = new StandardFunctions();
            string fileName = Path.GetFileName(e.FullPath).ToLower();
            bool errorOccurred = false;
            string fileText = "";

            try
            {
                fileText = File.ReadAllText(e.FullPath);
                string[] args = fileText.Split(null);

                stnFunc.AddToActivityLog("Attempted to start task via FileSystemWatcher: " + fileName);
                stnFunc.AddToActivityLog("Arguments are: " + fileText);

                SwitchDetection.DetectSwitch(args);

                if (File.Exists(e.FullPath))
                    File.Delete(e.FullPath);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error starting task from FileSystemWatcher: " + fileName);
                stnFunc.AddToActivityLog("Error: " + ex.Message);
                errorOccurred = true;
            }

            string logFilePath = stnFunc.LogActivity("filesystemwatcher");
            if (errorOccurred)
                stnFunc.SendSimpleEmail("Error Running FileSystemWatcher: " + fileName, logFilePath);
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
