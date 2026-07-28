using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using DP001BusinessLogic.Shared;
using DP001BusinessLogic;
using System.Net;
using System.Configuration;
using System.Web.Routing;
using DP001DataAccess.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Kendo.Mvc;

namespace DP001Website.Controllers.Shared
{
    [Authorize]
    public class SharedController : ApplicationController
    {
        public JsonResult GetBrandNames()
        {
            int channelId = GetChannelId();
            var brandList = SharedViewModel.GetBrandList(channelId, true, true)
                .Select(x => x.Text);

            return Json(new { Items = brandList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSuppBrandNames()
        {
            int channelId = GetChannelId();
            var brandList = SharedViewModel.GetSuppBrandList(channelId, true, true)
                .Select(x => x.Text);

            return Json(new { Items = brandList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCalculationOutcomes()
        {
            int channelId = GetChannelId();
            var outcomeList = SharedViewModel.GetCalculationOutcomeList(channelId, true)
                .OrderBy(x => x.Text)
                .Select(x => x.Text);

            return Json(new { Items = outcomeList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPriceRuleNames()
        {
            int channelId = GetChannelId();
            var ruleNamesList = SharedViewModel.GetRuleNameList(channelId, true)
                .Select(x => x.Text);

            return Json(new { Items = ruleNamesList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRuleTypeList()
        {
            int channelId = GetChannelId();
            var lookupList = SharedViewModel.GetRuleTypeList(channelId, true)
                .Select(x => x.Text);

            return Json(new { Items = lookupList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetMethodList()
        {
            int channelId = GetChannelId();
            var lookupList = SharedViewModel.GetMethodList(channelId, true)
                .Select(x => x.Text);

            return Json(new { Items = lookupList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCategoryList()
        {
            int channelId = GetChannelId();
            var lookupList = SharedViewModel.GetCategoryList(channelId, 0, true)
                .Select(x => x.Text);

            return Json(new { Items = lookupList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCategories(string categoryId)
        {
            string html = "";

            int channelId = GetChannelId();
            var pcl = SharedViewModel.GetCategoryList(channelId);

            html += "<option value=\"\">Select ...</option>";
            foreach (SelectListItem sli in pcl)
            {
                html += "<option value=\"" + sli.Value + "\"" + (sli.Value == categoryId ? " selected" : "") + ">" + sli.Text + "</option>";
            }
            return Json(new { isSuccess = true, html = html, msg = "" }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetCompetitorNames()
        {
            int channelId = GetChannelId();
            var competitorList = SharedViewModel.GetCompetitorList(channelId, true)
                .Select(x => x.Text);

            return Json(new { Items = competitorList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSupplierNames()
        {
            int channelId = GetChannelId();
            var supplierList = SharedViewModel.GetSupplierList(channelId, true)
                .Select(x => x.Text);

            return Json(new { Items = supplierList }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLookupTypes(string id)
        {
            var lookupTypes = SharedViewModel.GetLookupList(id)
                .Select(x => x.Text);

            return Json(new { Items = lookupTypes }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult TriggerBatchProcess(string process, int channelId, string attributes, bool runImmediately = true)
        {
            var isSuccess = false;
            var inProgress = false;
            var tenant = GetTenant();
            var hasPermission = tenant.Channels.Any(x => x.ChannelID == channelId);
            if (hasPermission)
            {
                var platform = ConfigurationManager.AppSettings["Platform"];
                var uniqueGuid = Guid.NewGuid();

                isSuccess = BatchServerPlatform(process, runImmediately, attributes, uniqueGuid);
                //if (platform == "Server")
                //{
                //    isSuccess = BatchServerPlatform(process, runImmediately, attributes, uniqueGuid);
                //}
                //else if (platform == "Azure")
                //{
                //    isSuccess = BatchAzurePlatform(process, runImmediately, attributes, uniqueGuid);
                //}

                if (isSuccess && runImmediately)
                {
                    //Set the flag on the tenant
                    var ten = new Tenant();
                    inProgress = ten.SetJobInProgress(channelId, true);
                }
            }

            return Json(new { isSuccess = isSuccess, inProgress = inProgress }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAuditActions()
        {
            int channelId = GetChannelId();
            var auditActionList = SharedViewModel.GetAuditActionList()
                .Select(x => x.Text);

            return Json(new { Items = auditActionList }, JsonRequestBehavior.AllowGet);
        }
        
        private static bool BatchAzurePlatform(string process, bool runImmediately, string arguments, Guid uniqueGuid)
        {
            bool isSuccess = true; ;

            //if (runImmediately)
            //{
            //    isSuccess = AzureFunctions.WriteToAzureStorageQueue(arguments);
            //}
            //else
            //{
            //    var fileName = $"{process}_{uniqueGuid}.txt";
            //    var stream = CommonFunctions.GenerateStreamFromString(arguments);
            //    isSuccess = AzureFunctions.UploadToBlobStorage("adhocschedule", fileName, stream);
            //}

            return isSuccess;
        }

        private static bool BatchServerPlatform(string process, bool runImmediately, string arguments, Guid uniqueGuid)
        {
            bool isSuccess;
            string directoryPath;
            if (runImmediately)
            {
                directoryPath = ConfigurationManager.AppSettings["WatcherLocation"];
            }
            else
            {
                directoryPath = ConfigurationManager.AppSettings["AdHocBatchLocation"];
            }

            var filePath = string.Format("{0}{1}_{2}.txt", directoryPath, process, uniqueGuid);
            isSuccess = CommonFunctions.CreateTextFile(filePath, arguments);
            return isSuccess;
        }

        [HttpPost]
        public JsonResult CheckJobInProgress()
        {
            var ten = new Tenant();
            return Json(new { inProgress = ten.CheckJobInProgress(Int32.Parse(Session["CurrentChannel"].ToString())) }, JsonRequestBehavior.AllowGet);
        }
    }
}

