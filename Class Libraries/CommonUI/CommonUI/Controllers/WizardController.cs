using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace CommonUI.Controllers
{
    public class WizardController : ApplicationController
    {
        [HttpPost]
        public JsonResult ChangeManufacturer(string typename, string manufacturerId)
        {
            Int32 fixedManuId = 1;
            if (!int.TryParse(manufacturerId, out fixedManuId))
            {
                fixedManuId = 1;
            }

            string popp = "";
            string popr = "";
            string popc = "";
            int poppc = 0;
            int poprc = 0;
            int popcc = 0;
            string manutext = "";
            string printerlinks = "";
            string cdn = "";
            //string body = "";

            var model = new WizardViewModel();
            cdn = ConfigurationManager.AppSettings["CDN"];

            int typeId = 0;
            model.GetCartridgeTypes();
            if (typename != "")
            {
                typeId = model.CartridgeTypes.Find(x => x.LookupName.ToLower() == typename.ToLower()).AltLookupId.Value;
                model.CartridgeTypeName = typename;
            }

            model.GetPopularPrinters(fixedManuId, typeId);
            model.GetPopularRanges(fixedManuId, typeId);
            model.GetPopularCartridges(fixedManuId, typeId);

            //body = RenderPartialViewToString("~/Views/Equipment/PrinterWizard.cshtml", model);


            popp = RenderPartialViewToString("~/Views/Equipment/PopularPrinters.cshtml", model);
            poppc = model.PopularPrinters.Rows.Count;
            popr = RenderPartialViewToString("~/Views/Equipment/PopularRanges.cshtml", model);
            poprc = model.PopularRanges.Rows.Count;

            //Not on Home page
            model.ManufacturerName = "";
            if (typeId != 0)
            {
                int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                if (fixedManuId != 0)
                {
                    manufacturer manu = EntityAccess.ReadManufacturer(x => x.manufacturerID == fixedManuId).FirstOrDefault();
                    manu.manufacturerNotes = manu.manufacturerNotes.Where(x => x.eqCartridgeTypeFK == typeId && x.websiteFK == w).ToList();
                    manutext = "<h2>About our " + manu.manufacturerName + " " + typename + "</h2>";
                    manutext += string.IsNullOrEmpty(manu.manufacturerNotes.FirstOrDefault().note) ? "" : manu.manufacturerNotes.FirstOrDefault().note;
                    model.ManufacturerName = manu.manufacturerName;
                    model.Manufacturer = manu;
                }
                else
                {
                    manufacturerNote manunote = EntityAccess.ReadManufacturerNotes(x => x.eqCartridgeTypeFK == typeId && x.websiteFK == w).FirstOrDefault();
                    manutext = "<h2>" + typename + "</h2>" + manunote.note;
                }
                model.GetWizardLists(typename, fixedManuId, 0);
                printerlinks = RenderPartialViewToString("~/Views/Equipment/PrinterLinks.cshtml", model.WizDropDowns);
            }
            popc = RenderPartialViewToString("~/Views/Equipment/PopularCartridges.cshtml", model);
            popcc = model.PopularCartridges.Rows.Count;

            List<SelectListItem> fam = new List<SelectListItem>();
            List<ExtdSelectListItem> equ = new List<ExtdSelectListItem>();
            if (model.IsMobile)
            {
                fam = DataCache.GetFamilies(typename, fixedManuId);
                equ = DataCache.GetEquipment(typename, fixedManuId, 0);
            }

            return Json( new {
                popprint = popp,
                popprintcount = poppc,
                poprange = popr,
                poprangecount = poprc,
                popcart = popc,
                popcartcount = popcc,
                manutext = manutext,
                printerlinks = printerlinks,
                cdn = cdn,
                familylist = fam,
                equiplist = equ
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
            var model = new WizardViewModel();
            List<ExtdSelectListItem> list = manufacturerId == 0 || familyId == 0 ? new List<ExtdSelectListItem>() : DataCache.GetEquipment(type, manufacturerId, familyId);

            if (!model.IsMobile)
            {
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    equiplist = list
                });
            }
        }

        public JsonResult ChangeEquipment(int equipmentId = 0)
        {
            var model = new WizardViewModel();

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

            if (!model.IsMobile)
            {
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    cartlist = list
                });
            }
        }
    }
}