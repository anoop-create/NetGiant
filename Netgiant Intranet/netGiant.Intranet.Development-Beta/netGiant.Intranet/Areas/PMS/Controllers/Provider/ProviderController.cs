using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider;
using System.Collections.Generic;
using System;
using System.Net.Mime;
using System.Configuration;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Newtonsoft.Json;
using System.Web;

namespace netGiant.Intranet.Areas.PMS.Provider
{
    [Authorize]
    public class ProviderController : Controller
    {
        #region Provider

        public ActionResult ProviderIndex()
        {
            var model = new ProviderViewModel();
            return View(model.GetProviders());
        }

        public ActionResult ProviderDataAjax([DataSourceRequest] DataSourceRequest request)
        {
            var model = new ProviderViewModel();
            model.GetProviders();

            var result = model.ProviderList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProvider(int id)
        {
            return View(ProviderViewModel.CreateProvider(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveProvider(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveProvider();
                TempData["InformationBoxFlag"] = "Provider Saved";
                return RedirectToAction("ProviderIndex");
            }

            return View("CreateProvider", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProvider(List<string> optionsArray)
        {
            var model = new ProviderViewModel();
            model.DeleteProvider(Convert.ToInt32(optionsArray[0]));

            return RedirectToAction("ProviderIndex", "Provider");
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult ProviderDetails(int id)
        {
            var model = new ProviderViewModel();
            return View(model.GetProviderByID(id));
        }

        public void EnableDisableProvider(int id)
        {
            var model = new ProviderViewModel();
            model.EnableDisableProvider(id);
        }

        #endregion Provider
        
        #region Field Mappings

        public ActionResult FieldMapping()
        {
            return View(new ProviderViewModel());
        }

        public ActionResult FieldMapping_Read([DataSourceRequest]DataSourceRequest request, int id = 0)
        {
            var model = new ProviderViewModel().GetFieldMappings(id);

            var result = model.FieldMappingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFieldMapping(int id, int? selectedProviderId, int? selectedFtpDetailId)
        {
            return View(ProviderViewModel.CreateFieldMapping(id, selectedProviderId.HasValue ? selectedProviderId.Value : 0, selectedFtpDetailId.HasValue ? selectedFtpDetailId.Value : 0));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFieldMapping(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveFieldMapping();
                TempData["InformationBoxFlag"] = "Field Mapping Saved";

                if (model.selectedProviderID > 0)
                {
                    return Redirect(Url.Action("ProviderDetails", "Provider") + "/" + model.selectedProviderID + "#mappings");
                }

                return RedirectToAction("FieldMapping");
            }

            return View("CreateFtpDetails", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFieldMapping(int id)
        {
            return Json(new { saveReturn = new ProviderViewModel().DeleteFieldMapping(id) });
        }

        #endregion Field Mappings

        #region FTP Details

        public ActionResult FtpDetailsIndex()
        {
            return View(new ProviderViewModel());
        }

        public ActionResult FtpDetails_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProviderViewModel().GetFtpDetails();

            var result = model.FtpDetailsList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFtpDetails(int id, int? selectedProviderID)
        {
            return View(ProviderViewModel.CreateFtpDetails(id, selectedProviderID.HasValue ? selectedProviderID.Value : 0));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFtpDetails(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveFtpDetails();
                TempData["InformationBoxFlag"] = "FTP Details Saved";

                if (model.selectedProviderID > 0)
                {
                    return RedirectToAction("ProviderDetails", new { id = model.selectedProviderID });
                }

                return RedirectToAction("FtpDetailsIndex");
            }

            return View("CreateFtpDetails", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFtpDetails(int id)
        {
            return Json(new { saveReturn = new ProviderViewModel().DeleteFtpDetails(id) });
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult FtpConnection(int id)
        {
            bool connected = ProviderViewModel.TestFTPConnection(id);
            TempData["InformationBoxFlag"] = connected ? "FTP Connection Succeed" : "FTP Connection Failed";

            return RedirectToAction("FtpDetailsIndex");
        }

        #endregion FTP Details

        #region Provider Inventory

        public ActionResult ProviderInventoryIndex()
        {
            try
            {
                return View(new ProviderViewModel());
            }
            catch
            {
                return View("   ");
            }
        }

        public ActionResult ProviderInventory_Read([DataSourceRequest]DataSourceRequest request)
        {
            Session["ProviderInventory_Read_DataSourceRequest"] = request;

            var model = new ProviderViewModel();
            model.GetInventory();

            var result = model.providerInventory.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult ProviderInventoryData(List<string> optionsArray)
        {
            var model = new ProviderViewModel();
            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[5]);
            model.GetProviderInventory(Convert.ToInt32(optionsArray[11]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]), 
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);

            return PartialView("_ProviderInventoryData", model.providerInventories);
        }

        public ActionResult ProviderInventoryItemPrice(int id, int? page)
        {
            var model = new ProviderViewModel();
            model.GetProviderInventoryItemPrices(id, page);

            return PartialView("_PriceDetail", model.providerItemPrices);
        }


        [HttpPost]
        public ActionResult SetProductInterest(List<string> options)
        {
            var model = new ProviderViewModel();

            model.SetProductInterest(options[0], Convert.ToBoolean(options[1]));

            return PartialView("ProviderInventoryIndex", model.GetInventory());
        }

        [HttpPost]
        public ActionResult ReinstateUnwantedProducts(List<string> optionsArray)
        {
            var model = new ProviderViewModel();

            model.ReinstateUnwantedProduct(optionsArray[11].ToString());
            TempData["InformationBoxFlag"] = "Product Reinstated";

            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[5]);
            model.GetProviderInventory(1, optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]),
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);

            return PartialView("_ProviderInventoryData", model.providerInventories);
        }


        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SetUntrustedProvider(List<string> options)
        {
            var model = new ProviderViewModel();

            model.SetProviderUntrusted(Convert.ToInt32(options[0]), Convert.ToBoolean(options[1]));

            return PartialView("ProviderInventoryIndex", model.GetInventory());
        }

        [HttpPost]
        [DeleteFile]
        [ValidateAntiForgeryToken]
        public FileResult ExportProviderInventory()
        {
            DataSourceRequest dataSourceRequest = Session["ProviderInventory_Read_DataSourceRequest"] as DataSourceRequest;
            dataSourceRequest.PageSize = int.MaxValue;  

            var model = new ProviderViewModel();
            model.GetInventory();
            DataSourceResult result = model.providerInventory.ToDataSourceResult(dataSourceRequest);

            List<TelerikProviderInventory> telerikProviderInventories = new List<TelerikProviderInventory>();

            foreach(TelerikProviderInventory row in result.Data)
            {
                telerikProviderInventories.Add(row);
            }

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateCSVFile(telerikProviderInventories);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }


        #endregion Provider Inventory

        #region SKU Mappings

        public ActionResult SkuMappings() 
        {
            return View("SkuMappingsIndex", new ProviderViewModel());
        }

        public ActionResult SkuMappings_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProviderViewModel().GetSkuMappings();

            var result = model.SkuMappingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateSkuMapping(int id)
        {
            return View("CreateSkuMappings", ProviderViewModel.CreateSkuMapping(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveSkuMapping(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveSkuMapping();
                TempData["InformationBoxFlag"] = "Sku Map Saved";
                return RedirectToAction("SkuMappings");
            }

            return View("CreateSkuMappings", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteSkuMapping(int id)
        {
            return Json(new { saveReturn = new ProviderViewModel().DeleteSkuMapping(id) });
        }

        #endregion

        #region Supplier Manufacturer Mappings

        public ActionResult SupManuMappings()
        {
            return View("SupManuMappingsIndex", new ProviderViewModel());
        }

        public ActionResult SupplierManufacturerMapping_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProviderViewModel().GetSupplierManufacturerMappings();

            var result = model.SupplierManufacturerMappingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateSupManuMapping(int id)
        {
            return View("CreateSupManuMappings", ProviderViewModel.CreateSupplierManufacturerMapping(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveSupManuMapping(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveSupplierManufacturerMapping();
                TempData["InformationBoxFlag"] = "Supplier Manufacturer Mapping Saved";
                return RedirectToAction("SupManuMappings");
            }

            return View("CreateSupManuMappings", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteSupManuMapping(int id)
        {
            return Json(new { saveReturn = new ProviderViewModel().DeleteSupplierManufacturerMapping(id) });
        }

        #endregion

        #region Mfpn Extensions

        public ActionResult MfpnExtensions()
        {
            return View("MfpnExtensionsIndex", new ProviderViewModel());
        }

        public ActionResult MfpnExtensions_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProviderViewModel().GetMfpnExtensions();

            var result = model.MfpnExtensionsList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateMfpnExtension(int id)
        {
            return View(ProviderViewModel.CreateMfpnExtension(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveMfpnExtension(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveMfpnExtension();
                TempData["InformationBoxFlag"] = "Mfpn Extension Saved";
                return RedirectToAction("MfpnExtensions");
            }

            return View("CreateMfpnExtension", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMfpnExtension(int id)
        {
            return Json(new { saveReturn = new ProviderViewModel().DeleteMfpnExtension(id) });
        }

        #endregion
    }

    public class DeleteFileAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            filterContext.HttpContext.Response.Flush();
            Type fcType = filterContext.Result.GetType();

            if (fcType.Name == "FilePathResult")
            {
                string filePath = (filterContext.Result as FilePathResult).FileName;
                var model = new ProviderViewModel();
                model.DeleteFile(filePath);
            }
        }
    }
}
