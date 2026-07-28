using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace DP001Website.Controllers
{
    [Authorize]
    public class SkuMappingsController : ApplicationController
    {
        private SkuMappingViewModel model;

        public ActionResult Index()
        {
            model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.InitializeReport();

            return View(model);
        }

        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.Get();

            var result = model.SkuMappingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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
            //model.GetSupplierExceptions();

            return View(model);
        }

        public ActionResult SupplierExceptions_Read([DataSourceRequest]DataSourceRequest request)
        {
            model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.GetSupplierExceptions();

            var result = model.SupplierExceptions.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult CompetitorExceptions()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            model = new SkuMappingViewModel(channelId, tenant);
            //model.GetCompetitorExceptions();

            return View(model);
        }

        public ActionResult CompetitorExceptions_Read([DataSourceRequest]DataSourceRequest request)
        {
            model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.GetCompetitorExceptions();

            var result = model.CompetitorExceptions.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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

        public ActionResult GetBrands([DataSourceRequest]DataSourceRequest data)
        {
            var model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.Get();

            var result = model.SkuMappingList.ToDataSourceResult(data);
            var brandList = ((IEnumerable<SkuMappingViewModel.TelerikSkuMappings>)result.Data)
                .OrderBy(x => x.BrandName)
                .Select(x => x.BrandName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSupplierCompetitors([DataSourceRequest]DataSourceRequest data)
        {
            var model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.Get();

            var result = model.SkuMappingList.ToDataSourceResult(data);
            var supplierCompetitorList = ((IEnumerable<SkuMappingViewModel.TelerikSkuMappings>)result.Data)
                .OrderBy(x => x.SupplierCompetitorName)
                .Select(x => x.SupplierCompetitorName)
                .Distinct()
                .ToList();

            return Json(supplierCompetitorList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMappingTypes([DataSourceRequest]DataSourceRequest data)
        {
            var model = new SkuMappingViewModel(GetChannelId(), GetTenant());
            model.Get();

            var result = model.SkuMappingList.ToDataSourceResult(data);
            var supplierCompetitorList = ((IEnumerable<SkuMappingViewModel.TelerikSkuMappings>)result.Data)
                .OrderBy(x => x.MappingType)
                .Select(x => x.MappingType)
                .Distinct()
                .ToList();

            return Json(supplierCompetitorList, JsonRequestBehavior.AllowGet);
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
