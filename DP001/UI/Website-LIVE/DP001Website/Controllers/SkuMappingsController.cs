using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;

namespace DP001Website.Controllers
{
    [Authorize]
    public class SkuMappingsController : ApplicationController
    {
        private SkuMappingViewModel model;

        public ActionResult Index()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.Get();

            return View(model);
        }

        public ActionResult EditSkuMapping(int id)
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.EditSkuMapping(id);

            if (model.SkuMappingRecord != null)
            {
                return View(model);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public ActionResult SupplierExceptions()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.GetSupplierExceptions();

            return View(model);
        }

        public ActionResult CompetitorExceptions()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.GetCompetitorExceptions();

            return View(model);
        }

        public ActionResult EditSupplierException(int id)
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.EditSupplierException(id);

            if (model.SupplierException != null)
            {
                return View(model);
            }
            else
            {
                return RedirectToAction("Index", "SkuMappings");
            }
        }

        public ActionResult EditCompetitorException(int id)
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            model.EditCompetitorException(id);

            if (model.CompetitorException != null)
            {
                return View(model);
            }
            else
            {
                return RedirectToAction("Index", "SkuMappings");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult SaveSupplierException(SkuMappingViewModel model)
        {
            int channelId = GetChannelId();
            var saveReturn = model.UpdateSupplierMappings(model.SuggestedSupplierMappings, model.SupplierException, channelId);

            return Json(new
            {
                isSuccess = saveReturn.IsSuccess,
                msg = saveReturn.Message
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult SaveManualSupplierException(SkuMappingViewModel model)
        {
            var channelID = GetChannelId();
            var saveReturn = model.UpdateSingleSupplierMapping(model.SupplierInventoryFK, model.SupplierException.ProductInventoryID, channelID);

            return Json(new
            {
                isSuccess = saveReturn.IsSuccess,
                msg = saveReturn.Message
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult SaveCompetitorException(SkuMappingViewModel model)
        {
            int channelId = GetChannelId();
            var saveReturn = model.UpdateCompetitorMappings(model.SuggestedCompetitorMappings, model.CompetitorException, channelId);

            return Json(new
            {
                isSuccess = saveReturn.IsSuccess,
                msg = saveReturn.Message
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult SaveManualCompetitorException(SkuMappingViewModel model)
        {
            var channelID = GetChannelId();
            var saveReturn = model.UpdateSingleCompetitorMapping(model.CompetitorInventoryFK, model.CompetitorException.ProductInventoryID, channelID);

            return Json(new
            {
                isSuccess = saveReturn.IsSuccess,
                msg = saveReturn.Message
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Update")]
        public JsonResult Update(SkuMappingViewModel model)
        {
            int channelId = GetChannelId();
            model.SkuMappingRecord.Mapping.ChannelFK = channelId;
            var saveReturn = model.UpateSkuMapping();
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SkuMappingRecord.Mapping.SKUMappingID, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SkuMappingRecord.Mapping.SKUMappingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id, int invId)
        {
            int channelId = GetChannelId();
            var model = new SkuMappingViewModel(channelId, GetTenant());
            var saveReturn = model.Delete(id, invId);

            if (saveReturn.IsSuccess)
            {
                return Json(new { IsSuccess = true, Id = id, Action = "Delete", Msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { IsSuccess = false, Id = id, Action = "Delete", Msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetMappingTypes()
        {
            var list = new List<string>();
            list.Add("Competitor");
            list.Add("Supplier");

            return Json(new { Items = list }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSupplierCompetitors()
        {
            var model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            var list = model.GetSuppliersAndCompetitors();

            return Json(new { Items = list }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (model != null)
                    model.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}