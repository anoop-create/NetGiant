using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Areas.PMS.Product
{
    [Authorize]
    public class CrossSellingLinkController : Controller
    {
        public ActionResult Links()
        {
            return View("CrossSellingLink", new CrossSellingLinkViewModel());
        }

        public ActionResult Links_Read([DataSourceRequest]DataSourceRequest request)
        {
            Session["CrossSellingLink_Read_DataSourceRequest"] = request;
            var model = new CrossSellingLinkViewModel().Get();

            var result = model.CrossSellingLinkList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("CreateCrossSellingLink", CrossSellingLinkViewModel.Create(id));
        }

        public JsonResult GetProducts(string searchTerm)
        {
            return Json(SelectListViewModel.GetProductsArray(searchTerm).ToList(), JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CrossSellingLinkViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
            }

            return RedirectToAction("Links");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new CrossSellingLinkViewModel().Delete(id) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportCrossSellingLinks(string kendoData)
        {
            DataSourceRequest dataSourceRequest = Session["CrossSellingLink_Read_DataSourceRequest"] as DataSourceRequest;
            dataSourceRequest.PageSize = int.MaxValue;

            var model = new CrossSellingLinkViewModel();
            model.Get();

            DataSourceResult result = model.CrossSellingLinkList.ToDataSourceResult(dataSourceRequest);

            List<TelerikCrossSellingLink> telerikCrossSellingLink = new List<TelerikCrossSellingLink>();
            foreach (TelerikCrossSellingLink row in result.Data)
            {
                telerikCrossSellingLink.Add(row);
            }

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateCrossSellingLinkCSVFile(telerikCrossSellingLink);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }
    }
}