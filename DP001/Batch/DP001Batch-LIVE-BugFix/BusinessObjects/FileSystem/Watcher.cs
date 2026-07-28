using DP001Batch.BusinessObjects.Shared;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DP001Batch.BusinessObjects.FileSystem
{
    public class Watcher
    {
        /// <summary>
        /// Utilizes the FileSysmtemWatcher class to monitor a single specified folder for changes.
        /// </summary>
        public static void WatchFolder(string folderPath)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(folderPath);
            watcher.Filter = "*.txt";
            watcher.Created += new FileSystemEventHandler(OnFileCreated);
            watcher.EnableRaisingEvents = true;

            Console.ReadLine();
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            while (CommonFunctions.IsFileInUse(new FileInfo(e.FullPath)))
            {
                Thread.Sleep(500);
            }

            try
            {
                var fileText = File.ReadAllText(e.FullPath);
                string[] args = fileText.Split(null);
                var dictionary = SwitchDetection.loadParms(args);
                SwitchDetection.DetectSwitch(dictionary);

                DeleteFile(e);
            }
            catch (Exception)
            {
                DeleteFile(e);
            }
        }

        private static void DeleteFile(FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
                File.Delete(e.FullPath);
        }
    }
}
