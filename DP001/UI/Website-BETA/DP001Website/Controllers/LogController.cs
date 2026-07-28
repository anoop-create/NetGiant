using DP001DataAccess.Entities;
using DP001BusinessLogic.ViewModels;
using System.Web.Mvc;
using System.Collections.Generic;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace DP001Website.Controllers
{
    [Authorize]
    public class LogController : ApplicationController
    {
        // GET: Log
        public ActionResult Index()
        {
            int tenantId = GetTenant().TenantID;
            int channelId = GetChannel().ChannelID;
            LogViewModel model = new LogViewModel();
            model.LogList = model.GetNotifications(channelId, new List<string> { "Notification", "Suggestion", "ScheduleInfo" });

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin")]
        public ActionResult AdminIndex()
        {
            LogViewModel model = new LogViewModel();
            //model.GetErrors();

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin")]
        public ActionResult AdminIndex_Read([DataSourceRequest]DataSourceRequest request)
        {
            LogViewModel model = new LogViewModel();
            model.GetErrors();

            var result = model.ErrorList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        public JsonResult Summary()
        {
            int tenantId = GetTenant().TenantID;
            int channelId = GetChannel().ChannelID;
            LogViewModel model = new LogViewModel();
            model.GetSummary(channelId);

            string pv = RenderPartialViewToString("~/Views/Log/Summary.cshtml", model);
            return Json(new { isSuccess = true, html = pv }, JsonRequestBehavior.AllowGet);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public JsonResult RefreshNotifications(string log_not, string log_sug, string log_sch)
        {
            List<string> typeArray = new List<string>();

            if (log_not == "on")
            {
                typeArray.Add("Notification");
            }

            if (log_sug == "on")
            {
                typeArray.Add("Suggestion");
            }

            if (log_sch == "on")
            {
                typeArray.Add("ScheduleInfo");
            }


            int tenantId = GetTenant().TenantID;
            int channelId = GetChannel().ChannelID;
            LogViewModel model = new LogViewModel();
            model.LogList = model.GetNotifications(channelId, typeArray);

            string pv = RenderPartialViewToString("~/Views/Log/Notifications.cshtml", model);
            return Json(new { isSuccess = true, html = pv }, JsonRequestBehavior.AllowGet);
        }
    }
}