using netGiant.Intranet.BusinessLayer.ViewModels.Admin.BatchProgram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Admin.BatchProgram
{
    public class BatchProgramController : Controller
    {
        public ActionResult Jobs()
        {
            ScheduledTasksViewModel model = new ScheduledTasksViewModel();
            return View("~/Views/Admin/BatchProgram/ListBatchJobs.cshtml", model.GetTasks());
        }

        public bool RunJob(string jobName, string jobArguments)
        {
            ScheduledTasksViewModel model = new ScheduledTasksViewModel();
            return model.RunTask(jobName, jobArguments);   
        }
    }
}