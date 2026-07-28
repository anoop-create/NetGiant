using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.Areas.PMS.Export;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Orders
{
    public class BackOrderController : Controller
    {
        public ActionResult BackOrder()
        {
            return View(new BackOrderViewModel());
        }

        public ActionResult BackOrderItem(int id)
        {
            var model = new BackOrderViewModel();
            model.BackOrderId = id;
            model.GetBackOrderEntry(id);
            return View(model);
        }

        public ActionResult BackOrder_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new BackOrderViewModel().GetBackOrder();

            var result = model.BackOrderList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult BackOrderItem_Read([DataSourceRequest] DataSourceRequest request, int id)
        {
            var model = new BackOrderViewModel();
            model.BackOrderId = id;
            model.GetBackOrderItem();

            var result = model.BackOrderItemList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        [DeleteFile]
        [ValidateAntiForgeryToken]
        public FileResult ExportBackOrder(string kendoData)
        {
            var model = new BackOrderViewModel();
            model.GetFullBackOrder();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateBackOrderCSVFile(model.BackOrderForExport);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        [HttpPost]
        public ActionResult SwitchStatus(int id)
        {
            var model = new BackOrderViewModel();
            SaveReturn sr = model.SwitchStatus(id);
            return Json(new { saveReturn = sr });
        }

        [HttpPost]
        public ActionResult SwitchLineStatus(int id)
        {
            var model = new BackOrderViewModel();
            SaveReturn sr = model.SwitchLineStatus(id);
            return Json(new { saveReturn = sr });
        }
    }
}