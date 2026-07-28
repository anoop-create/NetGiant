using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.QA;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using static netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.OpenRangeImagesViewModel;

//TIDYUP
namespace netGiant.Intranet.Controllers.Ecommerce
{
    [Authorize(Roles = "IntranetAdmin")]
    public class EcommerceController : ApplicationController
    {
        public ActionResult Log()
        {
            var model = new LogViewModel();
            return View(model);
        }

        public ActionResult Log_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new LogViewModel();
            model.GetLogs();

            var result = model.LogList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetLogEntry(long id)
        {
            var model = new LogViewModel();
            model.GetLogEntry(id);

            string[] entry = model.LogEntry.Entry.Split(new string[] { "<b>FURTHER INFO:</b>" }, StringSplitOptions.None);
            string fi = "";
            if (entry.Length > 1)
            {
                fi = entry[1];
            }

            return Json(new {
                entry = entry[0]
                .Replace(Environment.NewLine, "<br />")
                .Replace("<script>", "<text>")
                .Replace("<script type='text/javascript'>", "<text>")
                .Replace("</script>", "</text>"),
                furtherinfo = fi.Replace(Environment.NewLine, "<br />"),
                queryString = model.LogEntry.QueryString.Replace("&", "<br />"),
                formData = model.LogEntry.FormData.Replace("&", "<br />"),
                developerComments =  model.LogEntry.DeveloperComments == null ? "" : model.LogEntry.DeveloperComments,
                description = model.LogEntry.Description,
                dateTime = model.LogEntry.DateTime,
                url = model.LogEntry.Url
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveLogEntry(LogViewModel model)
        {
            LogViewModel m = new LogViewModel();
            m.GetLogEntry(model.LogEntry.Id);
            m.LogEntry.DeveloperComments = model.LogEntry.DeveloperComments;
            var sr = m.SaveLogEntry();

            return Json(new
            {
                saveReturn = sr
            });
        }

        public ActionResult SetDeletedFlag(int id)
        {
            var model = new LogViewModel();
            var saveReturn = model.SetDeletedFlag(id, true);

            return Json(saveReturn, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeliveryZones()
        {
            var model = new DeliveryConfigurationViewModel();
            return View(model);
        }

        public ActionResult DeliveryZones_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliveryZones();

            var result = model.DeliveryZones.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult EditDeliveryZone(int id)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliveryZone(id);

            return View(model);
        }

        public ActionResult DeliveryServices()
        {
            return View(new DeliveryConfigurationViewModel());
        }

        public ActionResult DeliveryServices_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliveryServices();

            var result = model.DeliveryServices.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult DeliverySupplierCode()
        {
            return View(new DeliveryConfigurationViewModel());
        }

        public ActionResult DeliverySupplierCode_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliverySupplierCodes();

            var result = model.DeliverySupplierCodes.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpGet]
        public ActionResult NewEditDeliverySupplierCode(int id)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliverySupplierCode(id);
            return View(model);
        }

        [HttpPost]
        public ActionResult NewEditDeliverySupplierCode(DeliveryConfigurationViewModel model)
        {
            //check if this whole deliverySupplierCode exists
            if(model.CheckDeliverySupplierCodeExists(model) == true)
            {
                //there is already a record for this service and supplier
                ModelState.AddModelError("AlreadyExists", "There is already an entry for this Service and Provider");
            }

            if (ModelState.IsValid)
            {
                model.NewSaveDeliverySupplierCode(model);
                return RedirectToAction("DeliverySupplierCode");
            }
            else
            {
                model.GetDeliverySupplierCode(model.DeliverySupplierCode.deliverySupplierCodeId);
                return View(model);
            }
        }

        public ActionResult DeleteDeliverySupplierCode(int id)
        {
            try
            {
                DeliveryConfigurationViewModel.DeleteDeliverySupplierCode(id);

                return Json(new { IsSuccess = true });
            }
            catch (Exception ex)
            {
                return Json(new { IsSuccess = false, Message = ex.Message });
            }
        }

        public ActionResult EditDeliveryService(int id)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliveryService(id);

            return View(model);
        }

        [HttpPost]
        public ActionResult SaveDeliveryZone(DeliveryConfigurationViewModel model)
        {
            model.SaveDeliveryZone();

            return RedirectToAction("DeliveryZones");
        }

        public ActionResult NewDeliveryLookup(int deliveryZoneId, int deliveryServiceId, int sequence)
        {
            var model = new DeliveryConfigurationViewModel();
            var newLookup = model.CreateNewDeliveryLookup(deliveryZoneId, deliveryServiceId, sequence);

            return PartialView("_DeliveryLookup", newLookup);
        }

        public ActionResult NewDeliveryZone()
        {
            var model = new DeliveryConfigurationViewModel();
            model.NewDeliveryZone();

            return View(model);
        }

        [HttpPost]
        public ActionResult GetDeliveryServices(int websiteId)
        {
            var model = new DeliveryConfigurationViewModel();
            model.GetDeliveryServicesForWebsite(websiteId);

            return Json(model.Services, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CreateDeliveryZone(DeliveryConfigurationViewModel model)
        {
            model.CreateDeliveryZone();

            return RedirectToAction("DeliveryZones");
        }

        [HttpPost]
        public ActionResult SaveDeliveryService(DeliveryConfigurationViewModel model)
        {
            model.SaveDeliveryService();

            return RedirectToAction("DeliveryServices");
        }

        [HttpPost]
        public ActionResult CreateDeliveryService(DeliveryConfigurationViewModel model)
        {
            model.CreateDeliveryService();

            return RedirectToAction("DeliveryServices");
        }

        public ActionResult NewDeliveryService()
        {
            var model = new DeliveryConfigurationViewModel();
            model.NewDeliveryService();

            return View(model);
        }

        [HttpPost]
        public ActionResult DeleteDeliveryService(int id)
        {
            try
            {
                DeliveryConfigurationViewModel.DeleteDeliveryService(id);

                return Json(new { IsSuccess = true });
            }
            catch (Exception ex)
            {
                return Json(new { IsSuccess = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult DeleteDeliveryZone(int id)
        {
            try
            {
                DeliveryConfigurationViewModel.DeleteDeliveryZone(id);

                return Json(new { IsSuccess = true });
            }
            catch (Exception ex)
            {
                return Json(new { IsSuccess = false, Message = ex.Message });
            }
        }

        public ActionResult StuckOrdersIndex()
        {
            var model = new StuckOrdersViewModel();

            model.PwaSettings.IsPwa = true;
            model.PwaSettings.Description = "NetGiant Orders Waiting";

            return View(model);
        }

        public ActionResult StuckOrders_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new StuckOrdersViewModel();
            model.GetStuckOrders();

            var result = model.StuckOrdersList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        public ActionResult StuckOrderResolved(int id, string dbName)
        {
            var model = new StuckOrdersViewModel();
            var saveReturn = model.StuckOrderResolved(id, dbName);

            return Json(saveReturn, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult StuckOrderUpdRecord(StuckOrdersViewModel model)
        {
            var saveReturn = model.StuckOrderUpdRecord();

            return Json(saveReturn, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InterimOrdersIndex()
        {
            var model = new InterimOrdersViewModel();
            return View(model);
        }

        public ActionResult InterimOrders_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new InterimOrdersViewModel();
            model.GetInterimOrders();

            var result = model.InterimOrdersList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult InterimOrderCreateEntry(int id)
        {
            return View(new InterimOrdersViewModel().EditEntry(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult InterimOrderSaveEntry(InterimOrdersViewModel model)
        {
            try
            {
                if (model.SaveEntry())
                {
                    TempData["InformationBoxFlag"] = "Interim Order Saved";
                }
                return RedirectToAction("InterimOrdersIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("InterimOrderCreateEntry", model);
            }
        }

        public ActionResult GetInterimOrder(int id)
        {
            var model = new InterimOrdersViewModel();
            model.GetInterimOrder(id);

            return View("InterimOrderDetail", model);
        }

        public ActionResult OrderTrackingIndex()
        {
            var model = new OrderTrackingViewModel();
            return View(model);
        }

        public ActionResult OrderTracking_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new OrderTrackingViewModel();
            model.GetOrderTracking();

            var result = model.OrderTrackingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult AnalyseIisLogs()
        {
            var model = new IisLogViewModel();
            model.GetLogAnalysis(1, DateTime.Today);

            return View(model);

        }

        public ActionResult FraudCriteria()
        {
            return View(new FraudCriteriaViewModel());
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFraudCriteria(int id)
        {
            return View(new FraudCriteriaViewModel().CreateFraudCriteria(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFraudCriteria(FraudCriteriaViewModel model, string content)
        {
            try
            {
                if (model.SaveFraudCriteria())
                {
                    TempData["InformationBoxFlag"] = "Fraud Criteria Saved";
                }
                return RedirectToAction("FraudCriteria");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateFraudCriteria", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFraudCriteria(int id)
        {
            FraudCriteriaViewModel model = new FraudCriteriaViewModel();

            SaveReturn sr = model.DeleteFraudCriteria(id);

            return Json(new { saveReturn = sr });
        }

        public ActionResult FraudCriteria_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new FraudCriteriaViewModel();
            model.GetFraudCriteria();

            var result = model.FraudCriteriaList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult OpenRangeImagesIndex(int id = 1)
        {
            var model = new OpenRangeImagesViewModel(id.ToString());
            return View(model);
        }

        public ActionResult OpenRangeImages_Read([DataSourceRequest]DataSourceRequest request, string site)
        {
            var model = new OpenRangeImagesViewModel(site);
            model.GetOpenRangeImages();

            var result = model.OpenRangeImagesList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult IceCatImagesIndex(int id = 1)
        {
            var model = new IceCatImagesViewModel(id.ToString());
            return View(model);
        }

        public ActionResult IceCatImages_Read([DataSourceRequest] DataSourceRequest request, string site)
        {
            var model = new IceCatImagesViewModel(site);
            model.GetIceCatImages();

            var result = model.IceCatImagesList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult CampaignTrackingIndex()
        {
            var model = new CampaignTrackingViewModel();
            return View(model);
        }

        public ActionResult CampaignTracking_Read([DataSourceRequest] DataSourceRequest request)
        {
            Session["CampaignTracking_Read_DataSourceRequest"] = request;

            var model = new CampaignTrackingViewModel();
            model.GetCampaignTracking();

            var result = model.CampaignTrackingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        //public ActionResult WAFRulesIndex()
        //{
        //    var model = new WAFRulesViewModel();
        //    return View(model);
        //}

        //public ActionResult WAFRules_Read([DataSourceRequest]DataSourceRequest request, string site)
        //{
        //    var model = new WAFRulesViewModel();
        //    model.GetWAFRules(site);

        //    var result = model.WAFRulesList.ToDataSourceResult(request);
        //    var jsonResult = Json(result);
        //    jsonResult.MaxJsonLength = int.MaxValue;
        //    return jsonResult;
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportImageSpec(string kendoData, int id = 1)
        {
            var data = JsonConvert.DeserializeObject<IList<Telerik>>(HttpUtility.UrlDecode(kendoData));
            var model = new OpenRangeImagesViewModel(id.ToString());

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateImageSpecCSVFile();

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportCampaignTracking(string kendoData)
        {
            DataSourceRequest dataSourceRequest = Session["CampaignTracking_Read_DataSourceRequest"] as DataSourceRequest;
            dataSourceRequest.PageSize = int.MaxValue;

            //var data = JsonConvert.DeserializeObject<IList<Telerik>>(HttpUtility.UrlDecode(kendoData));
            var model = new CampaignTrackingViewModel();
            model.GetCampaignTracking();

            DataSourceResult result = model.CampaignTrackingList.ToDataSourceResult(dataSourceRequest);

            List<TelerikCampaignTracking> telerikCampaignTracking = new List<TelerikCampaignTracking>();
            foreach (TelerikCampaignTracking row in result.Data)
            {
                telerikCampaignTracking.Add(row);
            }

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateCampaignTrackingCSVFile(telerikCampaignTracking);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        #region FAQ
        public ActionResult FaqIndex()
        {
            Session["ReturnAction"] = "FaqIndex";
            Session["ReturnController"] = "Ecommerce";
            return View(new FaqViewModel());
        }

        public ActionResult Faq_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new FaqViewModel().GetFaqs();

            var result = model.FaqList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateFaq(int id, bool isPopup = true)
        {
            ViewBag.IsPopup = isPopup;
            FaqViewModel model = new FaqViewModel();
            model.Layout = SharedFunctions.ModifyForPopup(isPopup);
            model.CreateFaq(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveFaq(FaqViewModel model, string question, string answer)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                model.Faq.Question = question;
                model.Faq.Answer = answer;
                sr = model.SaveFaq();
                if (sr.IsSuccess)
                {
                    TempData["InformationBoxFlag"] = "FAQ Saved";
                    //sr.Message = "FAQ Saved.";
                }
                return RedirectToAction("FaqIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("FaqIndex", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteFaq(int id)
        {
            FaqViewModel model = new FaqViewModel();

            SaveReturn sr = model.DeleteFaq(id);

            return Json(new { saveReturn = sr });
        }
        #endregion

        public JsonResult GetProducts(string searchTerm)
        {
            return Json(SelectListViewModel.GetProductsArray(searchTerm).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetModels(string searchTerm)
        {
            return Json(SelectListViewModel.GetModelsArray(searchTerm).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}


