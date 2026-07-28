using System;
using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class EquipmentController : WizardController
    {
        private EquipmentViewModel model;

        public ActionResult PrinterWizard(string typename = null, string manuname = null, string familyname = null)
        {
            model = new EquipmentViewModel();
            model.SetupWizard(typename, manuname, familyname);
            if (!string.IsNullOrEmpty(familyname) && model.Family == null)
            {
                // Couldn't find the famiily. Possible old-old style model page.
                string pattern = familyname.Replace("-", "_");
                eqEquipment e = EntityAccess.ReadEquipment(pattern);
                if (e != null)
                {
                    // Redirect to model page
                    return RedirectPermanent("/model/" + familyname + "-" + e.eqCartridgeType.eqCartridgeTypeName.ToLower().Replace(" ", "-") + "/");
                }
            }
            model.GetMeta(model.CartridgeType, model.Manufacturer, model.Family);

            int manuId = model.Manufacturer != null ? model.Manufacturer.manufacturerID : 0;
            ViewBag.ManufacturerName = model.Manufacturer != null
                ? model.Manufacturer.manufacturerName.Replace(" ", "-").ToLower()
                : "none";
            int typeId = model.CartridgeType != null ? model.CartridgeType.eqCartridgeTypeID : 0;
            int familyId = model.Family != null ? model.Family.eqFamilyID : 0;

            model.GetPopularPrinters(manuId, typeId);
            model.GetPopularCartridges(manuId, typeId);
            model.GetWizardLists(typename.Replace('-', ' '), manuId, familyId);
            if (manuId == 0)
            {
                int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                manufacturerNote manunote = EntityAccess.ReadManufacturerNotes(x => x.eqCartridgeTypeFK == typeId && x.manufacturerFK == null).FirstOrDefault();
                model.WizDropDowns.CartridgeTypeNote = manunote.note;
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
            decimal val = Decimal.Parse(Request.Form["PagesPrintedPerDay"]);
            int printsPerDay = Convert.ToInt32(val);
            val = Decimal.Parse(Request.Form["PageCoverage"]);
            int pageCoverage = Convert.ToInt32(val);
            int frequency = int.Parse(Request.Form["Frequency"]);
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

            if (p.websiteInventories.FirstOrDefault(x => x.websiteFK == w) != null)
            {
                wi = p.websiteInventories.FirstOrDefault(x => x.websiteFK == w);
                pi = wi.productImages.OrderByDescending(y => y.thumbnailImage).FirstOrDefault();
            }

            if (p != null && printsPerDay > 0)
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
                    ViewBag.Url = "/product/" + Utilities.CleanUrl(p.productName + "-" + p.partNo + "-" + p.AxisField.stockReference);
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