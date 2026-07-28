using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import;
using System.Configuration;
using System.IO;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment;

namespace netGiant.Intranet.Controllers.PMS.Import
{
    [Authorize(Roles="IntranetAdmin, PMSAdmin")]
    public class ImportController : ApplicationController
    {
        [RestoreModelStateFromTempData]
        public ActionResult Product()
        {
            var state = ModelState;
            ImportProductViewModel model = new ImportProductViewModel();
            return View("~/Views/PMS/Import/ImportProduct.cshtml", model);
        }

        [RestoreModelStateFromTempData]
        public ActionResult PriceRule()
        {
            ImportPriceRuleViewModel model = new ImportPriceRuleViewModel();
            return View("~/Views/PMS/Import/ImportPriceRule.cshtml", model);
        }

        [RestoreModelStateFromTempData]
        public ActionResult Equipment()
        {
            ImportEquipmentViewModel model = new ImportEquipmentViewModel();
            return View("~/Views/PMS/Import/ImportEquipment.cshtml", model);
        }

        [HttpPost]
        [SetTempDataModelState]
        public ActionResult ImportPriceRule(HttpPostedFileBase uploadedFile, int websiteFK)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            string localDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();
            string filePath = localDirectory + "\\PMSTempData\\" +
                                    DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss_") +
                                    uploadedFile.FileName;

            uploadedFile.SaveAs(filePath);
            ImportPriceRuleViewModel model = new ImportPriceRuleViewModel();

            if (uploadedFile != null)
            {
                model.WebsiteFK = websiteFK;
                model.Import(filePath);
            }

            SharedFunctions.DeleteFile(filePath);
            return RedirectToAction("PriceRule");
        }

        [HttpPost]
        [SetTempDataModelState]
        public ActionResult ImportProduct(HttpPostedFileBase uploadedFile, int websiteFK)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            string localDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            if (uploadedFile != null)
            {
                string extension = Path.GetExtension(uploadedFile.FileName);

                if (extension.ToLower() != ".csv")
                {
                    ModelState.AddModelError("", "Invalid File Type, CSV Files Only");
                }
                else
                {
                    DoImport(uploadedFile, websiteFK, localDirectory);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please Select a File");
            }

            return RedirectToAction("Product");
        }

        [HttpPost]
        [SetTempDataModelState]
        public ActionResult ImportEquipment(HttpPostedFileBase uploadedFile)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            string localDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            if (uploadedFile != null)
            {
                string extension = Path.GetExtension(uploadedFile.FileName);

                if (extension.ToLower() != ".csv")
                {
                    ModelState.AddModelError("", "Invalid File Type, CSV Files Only");
                }
                else
                {
                    DoEquipmentImport(uploadedFile, localDirectory);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please Select a File");
            }

            return RedirectToAction("Equipment");
        }

        private void DoImport(HttpPostedFileBase uploadedFile, int websiteFK, string localDirectory)
        {
            string filePath = string.Empty;
            ImportProductViewModel model = new ImportProductViewModel();

            try
            {
                filePath = localDirectory + "\\PMSTempData\\" +
                                    DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss_") +
                                    uploadedFile.FileName;

                uploadedFile.SaveAs(filePath);
                model.FilePath = filePath;
                model.WebsiteFK = websiteFK;

                try
                {
                    model.Import();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }

                foreach (string warning in model.Warnings)
                {
                    ModelState.AddModelError("", warning);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            finally
            {
                if (filePath != string.Empty)
                    model.DeleteFile(filePath);
            }
        }

        private void DoEquipmentImport(HttpPostedFileBase uploadedFile, string localDirectory)
        {
            string filePath = string.Empty;
            ImportEquipmentViewModel model = new ImportEquipmentViewModel();

            try
            {
                filePath = localDirectory + "\\PMSTempData\\" +
                                    DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss_") +
                                    uploadedFile.FileName;

                uploadedFile.SaveAs(filePath);
                model.FilePath = filePath;

                try
                {
                    model.Import(filePath);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }

                foreach (string warning in model.Warnings)
                {
                    ModelState.AddModelError("", warning);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            finally
            {
                if (filePath != string.Empty)
                    SharedFunctions.DeleteFile(filePath);
            }
        }
    }

    public class SetTempDataModelStateAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            filterContext.Controller.TempData["ModelState"] =
               filterContext.Controller.ViewData.ModelState;

            if (filterContext.Controller.ViewData.ModelState.Values.Where(v => v.Errors.Count != 0).Count() == 0)
                filterContext.Controller.TempData["ModelStateValid"] = true;
        }
    }

    public class RestoreModelStateFromTempDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (filterContext.Controller.TempData.ContainsKey("ModelState"))
            {
                filterContext.Controller.ViewData.ModelState.Merge(
                    (ModelStateDictionary)filterContext.Controller.TempData["ModelState"]);
            }
        }
    }
}