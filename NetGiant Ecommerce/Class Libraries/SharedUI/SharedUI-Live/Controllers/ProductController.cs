using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using SharedUI.Models;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class ProductController : ApplicationController
    {
        private ProductViewModel model;

        public ActionResult Index(string productname, string id)
        {
            if (id == null)
            {
                return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
            }

            model = new ProductViewModel();
            model.GetProductDetail(id);

            if (model.Product == null)
            {
                obsoleteItem oi = EntityAccess.ReadObsoleteItem(x => x.stockReference == id).FirstOrDefault();
                if (oi != null)
                {
                    return RedirectPermanent("/" + oi.URL);
                }
                else
                {
                    if (int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) == 3)
                    {
                        // See if the inactive product is a stationery item
                        if (EntityAccess.ReadProduct(x => x.AxisField.stockReference == id).FirstOrDefault()
                                ?.productGroup.productTypeFK == 6)
                        {
                            return RedirectPermanent("/stationery");
                        }
                    }
                }
                return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
            }

            if (String.IsNullOrEmpty(productname) || model.Product.Url.ToLower() != "product/" + productname.ToLower() + "-" + id + "/")
            {
                return RedirectPermanent("/" + model.Product.Url);
            }

            model.PpcSuppress = model.CheckPPCSuppression(model.Product.Brand);
            model.GetMeta("index", int.Parse(id));
            model.GetModelList(model.Product.ProductId);
            model.GetProductQA(model.Product.ProductId);
            model.GetSpecification(new List<DataSupplierAttributeLookup> { new DataSupplierAttributeLookup { PartNo = model.Product.PartNo, ManufacturerName = model.Product.Brand } });

            Utilities.AddToRecentViewed(new RecentlyViewed{
                Type = "Product",
                Reference = model.Product.Reference,
                Description = model.Product.Description,
                Url = "/" + model.Product.Url,
                ImageUrl = model.Product.ImageUrl
            });

            int basketCount = 0;
            if (model.BasketContents != null)
            {
                BasketContents bc = model.BasketContents.Find(x => x.StockRef == model.Product.Reference);
                basketCount = bc != null ? bc.Quantity : 0;
            }
            ViewBag.BasketCount = basketCount;

            ViewBag.Brand = model.Product.Brand;
            if (ViewBag.Brand == "Katun")
            {
                ViewBag.Brand = "Xerox";
            }
            if (ViewBag.Brand == "Own Brand")
            {
                ViewBag.Brand = model.Product.CrossSellBrand;
            }

            StringBuilder replacements = new System.Text.StringBuilder();
            replacements.Append("questionarea=" + model.Product.Description.Replace("&", "and"));
            replacements.Append("&granularity=7");
            replacements.Append("&equipid=");
            replacements.Append("&prodid=" + model.Product.ProductId);
            replacements.Append("&altref=" + model.Product.Reference);
            ViewBag.AskAQuestionRep = replacements.ToString();

            return View(model);
        }

        public ActionResult ProductList(string equipname, string filter = "")
        {
            model = new SearchViewModel();
            model.GetMeta(equipname);

            string equip = equipname
                .Replace("-toner-cartridges", "")
                .Replace("-solid-ink-cartridges", "")
                .Replace("-ink-cartridges", "")
                .Replace("-franking-cartridges", "");

            model.GetDetailForModel(equip);
            if (model.EquipmentDetail.Rows.Count == 0)
            {
                string pattern = equip.Replace("-", "_");
                obsoleteItem oi = EntityAccess.ReadObsoleteItem(pattern);
                if (oi != null)
                {
                    return RedirectPermanent("/" + oi.URL);
                }
                else
                {
                    return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
                }
            }

            if (model.EquipmentDetail.Rows[0]["EquipURL"].ToSafeString().Replace("hp-range", "toner-cartridges")
                    .ToLower() != "/model/" + equipname.ToLower() + "/")
            {
                return RedirectPermanent(model.EquipmentDetail.Rows[0]["EquipURL"].ToSafeString());
            }
            model.PpcSuppress = model.CheckPPCSuppression(model.EquipmentDetail.Rows[0]["Brand"].ToSafeString());

            model.GetProductsForModel(equip);
            model.GetPopularPrinters(int.Parse(model.EquipmentDetail.Rows[0]["ManufacturerID"].ToString()), int.Parse(model.EquipmentDetail.Rows[0]["CartridgeType"].ToString()), 30);
            if (filter != "")
            {
                model.SetProductFilters(filter);
            }
            if (model.EquipmentDetail != null)
            {
                model.GetEquipmentQA(int.Parse(model.EquipmentDetail.Rows[0]["ModelID"].ToString()));
            }
            Utilities.AddToRecentViewed(new RecentlyViewed
            {
                Type = "Model",
                Reference = model.EquipmentDetail.Rows[0]["ModelID"].ToString(),
                Description = model.EquipmentDetail.Rows[0]["Description"].ToString(),
                Url = model.EquipmentDetail.Rows[0]["EquipURL"].ToString(),
                ImageUrl = model.EquipmentDetail.Rows[0]["ThumbnailURL"].ToString()
            });
            StringBuilder replacements = new System.Text.StringBuilder();
            replacements.Append("equipid=" + model.EquipmentDetail.Rows[0]["ModelID"].ToString());
            replacements.Append("&modelname=" + model.EquipmentDetail.Rows[0]["Description"].ToString());
            if (Authentication.IsAuthenticated())
            {
                replacements.Append("&customerid=" + (Session["U_Record"].ToString().Contains("/") ? Session["U_Record"].ToString() : Session["U_Email"].ToString() ));
            }
            ViewBag.SaveMyPrinterRep = replacements.ToString();

            replacements = new System.Text.StringBuilder();
            replacements.Append("questionarea=" + model.EquipmentDetail.Rows[0]["Description"].ToString());
            replacements.Append("&granularity=7");
            replacements.Append("&equipid=" + model.EquipmentDetail.Rows[0]["ModelID"].ToString());
            replacements.Append("&prodid=");
            replacements.Append("&altref=");
            ViewBag.AskAQuestionRep = replacements.ToString();

            return View(model);
        }

        public ActionResult Grid(string categoryname, int id, string filter = "")
        {
            model = new ProductViewModel();
            model.CategoryId = id;
            model.GetCategory(id);
            model.GetMeta("grid", id);
            model.GetProductsForCategory(id);
            if (filter != "")
            {
                model.SetProductFilters(filter);
            }
            //model.GetCategoryName(id);
            if (model.CategoryCode == null || model.ProductList.Count == 0)
            {
                // Category not found or contains no products
                return RedirectPermanent("/");
            }

            if ("products/" + Utilities.CleanUrl(model.CategoryCode.categoryCodeName).ToLower() + "-" + id + "/" != "products/" + categoryname.ToLower() + "-" + id + "/")
            {
                return RedirectPermanent("/products/" + Utilities.CleanUrl(model.CategoryCode.categoryCodeName) + "-" + id + "/");
            }

            model.GetCategoryQA(id);

            StringBuilder replacements = new System.Text.StringBuilder();
            replacements.Append("questionarea=" + model.CategoryCode.categoryCodeName);
            replacements.Append("&granularity=7");
            replacements.Append("&equipid=");
            replacements.Append("&prodid=");
            replacements.Append("&altref=");
            ViewBag.AskAQuestionRep = replacements.ToString();

            return View(model);
        }

        public ActionResult Catalogue(string catalogname, int id)
        {
            model = new ProductViewModel();
            model.GetCategory(id);
            model.GetMeta("grid", id);
            model.GetSubCategories(id);

            return View(model);
        }

        [HttpPost]
        public JsonResult BasketAdd(string productref, int? productqty, decimal productprice, bool isadmindiscount, int? type = 0, int? lineUid = 0)
        {
            SaveReturn sr = new SaveReturn();

            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            if (productqty == null)
            {
                sr.IsSuccess = false;
                sr.Message = "Quantity is invalid";

                if (Session["B_BasketSummary"] == null)
                {
                    Session["B_BasketSummary"] = "";
                }
                if (Session["B_BasketQuantity"] == null)
                {
                    Session["B_BasketQuantity"] = 0;
                }
                if (Session["B_BasketTotal"] == null)
                {
                    Session["B_BasketTotal"] = 0;
                }
            }
            else
            {
                BasketContents bc = new BasketContents();
                bc.StockRef = productref;
                bc.Quantity = productqty ?? 0;
                bc.PriceEx = productprice;
                bc.IsAdminDiscount = isadmindiscount;
                if (bc.IsAdminDiscount)
                {
                    bc.Availability = 1;
                    bc.ImageUrl = "unknown.jpg";
                    bc.Description = "Administrator Discount";
                    bc.PartNo = "ADDCOUNT";
                }
                if (bc.PriceEx != 0)
                {
                    bc.PriceInc = productprice *
                                  Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());
                }
                bc.Type = type ?? 0;
                bc.LineUid = lineUid ?? 0;

                sr = Basket.Update(bc);
            }

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];

            return Json(new {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = Session["B_BasketSummary"],
                basketQuantity = bt.Quantity.ToString("##0"),
                //basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : bt.TotalExcVat.ToString("#,###,##0.00"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : (bt.TotalExcVat - bt.Delivery).ToString("#,###,##0.00")
            });
        }

        public JsonResult BasketDelete(string productref)
        {
            SaveReturn sr = new SaveReturn();

            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            sr = Basket.Delete(productref);

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];

            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = Session["B_BasketSummary"],
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalExcVat.ToString("#,###,##0.00") : bt.TotalIncVat.ToString("#,###,##0.00")
            });
        }

        [HttpPost]
        public ActionResult Comparison(string productsToCompare)
        {
            var model = new ProductViewModel();
            model.Compare(productsToCompare);

            return PartialView(model);
        }
    }    
}
