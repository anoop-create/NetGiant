using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    public class WizardController : ApplicationController
    {
        [HttpPost]
        public JsonResult ChangeManufacturer(string typename, int manufacturerId)
        {
            string popp = "";
            string popc = "";
            string manutext = "";
            string printerlinks = "";
            string cdn = "";

            var model = new WizardViewModel();
            cdn = ConfigurationManager.AppSettings["CDN"];

            int typeId = 0;
            model.GetCartridgeTypes();
            if (typename != "")
            {
                typeId = model.CartridgeTypes.Find(x => x.eqCartridgeTypeName.ToLower() == typename.ToLower()).eqCartridgeTypeID;
                model.CartridgeTypeName = typename;
            }
            model.GetPopularPrinters(manufacturerId, typeId);
            model.GetPopularCartridges(manufacturerId, typeId);
            popp = RenderPartialViewToString("~/Views/Equipment/PopularPrinters.cshtml", model);

            //Not on Home page
            model.ManufacturerName = "";
            if (typeId != 0)
            {
                int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                if (manufacturerId != 0)
                {
                    manufacturer manu = EntityAccess.ReadManufacturer(x => x.manufacturerID == manufacturerId).FirstOrDefault();
                    manu.manufacturerNotes = manu.manufacturerNotes.Where(x => x.eqCartridgeTypeFK == typeId && x.websiteFK == w).ToList();
                    manutext = "<h2>About our " + manu.manufacturerName + " " + typename + "</h2>" + manu.manufacturerNotes.FirstOrDefault().note;
                    model.ManufacturerName = manu.manufacturerName;
                }
                else
                {
                    manufacturerNote manunote = EntityAccess.ReadManufacturerNotes(x => x.eqCartridgeTypeFK == typeId && x.websiteFK == w).FirstOrDefault();
                    manutext = "<h2>" + typename + "</h2>" + manunote.note;
                }
                model.GetWizardLists(typename, manufacturerId, 0);
                printerlinks = RenderPartialViewToString("~/Views/Equipment/PrinterLinks.cshtml", model.WizDropDowns);
            }
            popc = RenderPartialViewToString("~/Views/Equipment/PopularCartridges.cshtml", model);

            return Json( new {
                popprint = popp,
                popcart = popc,
                manutext = manutext,
                printerlinks = printerlinks,
                cdn = cdn
            });
        }

        public JsonResult ChangeManufacturerFamily(string type, string search, int manufacturerId = 0)
        {
            List<SelectListItem> list = manufacturerId == 0 ? new List<SelectListItem>() : DataCache.GetFamilies(type, manufacturerId);

            if (!String.IsNullOrEmpty(search))
            {
                list = list.Where(x => x.Text.ToLower().Contains(search.ToLower())).ToList();
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ChangeManufacturerEquip(string type, string search, int familyId = 0, int manufacturerId = 0)
        {
            List<ExtdSelectListItem> list = manufacturerId == 0 ? new List<ExtdSelectListItem>() : DataCache.GetEquipment(type, manufacturerId, familyId);

            if (!String.IsNullOrEmpty(search))
            {
                list = list.Where(x => x.Text.ToLower().Contains(search.ToLower())).ToList();
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ChangeFamily(string type, int manufacturerId = 0, int familyId = 0)
        {
            List<ExtdSelectListItem> list = manufacturerId == 0 || familyId == 0 ? new List<ExtdSelectListItem>() : DataCache.GetEquipment(type, manufacturerId, familyId);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ChangeEquipment(int equipmentId = 0)
        {
            // Decided not to cache this as there would be too many
            List<eqProductMembership> lpm = EntityAccess.ReadProductMembership(x => x.eqEquipmentFK == equipmentId);
            List<ExtdSelectListItem> list = lpm.Count == 0 ? new List<ExtdSelectListItem>() : 
                lpm.Select(x => new ExtdSelectListItem
                {
                    Text = x.product.productName ?? "",
                    Value = x.productFK.ToString(),
                    Data = new { data_pyield = x.product.pageYield }
                })
                .OrderBy(x => x.Text)
                .ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult ChangeCartridge(int equipmentId = 0)
        //{
        //    // Decided not to cache this as there would be too many
        //    List<eqProductMembership> lpm = EntityAccess.ReadProductMembership(x => x.eqEquipmentFK == equipmentId);
        //    List<ExtdSelectListItem> list = lpm.Count == 0 ? new List<ExtdSelectListItem>() :
        //        lpm.Select(x => new ExtdSelectListItem
        //            {
        //                Text = x.product.productName ?? "",
        //                Value = x.productFK.ToString(),
        //                Data = new { data_pyield = x.product.pageYield }
        //            })
        //            .OrderBy(x => x.Text)
        //            .ToList();

        //    return Json(list, JsonRequestBehavior.AllowGet);
        //}
    }
}