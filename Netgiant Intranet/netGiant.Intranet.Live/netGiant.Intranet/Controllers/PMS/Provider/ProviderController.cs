using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.ViewModels;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using System.Collections.Generic;
using System;
using System.Net.Mime;
using System.Configuration;

namespace netGiant.Intranet.Controllers.PMS.Provider
{
    [Authorize]
    public class ProviderController : ApplicationController
    {
        #region Provider

        public ActionResult ProviderIndex()
        {
            ProviderViewModel model = new ProviderViewModel();
            model.selectedProviderTypeID = 2;
            return View("~/Views/PMS/Provider/Provider/ProviderIndex.cshtml", 
                        model.GetProviders(1, "", "", "feedFileDateStampDesc"));
        }

        public ActionResult ProviderData(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[3]);
            model.GetProviders(Convert.ToInt32(optionsArray[4]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());
            return PartialView("~/Views/PMS/Provider/Provider/ProviderData.cshtml", model.providers);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProvider(int id)
        {
            return View("~/Views/PMS/Provider/Provider/CreateProvider.cshtml", ProviderViewModel.CreateProvider(id));
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

            return View("~/Views/PMS/Provider/Provider/CreateProvider.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProvider(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteProvider(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Provider Deleted";

            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[4]);

            return PartialView("~/Views/PMS/Provider/Provider/ProviderData.cshtml", model.GetProviders(Convert.ToInt32(optionsArray[5]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).providers);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult ProviderDetails(int id)
        {
            ProviderViewModel model = new ProviderViewModel();
            return View("~/Views/PMS/Provider/Provider/ProviderDetails.cshtml", model.GetProviderByID(id));
        }

        public void EnableDisableProvider(int id)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.EnableDisableProvider(id);
        }

        #endregion Provider
        
        #region Field Mappings

        public ActionResult FieldMapping()
        {
            ProviderViewModel model = new ProviderViewModel();
            return View("~/Views/PMS/Provider/FieldMapping/FieldMapping.cshtml", model.GetFieldMappings());
        }

        public ActionResult FieldMappingData(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.GetFieldMappings(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Provider/FieldMapping/FieldMappingData.cshtml", model.fieldMappings);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFieldMapping(int id, int? selectedProviderID)
        {
            return View("~/Views/PMS/Provider/FieldMapping/CreateFieldMapping.cshtml", 
                ProviderViewModel.CreateFieldMapping(id, selectedProviderID.HasValue ? selectedProviderID.Value : 0));
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
                    return RedirectToAction("ProviderDetails", new { id = model.selectedProviderID });
                }

                return RedirectToAction("FieldMapping");
            }

            return View("~/Views/PMS/Provider/FieldMapping/CreateFieldMapping.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFieldMapping(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteFieldMapping(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Field Mapping Deleted";

            return PartialView("~/Views/PMS/Provider/FieldMapping/FieldMappingData.cshtml", model.GetFieldMappings(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).fieldMappings);
        }

        #endregion Field Mappings

        #region FTP Details

        public ActionResult FtpDetailsIndex()
        {
            ProviderViewModel model = new ProviderViewModel();
            return View("~/Views/PMS/Provider/FtpDetails/FtpDetailsIndex.cshtml", model.GetFtpDetails());
        }

        public ActionResult FtpDetailsData(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.GetFtpDetails(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Provider/FtpDetails/FtpDetailsData.cshtml", model.listFTPDetails);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFtpDetails(int id, int? selectedProviderID)
        {
            return View("~/Views/PMS/Provider/FtpDetails/CreateFtpDetails.cshtml", 
                ProviderViewModel.CreateFtpDetails(id, selectedProviderID.HasValue ? selectedProviderID.Value : 0));
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

            return View("~/Views/PMS/Provider/FtpDetails/CreateFtpDetails.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFtpDetails(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteFtpDetails(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "FTP Details Deleted";

            return PartialView("~/Views/PMS/Provider/FtpDetails/FtpDetailsData.cshtml", model.GetFtpDetails(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).listFTPDetails);
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
                ProviderViewModel model = new ProviderViewModel();
                model.selectedProviderTypeID = 2;
                return View("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryIndex.cshtml", model.GetProviderInventory());
            }
            catch (Exception)
            {
                return View("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryUpdating.cshtml");
            }
        }

        public ActionResult ProviderInventoryData(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[5]);
            model.GetProviderInventory(Convert.ToInt32(optionsArray[11]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]), 
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);

            return PartialView("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryData.cshtml", model.providerInventories);
        }

        public ActionResult ProviderInventoryItemPrice(int id, int? page)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.GetProviderInventoryItemPrices(id, page);

            return PartialView("~/Views/PMS/Provider/ProviderInventory/PriceDetail.cshtml", model.providerItemPrices);
        }

        [HttpPost]
        public ActionResult SetProductInterest(List<string> optionsArray)
        {
            ProviderViewModel pVm = new ProviderViewModel();

            pVm.SetProductInterest(optionsArray[11], Convert.ToBoolean(optionsArray[12]));
            pVm.GetProviderInventory(1, optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]),
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);

            return PartialView("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryData.cshtml", pVm.providerInventories);
        }

        [HttpPost]
        public ActionResult ReinstateUnwantedProducts(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();

            model.ReinstateUnwantedProduct(optionsArray[11].ToString());
            TempData["InformationBoxFlag"] = "Product Reinstated";

            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[5]);
            model.GetProviderInventory(1, optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]),
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);

            return PartialView("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryData.cshtml", model.providerInventories);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SetUntrustedProvider(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();

            model.SetProviderUntrusted(Convert.ToInt32(optionsArray[12]), Convert.ToBoolean(optionsArray[11]));
            TempData["InformationBoxFlag"] = "Product Reinstated";

            model.selectedProviderTypeID = Convert.ToInt32(optionsArray[5]);
            model.GetProviderInventory(Convert.ToInt32(optionsArray[13]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]),
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[11]), optionsArray[10]);

            return PartialView("~/Views/PMS/Provider/ProviderInventory/ProviderInventoryData.cshtml", model.providerInventories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DeleteFileAttribute]
        public FileResult ExportProviderInventory(string options)
        {
            string[] optionsArray = options.Split(',');
            ProviderViewModel model = new ProviderViewModel();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            if (options.Length > 0)
            {
                model.ExportProviderInventory(model, optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[3]),
                Convert.ToBoolean(optionsArray[6]), Convert.ToInt32(optionsArray[2]), Convert.ToBoolean(optionsArray[7]),
                Convert.ToBoolean(optionsArray[8]), Convert.ToBoolean(optionsArray[9]), optionsArray[10]);
            }
            else
            {
                model.ExportProviderInventory(model);
            }

            return File(model.FilePath,
                            MediaTypeNames.Application.Octet,
                            "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        #endregion Provider Inventory

        #region Provider Type
        public ActionResult ProviderTypeIndex()
        {
            ProviderViewModel model = new ProviderViewModel();
            return View("~/Views/PMS/Provider/ProviderType/ProviderTypeIndex.cshtml", model.GetProviderTypes());
        }

        public ActionResult ProviderTypeData(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.GetProviderTypes(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Provider/ProviderType/ProviderTypeData.cshtml", model.ProviderTypes);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProviderType(int id)
        {
            return View("~/Views/PMS/Provider/ProviderType/CreateProviderType.cshtml", ProviderViewModel.CreateProviderType(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveProviderType(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveProviderType();
                TempData["InformationBoxFlag"] = "Provider Type Saved";
                return RedirectToAction("ProviderTypeIndex");
            }

            return View("~/Views/PMS/Provider/ProviderType/CreateProviderType.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProviderType(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteProviderType(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Provider Type Deleted";

            return PartialView("~/Views/PMS/Provider/ProviderType/ProviderTypeData.cshtml", model.GetProviderTypes(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).ProviderTypes);
        }
        #endregion Provider Type

        public ActionResult SkuMappings() 
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return View("~/Views/PMS/Provider/SkuMappings/SkuMappingsIndex.cshtml",
                        prVm.GetSkuMappings(null, null, null, null, null));
        }

        public ActionResult SkuMappingsData(List<string> optionsArray)
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return PartialView("~/Views/PMS/Provider/SkuMappings/SkuMappingsData.cshtml",
                        prVm.GetSkuMappings(Convert.ToInt32(optionsArray[4]), optionsArray[1], optionsArray[2], optionsArray[3],
                                            Convert.ToInt32(optionsArray[0])).skuMappingsList);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateSkuMapping(int id)
        {
            return View("~/Views/PMS/Provider/SkuMappings/CreateSkuMappings.cshtml", ProviderViewModel.CreateSkuMapping(id));
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

            return View("~/Views/PMS/Provider/SkuMappings/CreateSkuMappings.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteSkuMapping(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteSkuMapping(Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "Sku Mapping Deleted";

            return PartialView("~/Views/PMS/Provider/SkuMappings/SkuMappingsData.cshtml",
                                model.GetSkuMappings(Convert.ToInt32(optionsArray[5]),
                                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString(), null).skuMappingsList);
        }

        public ActionResult SupManuMappings()
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return View("~/Views/PMS/Provider/SupManuMappings/SupManuMappingsIndex.cshtml",
                        prVm.GetSupManuMappings(null, null, null, null, null, null));
        }

        public ActionResult SupManuMappingsData(List<string> optionsArray)
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return PartialView("~/Views/PMS/Provider/SupManuMappings/SupManuMappingsData.cshtml",
                        prVm.GetSupManuMappings(Convert.ToInt32(optionsArray[5]), optionsArray[1], optionsArray[2], optionsArray[4],
                                            Convert.ToInt32(optionsArray[0]), Convert.ToInt32(optionsArray[3])).supManuMappingsList);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateSupManuMapping(int id)
        {
            return View("~/Views/PMS/Provider/SupManuMappings/CreateSupManuMappings.cshtml", ProviderViewModel.CreateSupManuMapping(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveSupManuMapping(ProviderViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveSupManuMapping();
                TempData["InformationBoxFlag"] = "Supplier Manufacturer Mapping Saved";
                return RedirectToAction("SupManuMappings");
            }

            return View("~/Views/PMS/Provider/SupManuMappings/CreateSupManuMappings.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteSupManuMapping(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteSupManuMapping(Convert.ToInt32(optionsArray[5]));
            TempData["InformationBoxFlag"] = "Sup Manufacturer Mapping Deleted";

            return PartialView("~/Views/PMS/Provider/SupManuMappings/SupManuMappingsData.cshtml",
                                model.GetSupManuMappings(Convert.ToInt32(optionsArray[6]),
                                optionsArray[1].ToString(), optionsArray[2], optionsArray[4].ToString(),
                                Convert.ToInt32(optionsArray[0]), Convert.ToInt32(optionsArray[3])).supManuMappingsList);
        }


        public ActionResult MfpnExtensions()
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return View("~/Views/PMS/Provider/MfpnExtensions/MfpnExtensionsIndex.cshtml",
                        prVm.GetMfpnExtensions(null, null, null, null, null));
        }

        public ActionResult MfpnExtensionsData(List<string> optionsArray)
        {
            ProviderViewModel prVm = new ProviderViewModel();
            return PartialView("~/Views/PMS/Provider/MfpnExtensions/MfpnExtensionsData.cshtml",
                        prVm.GetMfpnExtensions(Convert.ToInt32(optionsArray[4]), optionsArray[1], optionsArray[2], optionsArray[3],
                                            Convert.ToInt32(optionsArray[0])).mfpnExtensionsList);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateMfpnExtension(int id)
        {
            return View("~/Views/PMS/Provider/MfpnExtensions/CreateMfpnExtension.cshtml", ProviderViewModel.CreateMfpnExtension(id));
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

            return View("~/Views/PMS/Provider/MfpnExtensions/CreateMfpnExtension.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMfpnExtension(List<string> optionsArray)
        {
            ProviderViewModel model = new ProviderViewModel();
            model.DeleteMfpnExtension(Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "Mfpn Extension Deleted";

            return PartialView("~/Views/PMS/Provider/MfpnExtensions/MfpnExtensionsData.cshtml",
                                model.GetMfpnExtensions(Convert.ToInt32(optionsArray[5]),
                                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString(), null).mfpnExtensionsList);
        }

        public ActionResult Exceptions()
        {
            ProviderExceptionsViewModel model = new ProviderExceptionsViewModel();
            return View("~/Views/PMS/Provider/Exceptions/ExceptionsIndex.cshtml", model.GetExceptions());
        }

        public ActionResult ExceptionsData(string[] optionsArray)
        {
            ProviderExceptionsViewModel model = new ProviderExceptionsViewModel();

            return PartialView("~/Views/PMS/Provider/Exceptions/ExceptionsData.cshtml", 
                model.GetExceptions(Convert.ToInt32(optionsArray[7]), optionsArray[0], 
                    Convert.ToBoolean(optionsArray[1]), optionsArray[2], Convert.ToBoolean(optionsArray[3]),
                    Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]), Convert.ToBoolean(optionsArray[6])));
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SetUntrustedProviderException(List<string> optionsArray)
        {
            ProviderViewModel pvVm = new ProviderViewModel();
            pvVm.SetProviderUntrusted(Convert.ToInt32(optionsArray[7]), Convert.ToBoolean(optionsArray[8]));

            ProviderExceptionsViewModel model = new ProviderExceptionsViewModel();
            return PartialView("~/Views/PMS/Provider/Exceptions/ExceptionsData.cshtml",
                model.GetExceptions(Convert.ToInt32(optionsArray[8]), optionsArray[0],
                    Convert.ToBoolean(optionsArray[1]), optionsArray[2], Convert.ToBoolean(optionsArray[3]),
                    Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]), Convert.ToBoolean(optionsArray[6])));
        }
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
                ProviderViewModel model = new ProviderViewModel();
                model.DeleteFile(filePath);
            }
        }
    }
}
