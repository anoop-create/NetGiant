using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin.BatchProgram
{
    public class ScheduledTasksViewModel : CommonViewModel
    {
        public ScheduledTasksViewModel()
        {
            TaskList = new List<TaskInfo>();
        }

        public List<TaskInfo> TaskList { get; set; }

        public ScheduledTasksViewModel GetTasks()
        {
            var configList = SharedFunctions.GetConfigurationSettingList("BatchProgramTask").OrderBy(x => x.description);

            foreach (var config in configList)
            {
                var ti = new TaskInfo()
                {
                    Name = config.settingName,
                    Arguments = config.settingValue,
                    Description = config.description
                };

                TaskList.Add(ti);
            }

            return this;
        }

        public bool RunTask(string taskName, string taskArgs)
        {
            var success = true;

            try
            {
                var taskWatcherFilePath = SharedFunctions.GetConfigurationSetting("BatchProgram", "TaskWatcherPath") + taskName + ".txt";
                if (!File.Exists(taskWatcherFilePath))
                {
                    using (StreamWriter sw = new StreamWriter(taskWatcherFilePath))
                    {
                        sw.WriteLine(taskArgs);
                    }
                }
            }
            catch (Exception)
            {
                success = false;   
            }

            return success;
        }
    }

    public class TaskInfo
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
        public string Description { get; set; }
    }
}
