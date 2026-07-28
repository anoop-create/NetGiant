using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;

namespace netGiant.Intranet.Controllers.Ecommerce
{
    [Authorize(Roles = "IntranetAdmin")]
    public class EcommerceController : ApplicationController
    {
        public ActionResult Log()
        {
            return View();
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

            return Json(new {
                entry = model.LogEntry.Entry.Replace(Environment.NewLine, "<br />"),
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
            return View();
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
            return View();
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

            return PartialView("~/Views/Ecommerce/DeliveryLookup.cshtml", newLookup);
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
    }
}


