using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class TenantAuditController : ApplicationController
    {
        private TenantAuditViewModel model;

        // GET: TenantAudit
        public ActionResult Index()
        {
            return View(model);
        }

        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            int channelId = GetChannelId();
            model = new TenantAuditViewModel(channelId);
            model.GetEntries();

            var result = model.TenantAuditList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult DisplayEntry(int id)
        {
            int channelId = GetChannelId();
            var model = new TenantAuditViewModel(channelId);
            model.DisplayEntry(id);

            if (model.TenantAuditEntry == null)
                return RedirectToAction("Index");
            else
            {
                return View(model);
            }
        }

    }
}