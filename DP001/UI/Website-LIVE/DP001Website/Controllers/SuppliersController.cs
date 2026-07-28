using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using DP001DataAccess.Entities;
using DP001BusinessLogic;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    [Authorize]
    public class SuppliersController : ApplicationController
    {
        private SupplierViewModel model;

        public ActionResult Inventory()
        {
            int channelId = GetChannelId();
            model = new SupplierViewModel(channelId);
            model.GetInventory();

            return View(model);
        }

        public JsonResult SearchInventory(string term, int brandFK)
        {
            if (term.Length < 4)
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
            var model = new SupplierViewModel(GetChannelId());
            var results = model.SearchInventory(term, brandFK).SearchResults
                .Select(x => new
                {
                    Des = x.Description,
                    Id = x.SupplierInventoryID,
                    Pn = x.ManufacturerPartNo,
                    Sn = x.Supplier.SupplierName
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        [DeleteFile]
        public FileResult ExportToExcel()
        {
            model = new SupplierViewModel(GetChannelId());
            model.GetInventory();

            return File(model.CreateExportFile(), Application.Octet, "PriceologyExport.csv");
        }

        //
        //Brand Aliases
        //
        public ActionResult BrandAliases()
        {
            int channelId = GetChannelId();
            model = new SupplierViewModel(channelId);
            model.GetBrandAliases();
            ViewBag.MvcGridAddRow = new HtmlString("<a href=\"/Suppliers/NewBrandAlias\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Brand Alias</button></a>");

            return View(model);
        }

        public ActionResult NewBrandAlias()
        {
            var model = new SupplierViewModel(GetChannelId());
            model.NewBrandAlias();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult CreateBrandAlias(SupplierViewModel model)
        {
            var saveReturn = model.CreateBrandAlias(GetChannelId());
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditBrandAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.EditBrandAlias(id);

            if (model.SupplierBrandMatchingEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("BrandAliases");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateBrandAlias(SupplierViewModel model)
        {
            model.ChannelID = GetChannelId();
            var saveReturn = model.UpdateBrandAlias(model.SupplierBrandMatchingEntry);
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        public ActionResult DeleteBrandAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.DeleteBrandAlias(id);

            return RedirectToAction("BrandAliases");
        }

        //
        //MfPN Aliases
        //
        public ActionResult MfPNAliases()
        {
            int channelId = GetChannelId();
            model = new SupplierViewModel(channelId);
            model.GetMfpnAliases();
            ViewBag.MvcGridAddRow = new HtmlString("<a href=\"/Suppliers/NewMfpnAlias\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Part No. Alias</button></a>");

            return View(model);
        }

        public ActionResult NewMfPNAlias()
        {
            var model = new SupplierViewModel(GetChannelId());
            model.NewMfpnAlias();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateMfPNAlias(SupplierViewModel model)
        {
            model.SupplierMfpnMatchingEntry.ChannelFK = GetChannelId();
            var saveReturn = model.CreateMfpnAlias();
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditMfPNAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.EditMfpnAlias(id);

            if (model.SupplierMfpnMatchingEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("MfpnAliases");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateMfPNAlias(SupplierViewModel model)
        {
            model.SupplierMfpnMatchingEntry.ChannelFK = GetChannelId();
            var saveReturn = model.UpdateMfpnAlias(model.SupplierMfpnMatchingEntry);
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        public ActionResult DeleteMfpnAlias(int id)
        {
            var model = new SupplierViewModel(GetChannelId());
            model.DeleteMfpnAlias(id);

            return RedirectToAction("MfpnAliases");
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