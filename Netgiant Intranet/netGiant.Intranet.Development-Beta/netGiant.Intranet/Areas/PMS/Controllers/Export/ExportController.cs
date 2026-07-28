using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export;
using System;
using System.Configuration;
using System.Net.Mime;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Export
{
    [Authorize]
    public class ExportController : Controller
    {
        public ActionResult ProductImages()
        {
            var model = new ExportProductImagesViewModel();
            model.SelectedWebsiteFK = 1;
            model.GetExportableFields();
            model.GetProductCount();
            return View("ExportProductImages", model);
        }

        public ActionResult Product()
        {
            var model = new ExportProductViewModel();
            model.SelectedWebsiteFK = 1;
            model.GetExportableFields();
            model.GetProductCount();
            return View("ExportProduct", model);
        }

        public ActionResult UpdateProductCheckboxes(int websiteFK)
        {
            var model = new ExportProductViewModel();
            model.SelectedWebsiteFK = websiteFK;
            model.GetExportableFields();
            return PartialView("_ExportCheckboxes", model);
        }

        public int UpdateProductImageCount(string[] optionsArray)
        {
            var model = new ExportProductImagesViewModel();
            model.SelectedWebsiteFK = Convert.ToInt32(optionsArray[0]);
            model.SelectedCategoryCodeFK = Convert.ToInt32(optionsArray[1]);
            model.SelectedProductStatusFK = Convert.ToInt32(optionsArray[2]);
            model.SelectedProductGroupFK = Convert.ToInt32(optionsArray[3]);
            model.SelectedSalesAreaGroupFK = Convert.ToInt32(optionsArray[4]);
            model.SelectedDataSupplierFK = Convert.ToInt32(optionsArray[5]);
            model.SelectedManufacturerFK = Convert.ToInt32(optionsArray[6]);
            model.GetProductCount();

            return model.GetProductCount().ProductCount;
        }

        public int UpdateProductCount(string[] optionsArray)
        {
            var model = new ExportProductViewModel();
            model.SelectedWebsiteFK = Convert.ToInt32(optionsArray[0]);
            model.SelectedCategoryCodeFK = Convert.ToInt32(optionsArray[1]);
            model.SelectedProductStatusFK = Convert.ToInt32(optionsArray[2]);
            model.SelectedProductGroupFK = Convert.ToInt32(optionsArray[3]);
            model.SelectedSalesAreaGroupFK = Convert.ToInt32(optionsArray[4]);
            model.SelectedDataSupplierFK = Convert.ToInt32(optionsArray[5]);
            model.SelectedManufacturerFK = Convert.ToInt32(optionsArray[6]);
            model.SearchBy = optionsArray[7];
            model.SearchTerm = optionsArray[8];
            model.GetProductCount();

            return model.GetProductCount().ProductCount;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DeleteFileAttribute]
        public FileResult ExportProductImages(ExportProductImagesViewModel model)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.Export();

            return File(model.FilePath,
                            MediaTypeNames.Application.Octet,
                            "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DeleteFileAttribute]
        public FileResult ExportProduct(ExportProductViewModel model)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.Export();

            return File(model.FilePath, 
                            MediaTypeNames.Application.Octet, 
                            "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        public ActionResult Equipment()
        {
            ExportEquipmentViewModel model = new ExportEquipmentViewModel();
            model.GetResultsCount();
            return View("ExportEquipment", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DeleteFileAttribute]
        public FileResult ExportEquipment(ExportEquipmentViewModel model)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.Export();

            return File(model.FilePath,
                            MediaTypeNames.Application.Octet,
                            "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        public int UpdateEquipmentCount(string[] optionsArray)
        {
            var model = new ExportEquipmentViewModel();
            model.SelectedManufacturerID = Convert.ToInt32(optionsArray[0]);
            model.SelectedFamilyID = Convert.ToInt32(optionsArray[1]);
            model.SelectedExportType = optionsArray[2];

            return model.GetResultsCount().EquipmentCount;
        }

        public ActionResult PromotionalGroup()
        {
            var model = new ExportPromotionalGroupViewModel();
            model.GetResultsCount();
            return View("ExportPromotionalGroup", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [DeleteFile]
        public FileResult ExportPromotionalGroup(ExportPromotionalGroupViewModel model)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.Export();

            return File(model.FilePath,
                MediaTypeNames.Application.Octet,
                "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
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
                ExportProductViewModel model = new ExportProductViewModel();
                model.DeleteFile(filePath);
            }
        }
    }
}
