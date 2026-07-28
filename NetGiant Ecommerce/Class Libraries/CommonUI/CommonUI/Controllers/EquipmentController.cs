using System;
using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;

namespace CommonUI.Controllers
{
    [SiteOfflineCheck]
    public class EquipmentController : WizardController
    {
        private EquipmentViewModel model;

        public ActionResult PrinterWizard(string typename = null, string manuname = null, string familyname = null)
        {
            model = new EquipmentViewModel();
            model.SetupWizard(typename, manuname, familyname);

            //Canonical check
            var manufacturer = model.Manufacturer != null ? "/" + model.Manufacturer.manufacturerName.Replace(" ", "-") : "";
            manufacturer = model.Family != null && !String.IsNullOrEmpty(manufacturer) ? manufacturer + "/" + model.Family.description.Replace(" ", "-") : manufacturer;
            ViewBag.cUrl = "/" + Request.Url.Segments[1].Trim('/').ToLower() + manufacturer + "/";
            if (Request.Path != ViewBag.cUrl)
            {
                return RedirectPermanent(ViewBag.cUrl);
            }

            if (!string.IsNullOrEmpty(familyname) && model.Family == null)
            {
                // Couldn't find the family. Possible old-old style model page.
                string pattern = familyname.Replace("-", "_");
                eqEquipment e = EntityAccess.ReadEquipment(pattern);
                if (e != null)
                {
                    // Redirect to model page
                    return RedirectPermanent("/model/" + familyname + "-" + DataCache.GetCartridgeTypeName(e.eqCartridgeTypeFK).ToLower().Replace(" ", "-") + "/");
                }
            }
            model.GetMeta(model.CartridgeType, model.Manufacturer, model.Family);

            int manuId = model.Manufacturer != null ? model.Manufacturer.manufacturerID : 0;
            ViewBag.ManufacturerName = model.Manufacturer != null
                ? model.Manufacturer.manufacturerName.Replace(" ", "-").ToLower()
                : "none";
            int typeId = model.CartridgeType != null ? model.CartridgeType.AltLookupId.Value : 0;
            int familyId = model.Family != null ? model.Family.eqFamilyID : 0;

            model.GetPopularPrinters(manuId, typeId);
            model.GetPopularRanges(manuId, typeId);
            model.GetPopularCartridges(manuId, typeId);
            model.GetWizardLists(typename.Replace('-', ' '), manuId, familyId);
            if (manuId == 0)
            {
                int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                manufacturerNote manunote = EntityAccess.ReadManufacturerNotes(x => x.eqCartridgeTypeFK == typeId && x.manufacturerFK == null).FirstOrDefault();
                model.WizDropDowns.CartridgeTypeNote = manunote.note;
                model.WizDropDowns.PriorityNote = manunote.priorityNote;
                model.WizDropDowns.SecondaryNote = manunote.secondaryNote;
            }
            else
            {
                if (familyId == 0 && model.WizDropDowns.EquipList.Count == 0)
                {
                    // Redirect to type page
                    return RedirectPermanent("/" + typename + "/");
                }
            }

            model.BreadcrumbTrail.Add(model.CartridgeTypeName, model.CartridgeTypeName.ToLower().Replace(" ", "-") + "/");
            if (!string.IsNullOrEmpty(manuname))
            {
                model.BreadcrumbTrail.Add(manuname.Replace("-", " "), model.CartridgeTypeName.ToLower().Replace(" ", "-") + "/" + manuname + "/");
            }
            if (!string.IsNullOrEmpty(familyname))
            {
                if (familyname != manuname)
                {
                    model.BreadcrumbTrail.Add(familyname.Replace("-", " "), model.CartridgeTypeName.ToLower().Replace(" ", "-") + "/" + manuname + "/" + familyname + "/");
                }
            }

            int? manuIdForFaq = manuId;
            if (manuId == 0)
            {
                manuIdForFaq = null;
            }
            model.FaqList = EntityAccess.ReadFaq(x =>
                x.IsActive == true &&
                ((x.Lookup.LookupName == "Wizard Page" && (
                    (x.Lookup1.LookupName == "Universal") ||
                    (x.Lookup1.LookupName == "Cartridge Type" && x.CartridgeTypeFk == typeId && x.ManufacturerFk == manuIdForFaq) ||
                    (x.Lookup1.LookupName == "Manufacturer" && x.ManufacturerFk == manuId)
                    )) ||
                (x.Lookup.LookupName == "Universal"))
            );

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();
            ViewBag.FaqJson = model.BuildFaqJson();

            ViewBag.WizardFAQ = "";
            if (model.CartridgeTypeName == "Toner Cartridges" && model.Family == null && model.Manufacturer == null)
            {
                ViewBag.WizardFAQ = model.EquipmentData["TonerCartridgeJson"];
                if (!string.IsNullOrEmpty(ViewBag.WizardFAQ))
                {
                    ViewBag.WizardFAQ = "," + ViewBag.WizardFAQ;
                }
            }            

            ViewBag.ShowLinks = false;
            List<int> showManu = new List<int>();
            string manuArray = Utilities.GetItemFromDict(model.EquipmentData, "ShowFamilyEquipmentLinks");
            if (!string.IsNullOrEmpty(manuArray))
            {
                showManu = manuArray.Split(',').Select(Int32.Parse).ToList();
            }
            if (showManu.Contains(manuId))
            {
                ViewBag.ShowLinks = true;
            }

            return View(model);
        }

        [ChildActionOnly]
        public ActionResult RenderWizard()
        {
            model = new EquipmentViewModel();
            model.GetWizardLists();

            return PartialView("~/Views/Equipment/Wizard.cshtml", model);
        }

        public ActionResult CartridgeEnduranceTool(int id = 0, string ifid = "")
        {
            model = new EquipmentViewModel();
            model.GetWizardLists();
            model.FrequencyList = model.BuildFrequencyList();
            ViewBag.ShowDropDowns = id == 0;
            ViewBag.ProductId = id;
            ViewBag.IframeId = ifid;

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult CartridgeEnduranceCalc(EquipmentViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int productId = 0;

            if (String.IsNullOrEmpty(Request.Form["wiz-productid"]))
            {
                productId = String.IsNullOrEmpty(Request.Form["wiz-cartridge"]) ? 0 : int.Parse(Request.Form["wiz-cartridge"]);
            }
            else
            {
                productId = String.IsNullOrEmpty(Request.Form["wiz-productid"]) ? 0 : int.Parse(Request.Form["wiz-productid"]);
            }
            decimal val;
            Decimal.TryParse(Request.Form["PagesPrintedPerDay"], out val);
            int printsPerDay = Convert.ToInt32(val);
            val = String.IsNullOrEmpty(Request.Form["PageCoverage"]) ? 0 : Decimal.Parse(Request.Form["PageCoverage"]);
            int pageCoverage = Convert.ToInt32(val);
            int frequency = String.IsNullOrEmpty(Request.Form["Frequency"]) ? 0 : int.Parse(Request.Form["Frequency"]);
            product p = EntityAccess.ReadProduct(x => x.productID == productId && (x.productStatusFK == 1 || x.productStatusFK == 8)).FirstOrDefault();
            if (p == null)
            {
                sr.Html = "Unable to perform calculation";
                return Json(new
                {
                    savereturn = sr
                });
            }
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            websiteInventory wi = new websiteInventory();
            productImage pi = new productImage();

            if (p.websiteInventory.FirstOrDefault(x => x.websiteFK == w) != null)
            {
                wi = p.websiteInventory.FirstOrDefault(x => x.websiteFK == w);
                pi = wi.productImages.OrderByDescending(y => y.thumbnailImage).FirstOrDefault();
            }

            if (p != null && printsPerDay > 0 && pageCoverage > 0)
            {
                if (p.pageYield > 0)
                {
                    int fFactor = 1;
                    string fLegend = "day";
                    switch (frequency)
                    {
                        case 1:
                        {
                            break;
                        }
                        case 2:
                        {
                            fFactor = 7;
                            fLegend = "week";
                            break;
                        }
                        case 3:
                        {
                            fFactor = 30;
                           fLegend = "month";
                            break;
                        }
                        case 4:
                        {
                            fFactor = 365;
                            fLegend = "year";
                            break;
                        }
                    }
                    sr.IsSuccess = true;
                    decimal a = Decimal.Divide(((p.pageYield ?? 0) * 5 * fFactor), (pageCoverage * printsPerDay));
                    string endurance = "";
                    string comma = "";
                    if (a > 365)
                    {
                        int b = (int)a / 365;
                        a = a - (b * 365);
                        endurance += b + (b > 1 ? " years" : " year");
                        comma = ", ";
                    }
                    if (a > 30)
                    {
                        int b = (int)a / 30;
                        a = a - (b * 30);
                        endurance += comma + b + (b > 1 ? " months" : " month");
                        comma = ", ";
                    }
                    if (a > 7)
                    {
                        int b = (int)a / 7;
                        a = a - (b * 7);
                        endurance += comma + b + (b > 1 ? " weeks" : " week");
                    }
                    comma = (endurance != "" ? " & " : "");
                    endurance += comma + a.ToString("F1") + (a > 1 ? " days " : " day ");

                    ViewBag.Endurance = endurance;
                    ViewBag.Url = "/product/" + Utilities.CleanUrl(p.productName + "-" + p.partNo + "-" + p.AxisFields.stockReference);
                    ViewBag.ImageUrl = pi != null ? ConfigurationManager.AppSettings["CDN"] + "/" + pi.URL : "";
                    ViewBag.PrintsPerDay = printsPerDay.ToString("N0");
                    ViewBag.PageCoverage = pageCoverage;
                    ViewBag.ProductName = p.productName;
                    ViewBag.PageYield = (p.pageYield ?? 0).ToString("N0");
                    ViewBag.FLegend = fLegend;

                    sr.Html = RenderPartialViewToString("~/Views/Equipment/EnduranceCalc.cshtml", model);
                }
                else
                {
                    sr.Html = "<div>Sorry. We don't have page yield information for that cartridge.</div>";
                }
            }
            else
            {
                sr.Html = "<div>Please check that you have selected a printer cartridge and have entered page usage and page coverage details.</div>";
            }
            
            return Json(new
            {
                savereturn = sr
            });
        }
    }
}