using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Web;

namespace netGiant.Intranet
{
    public class StartProcess
    {
        public static string StartScheduledTask(string taskName)
        {
            if (!string.IsNullOrEmpty(taskName))
            {
                try
                {
                    using (Process proc = new Process())
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        proc.StartInfo = startInfo;
                        startInfo.WorkingDirectory = @"C:\Windows\System32";
                        startInfo.FileName = "schtasks.exe";
                        startInfo.Arguments = string.Format("/Run /TN \"{0}\"", taskName);
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.RedirectStandardError = true;
                        startInfo.UseShellExecute = false;
                        proc.Start();
                        string errors = proc.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(errors))
                        {
                            return "Error: " + errors;
                        }
                        else
                        {
                            return "Started Task: " + taskName;
                        }

                    }
                }
                catch(Exception e)
                {
                    return "Error: " + e.Message.ToString();
                }
            }
            else
            {
                return "Please supply a Task Name";
            }
        }
    }
}

