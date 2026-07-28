using DP001BusinessLogic.ViewModels;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic;
using DP001Website.Models;
using Microsoft.AspNet.Identity;

namespace DP001Website.Controllers
{
    [Authorize]
    [CheckUserPermission(FieldName = "SalesHistory", Check = TenantPermissonCheck.IsFeatureOn)]
    public class SalesHistoryController : ApplicationController
    {
        public ActionResult Index(int? id)
        {
            var model = new SalesHistoryViewModel(GetChannelId());
            var tenantId = GetTenant().TenantID;
            model.InitializeReport(id, User.Identity.GetUserId(), tenantId);

            return View(model);
        }

        public ActionResult Index_Read(
            [DataSourceRequest]DataSourceRequest request, 
            CrudSalesHistory.SummarizeBy? summarizeBy, 
            DateTime? dateFrom, 
            DateTime? dateTo,
            CrudSalesHistory.GroupBy groupBy)
        {
            var model = new SalesHistoryViewModel(GetChannelId())
            {
                SummarizeSalesHistoryBy = summarizeBy ?? CrudSalesHistory.SummarizeBy.Month,
                GroupSalesHistoryBy = groupBy,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            model.Get();

            var result = model.TelerikSalesHistory.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
    }
}
