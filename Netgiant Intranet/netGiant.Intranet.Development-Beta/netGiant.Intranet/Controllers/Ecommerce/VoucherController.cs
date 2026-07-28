using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.Models;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Ecommerce
{
    [Authorize(Roles = "IntranetAdmin, PMSAdmin, PMSReader")]
    public class VoucherController : Controller
    {
        public ActionResult PromotionalVouchersIndex()
        {
            var model = new PromotionalVouchersViewModel();
            return View("PromotionalVouchers", model);
        }

        public ActionResult ReadPromotionalVouchers([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PromotionalVouchersViewModel();
            model.GetPromotionalVouchers();

            var result = model.PromotionalVouchersList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult CustomerVouchersIndex()
        {
            var model = new PromotionalVouchersViewModel();
            return View("CustomerVouchers", model);
        }

        public ActionResult ReadCustomerVouchers([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PromotionalVouchersViewModel();
            model.GetCustomerVouchers();

            var result = model.CustomerVoucherList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreatePromotionalVoucher(int id)
        {
            ViewBag.PromotionalVoucherId = id;
            TempData["ParentAction"] = "CreatePromotionalVoucher";
            TempData.Keep("ParentAction");
            return View(PromotionalVouchersViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SavePromotionalVoucher(PromotionalVouchersViewModel model)
        {
            var sr = model.Save();
            
            return Json(new
            {
                saveReturn = sr
            });
        }

        public ActionResult DetailPromotionalVoucher(int id)
        {
            ViewBag.PromotionalVoucherId = id;
            TempData["ParentAction"] = "Detail";
            TempData.Keep("ParentAction");
            var model = new PromotionalVoucherDetailViewModel();
            return View("PromotionalVoucherDetail", model.GetPromotionalVoucherDetail(id));
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            PromotionalVouchersViewModel model = new PromotionalVouchersViewModel();

            SaveReturn sr = model.Delete(id);

            return Json(new { saveReturn = sr });
        }
        public ActionResult PromotionalVoucherGroupsIndex()
        {
            var model = new PromotionalVoucherGroupsViewModel();
            return View("PromotionalVoucherGroups", model);
        }

        public ActionResult ReadPromotionalVoucherGroups([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PromotionalVoucherGroupsViewModel();
            model.GetPromotionalVoucherGroups();

            var result = model.PromotionalVoucherGroupsList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreatePromotionalVoucherGroup(int id)
        {
            ViewBag.PromotionalVoucherGroupId = id;
            TempData["ParentAction"] = "CreatePromotionalVoucherGroup";
            TempData.Keep("ParentAction");
            return View(PromotionalVoucherGroupsViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SavePromotionalVoucherGroup(PromotionalVoucherGroupsViewModel model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);

            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Voucher Group Saved";
            }

            return RedirectToAction("CreatePromotionalVoucherGroup", new { id = model.pvouchgrp.VoucherPromoGroupId });
        }

        public ActionResult DetailPromotionalVoucherGroup(int id)
        {
            ViewBag.PromotionalVoucherGroupId = id;
            TempData["ParentAction"] = "Detail";
            TempData.Keep("ParentAction");
            var model = new PromotionalVoucherGroupDetailViewModel();
            return View("PromotionalVoucherGroupDetail", model.GetPromotionalVoucherGroupDetail(id));
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeletePromotionalVoucherGroup(List<string> optionsArray)
        {
            var model = new PromotionalVoucherGroupsViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Voucher Group Deleted";
            return Json(new JsonModel());
        }


        public ActionResult ReadPromoGroupMappings([DataSourceRequest]DataSourceRequest request, int id) // not a mapping id, but voucher group id
        {
            var model = new PromoGroupMappingDetailViewModel();
            model.GetPromoGroupMappingDetailViewModel(id);

            var result = model.PromoGroupMappingsList.ToDataSourceResult(request);
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreatePromoGroupMapping(int id) // not a mapping id, but voucher group id
        {
            ViewBag.PromotionalVoucherGroupId = id;
            TempData["ParentAction"] = "CreatePromoGroupMapping";
            TempData.Keep("ParentAction");
            return View(PromoGroupMappingDetailViewModel.Create(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreatePromoGroupToCategoryMapping(int voucherGroupId, string categoryIds)
        {
            TempData["ParentAction"] = "CreatePromoGroupToCategoryMapping";
            TempData.Keep("ParentAction");

            var model = new PromoGroupMappingDetailViewModel();
            var sr = model.CreatePromoGroupMapping(voucherGroupId, categoryIds);

            return Json(new
            {
                saveReturn = sr
            });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeletePromoGroupMapping(List<string> optionsArray)
        {
            var model = new PromoGroupMappingDetailViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Promo Group Mapping Deleted";
            return Json(new JsonModel());
        }

        [HttpGet]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public string GenerateVoucherCode()
        {
            string voucherCode = "";
            string seconds = Math.Round((DateTime.Now - (new DateTime(2018, 01, 01))).TotalSeconds, 0).ToString();

            char[] letters = { 'W', 'H', 'T', 'Y', 'E', 'A', 'P', 'L', 'F', 'D' };
            foreach (char c in seconds)
            {
                voucherCode += letters[int.Parse(c.ToString())];
            }

            return voucherCode;
        }

        public JsonResult OnSelectWebsite(int id)
        {
            var model = new PromotionalVouchersViewModel();
            List<SelectListItem> list = model.GetPromotionalGroups(id);
            string stockRef = model.GetStockRef(id);

            return Json(new {
                list = list,
                stockRef = stockRef 
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
