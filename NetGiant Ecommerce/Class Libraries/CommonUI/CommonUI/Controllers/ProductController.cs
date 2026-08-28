using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using CommonUI.Models;
using ASP;

namespace CommonUI.Controllers
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

            model = new SearchViewModel();
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
                        if (EntityAccess.ReadProduct(x => x.AxisFields.stockReference == id).FirstOrDefault()
                                ?.productGroup.productTypeFK == 6)
                        {
                            return RedirectPermanent("/stationery");
                        }
                    }
                }
                return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
            }

            //Canonical check
            ViewBag.cUrl = "/" + model.Product.Url;
            if (Request.Path != ViewBag.cUrl)
            {
                return RedirectPermanent(ViewBag.cUrl);
            }

            if (String.IsNullOrEmpty(productname) || model.Product.Url.ToLower() != "product/" + productname.ToLower() + "-" + id + "/")
            {
                return RedirectPermanent("/" + model.Product.Url);
            }

            model.PpcSuppress = model.CheckPPCSuppression(model.Product.Brand);
            model.CrossSellSuppress = model.CheckCrossSellSuppression(model.Product.Brand, model.Product.CrossSellBrand);
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
            BasketContents bc = new BasketContents();
            if (model.BasketContents != null)
            {
                bc = model.BasketContents.Find(x => x.StockRef == model.Product.Reference);
                basketCount = bc != null ? bc.Quantity : 0;
            }
            ViewBag.BasketCount = basketCount;
            ViewBag.BasketContents = bc;

            ViewBag.Brand = model.Product.Brand;
            if (ViewBag.Brand == "Katun")
            {
                ViewBag.Brand = "Xerox";
            }
            if (ViewBag.Brand == "Own Brand")
            {
                ViewBag.Brand = model.Product.CrossSellBrand;
            }
            ViewBag.ImageSash = model.GetImageSash();

            StringBuilder replacements = new System.Text.StringBuilder();
            replacements.Append("questionarea=" + model.Product.Description.Replace("&", "and"));
            replacements.Append("&granularity=7");
            replacements.Append("&equipid=");
            replacements.Append("&prodid=" + model.Product.ProductId);
            replacements.Append("&altref=" + model.Product.Reference);
            ViewBag.AskAQuestionRep = replacements.ToString();

            model.FaqList = EntityAccess.ReadFaq(x =>
                x.IsActive == true &&
                ((x.Lookup.LookupName == "Product Page" && (
                    (x.Lookup1.LookupName == "Universal") ||
                    //(x.Lookup1.LookupName == "Cartridge Type" && x.CartridgeTypeFk == model.Product.ProductTypeID) ||          // Needs fixing
                    (x.Lookup1.LookupName == "Manufacturer" && x.product.manufacturerFK == model.Product.ManufacturerId) ||
                    (x.Lookup1.LookupName == "Product" && x.ProductFk == model.Product.ProductId)
                    )) ||
                (x.Lookup.LookupName == "Universal"))
            );

            if ("Toner,Ink,Franking,Solid Ink".Contains(model.Product.Type))
            {
                model.BreadcrumbTrail.Add(model.Product.Type + " Cartridges", (model.Product.Type + " Cartridges").ToLower().Replace(' ', '-') + "/");
                if (ViewBag.Brand != "")
                {
                    model.BreadcrumbTrail.Add(ViewBag.Brand, (model.Product.Type + " Cartridges").ToLower().Replace(' ', '-') + "/" + ViewBag.Brand.Replace(' ', '-') + "/");
                }
            }
            else
            {
                model.BreadcrumbTrail.Add(model.Product.CategoryCodeName, "products/" + Utilities.CleanUrl(model.Product.CategoryCodeName) + "-" + model.Product.AxisGroupNo + "/");
            }
            model.BreadcrumbTrail.Add(model.Product.Description, model.Product.Url);

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();
            ViewBag.ProductJson = model.BuildProductJson();
            ViewBag.FaqJson = model.BuildFaqJson();

            return View(model);
        }

        public ActionResult ProductList(string equipname, string filter = "")
        {
            model = new SearchViewModel();            

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

            //Canonical check
            ViewBag.cUrl = "/model/" + Convert.ToString(model.EquipmentDetail.Rows[0]["Description"]).Replace(" ", "-").Replace(".", "-") + "-" 
                + Convert.ToString(model.EquipmentDetail.Rows[0]["CartridgeTypeName"]).RangeReplace() + "/";
            if (Request.Path != ViewBag.cUrl)
            {
                return RedirectPermanent(ViewBag.cUrl);
            }

            int manuId = int.Parse(model.EquipmentDetail.Rows[0]["ManufacturerID"].ToString());
            int cartridgeTypeId = int.Parse(model.EquipmentDetail.Rows[0]["CartridgeType"].ToString());
            int equipmentId = int.Parse(model.EquipmentDetail.Rows[0]["ModelID"].ToString());

            model.GetMeta(equipname);
            model.PpcSuppress = model.CheckPPCSuppression(model.EquipmentDetail.Rows[0]["Brand"].ToSafeString());

            model.GetProductsForModel(equip);
            model.GetPrintersForProducts();
            model.GetPopularPrinters(manuId, cartridgeTypeId, 30);
            if (filter != "")
            {
                model.SetProductFilters(filter);
            }
            if (model.EquipmentDetail != null)
            {
                model.GetEquipmentQA(equipmentId);
            }
            Utilities.AddToRecentViewed(new RecentlyViewed
            {
                Type = "Model",
                Reference = model.EquipmentDetail.Rows[0]["ModelID"].ToString(),
                Description = model.EquipmentDetail.Rows[0]["Description"].ToString(),
                Url = model.EquipmentDetail.Rows[0]["EquipURL"].RangeReplace(false),
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
            ViewBag.ShowSwitchBanner = model.SearchList.Find(x => x.Product.BrandFlag == BrandFlag.Compatible) != null;
            ViewBag.ShowSavings = Utilities.GetItemFromDict(model.ProductData, "SwitchToBanner").Length > 0;

            replacements = new System.Text.StringBuilder();
            replacements.Append("questionarea=" + model.EquipmentDetail.Rows[0]["Description"].ToString());
            replacements.Append("&granularity=7");
            replacements.Append("&equipid=" + model.EquipmentDetail.Rows[0]["ModelID"].ToString());
            replacements.Append("&prodid=");
            replacements.Append("&altref=");
            ViewBag.AskAQuestionRep = replacements.ToString();

            model.FaqList = EntityAccess.ReadFaq(x =>
                x.IsActive == true &&
                ((x.Lookup.LookupName == "Model Page" && (
                    (x.Lookup1.LookupName == "Universal") ||
                    (x.Lookup1.LookupName == "Cartridge Type" && x.eqEquipment.eqCartridgeTypeFK == cartridgeTypeId) || 
                    (x.Lookup1.LookupName == "Manufacturer" && x.eqEquipment.manufacturerFK == manuId) ||
                    (x.Lookup1.LookupName == "Model" && x.EquipmentFk == equipmentId)
                    )) ||
                (x.Lookup.LookupName == "Universal"))
            );

            model.BreadcrumbTrail.Add(model.EquipmentDetail.Rows[0]["CartridgeTypeName"].RangeReplace(false), model.EquipmentDetail.Rows[0]["CartridgeTypeName"].RangeReplace()  + "/");
            model.BreadcrumbTrail.Add(model.EquipmentDetail.Rows[0]["Brand"].ToString(), model.EquipmentDetail.Rows[0]["CartridgeTypeName"].RangeReplace() + "/" + model.EquipmentDetail.Rows[0]["Brand"].ToString().Replace(' ', '-') + "/");
            model.BreadcrumbTrail.Add(equip.Replace("-", " "), "model/" + equipname + "/");

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();
            ViewBag.FaqJson = model.BuildFaqJson();

            return View(model);
        }

        public ActionResult Grid(string categoryname, int id, string filter = "")
        {
            model = new ProductViewModel();
            model.CategoryId = id;
            model.GetCategory(id);

            //if model.GetCategory(id) comes back empty(ie no longer listed)
            if (model.CategoryCode == null)
            {
                return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
            }

            //Canonical check
            ViewBag.cUrl = "/products/" + model.CategoryCode.categoryCodeName.Replace(" ", "-") + "-" + model.CategoryId + "/";
            if (Request.Path != ViewBag.cUrl)
            {
                return RedirectPermanent(ViewBag.cUrl);
            }

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
                switch (int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()))
                {
                    case 3:
                        return RedirectPermanent("/stationery/");
                    default:
                        return RedirectPermanent("/");
                }
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

            model.BreadcrumbTrail.Add(model.CategoryCode.categoryCodeName.Replace("-", " "), "products/" + Utilities.CleanUrl(model.CategoryCode.categoryCodeName) + "-" + id + "/");
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        public ActionResult Catalogue(string catalogname, int id)
        {
            model = new ProductViewModel();
            model.GetCategory(id);
            if (model.CategoryCode != null)
            {
                if (Utilities.CleanUrl(model.CategoryCode.categoryCodeName) != catalogname)
                {
                    return RedirectPermanent("/catalogue/" + Utilities.CleanUrl(model.CategoryCode.categoryCodeName) + "-" + id + "/");
                }
            }
            else
            {
                // Category id doesn't exist
                return new MVCTransferResult("/error/index/404?asperrorpath=" + Request["URL"]);
            }
            model.GetMeta("grid", id);
            model.GetSubCategories(id);

            model.BreadcrumbTrail.Add(catalogname.Replace("-", " "), "catalogue/" + Utilities.CleanUrl(model.CategoryCode.categoryCodeName) + "-" + id + "/");
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        [HttpPost]
        public JsonResult BasketAdd(string productref, int? productqty, decimal productprice, BasketItemType itemtype, int? type = 0, int? lineUid = 0)
        {
            SaveReturn sr = new SaveReturn();
            model = new ProductViewModel();
            string infoMessage = "";
            string priceMessage = "";
            string basketSummary = "";

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
                bc.ItemType = itemtype;
                //bc.IsAdminDiscount = isadmindiscount;
                if (bc.ItemType == BasketItemType.AdminDiscount)
                {
                    bc.Availability = 1;
                    bc.ImageUrl = "unknown.jpg";
                    bc.Description = "Administrator Discount";
                    bc.PartNo = "ADDCOUNT";
                }
                if (bc.ItemType == BasketItemType.Alert)
                {
                    bc.Availability = 1;
                    bc.ImageUrl = "unknown.jpg";
                    switch (bc.StockRef)
                    {
                        case "ONHOLD":
                        {
                            bc.Description = "On Hold By Customer Services";
                            break;
                        }
                        case "ACCOUNTAPP":
                        {
                            bc.Description = "Account Application In Progress";
                            break;
                        }
                        case "BADADDRESS":
                        {
                            bc.Description = "Invalid Post Code";
                            break;
                        }
                    }
                    bc.PartNo = productref;
                }
                if (bc.PriceEx != 0)
                {
                    bc.PriceInc = productprice *
                                  Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());
                }
                bc.Type = type ?? 0;
                bc.LineUid = lineUid ?? 0;

                sr = Basket.Update(bc);
                sr.Html = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);
                // basketSummary feeds the mini-cart widget (#minibasket-widget in
                // MiniBasket.cshtml) via site.js's $('#minibasket-widget').replaceWith(...) -
                // it must render that same view, not the retired BasketSummary.cshtml (which
                // has no #minibasket-widget wrapper, so the client-side replaceWith silently
                // matched nothing and the mini-cart never visibly updated until a full page
                // reload re-rendered the header from scratch).
                basketSummary = RenderPartialViewToString("~/Views/Shared/MiniBasket.cshtml", model);

                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                var i = lbc.FindIndex(x => x.StockRef == bc.StockRef);
                if (i >= 0)
                {
                    bool productHasOffer = model.IsCompatibleSaleActive
                        || (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]) && Convert.ToBoolean(Session["U_IsFromPPC"]) && ConfigurationManager.AppSettings["PPCPromoAppl"] != "OEM");

                    ViewData.Add("Section", "Info");
                    ViewData.Add("ProductHasOffer", productHasOffer);
                    infoMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", lbc[i]);
                    ViewData.Clear();
                    ViewData.Add("Section", "Price");
                    ViewData.Add("ProductHasOffer", productHasOffer);
                    ViewData.Add("PriceEx", lbc[i].PriceEx);
                    ViewData.Add("PriceInc", lbc[i].PriceInc);
                    priceMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", lbc[i]);
                }
            }

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];

            return Json(new {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = basketSummary,
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : (bt.TotalExcVat - bt.Delivery).ToString("#,###,##0.00"),
                productInfoMessage = infoMessage,
                productPriceMessage = priceMessage
            });
        }

        /// <summary>
        /// Sets a basket line to an absolute quantity (used by the mini-cart and basket page
        /// quantity steppers). Basket.Update()/BasketAdd only ever ADD to the existing quantity,
        /// so the stepper needs this to set qty directly.
        /// </summary>
        [HttpPost]
        public JsonResult BasketUpdateQty(string productref, int qty)
        {
            SaveReturn sr = new SaveReturn();
            model = new ProductViewModel();

            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            if (qty <= 0)
            {
                sr = Basket.Delete(productref);
            }
            else
            {
                sr = Basket.UpdateQty(productref, qty);
            }

            bool productHasOffer = model.IsCompatibleSaleActive
                || (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]) && Convert.ToBoolean(Session["U_IsFromPPC"]) && ConfigurationManager.AppSettings["PPCPromoAppl"] != "OEM");

            // basketSummary feeds the mini-cart widget (#minibasket-widget in MiniBasket.cshtml)
            // via site.js's $('#minibasket-widget').replaceWith(...) - render that view here
            // instead of the retired BasketSummary.cshtml. sr.Html keeps its own separate
            // BasketDetails.cshtml render below for the full basket page.
            string basketSummary = RenderPartialViewToString("~/Views/Shared/MiniBasket.cshtml", model);
            sr.Html = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);

            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            var i = lbc.FindIndex(x => x.StockRef == productref);
            BasketContents bc = i >= 0 ? lbc[i] : new BasketContents();

            ViewData.Add("Section", "Info");
            ViewData.Add("ProductHasOffer", productHasOffer);
            string infoMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", bc);
            ViewData.Clear();
            ViewData.Add("Section", "Price");
            ViewData.Add("ProductHasOffer", productHasOffer);
            ViewData.Add("PriceEx", bc.PriceEx);
            ViewData.Add("PriceInc", bc.PriceInc);
            string priceMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", bc);

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];

            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = basketSummary,
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : (bt.TotalExcVat - bt.Delivery).ToString("#,###,##0.00"),
                productInfoMessage = infoMessage,
                productPriceMessage = priceMessage
            });
        }

        public JsonResult BasketDelete(string productref)
        {
            SaveReturn sr = new SaveReturn();
            model = new ProductViewModel();

            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            var i = lbc.FindIndex(x => x.StockRef == productref);
            BasketContents bc = new BasketContents();
            if (i >= 0)
            {
                bc = lbc[i];
                bc.Quantity = 0;
            }
            sr = Basket.Delete(productref);

            bool productHasOffer = model.IsCompatibleSaleActive
                || (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]) && Convert.ToBoolean(Session["U_IsFromPPC"]) && ConfigurationManager.AppSettings["PPCPromoAppl"] != "OEM");

            // basketSummary feeds the mini-cart widget (#minibasket-widget in MiniBasket.cshtml)
            // via site.js's $('#minibasket-widget').replaceWith(...) - render that view here
            // instead of the retired BasketSummary.cshtml. sr.Html keeps its own separate
            // BasketDetails.cshtml render below for the full basket page.
            string basketSummary = RenderPartialViewToString("~/Views/Shared/MiniBasket.cshtml", model);
            sr.Html = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);
            ViewData.Add("Section", "Info");
            ViewData.Add("ProductHasOffer", productHasOffer);
            string infoMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", bc);
            ViewData.Clear();
            ViewData.Add("Section", "Price");
            ViewData.Add("ProductHasOffer", productHasOffer);
            ViewData.Add("PriceEx", bc.PriceEx);
            ViewData.Add("PriceInc", bc.PriceInc);
            string priceMessage = RenderPartialViewToString("~/Views/Product/InfoMessage.cshtml", bc);

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];

            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = basketSummary,
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : bt.TotalExcVat.ToString("#,###,##0.00"),
                productInfoMessage = infoMessage,
                productPriceMessage = priceMessage
            });
        }

        [HttpPost]
        public JsonResult BasketReplace(string productref, int? productqty, decimal productprice, string productrefremove, int? type = 0, int? lineUid = 0)
        {
            SaveReturn sr = new SaveReturn();
            // FIX: was missing - every other basket-mutating action in this controller
            // (BasketUpdateQty, BasketDelete) sets this before rendering MiniBasket.cshtml below,
            // because that partial dereferences Model.IsMobile/Model.VatMultiplier directly. Left
            // null here, RenderPartialViewToString(..., model) throws a NullReferenceException
            // AFTER Basket.Update()/Basket.Delete() below have already committed the switch to
            // Session - so the switch genuinely happened (a manual page refresh shows it), but the
            // AJAX response itself failed, so the click handler's success callback (the one that
            // updates the mini-cart/basket-page DOM) never ran. Root cause of "Switch Now does
            // nothing until I refresh manually" (top banner, line items, and mini-cart all go
            // through this one action or its BasketReplaceAll sibling below, which had the same
            // bug).
            model = new ProductViewModel();

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
                bc.QtyStart = bc.Quantity;
                bc.PriceEx = productprice;
                bc.ItemType = BasketItemType.Item;
                if (bc.PriceEx != 0)
                {
                    bc.PriceInc = productprice *
                                  Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());
                }
                bc.Type = type ?? 0;
                bc.LineUid = lineUid ?? 0;

                sr = Basket.Update(bc);
                if (sr.IsSuccess)
                {
                    sr = Basket.Delete(productrefremove);
                }
            }

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            // basketSummary feeds the mini-cart widget (#minibasket-widget in MiniBasket.cshtml)
            // via site.js's $('#minibasket-widget').replaceWith(...) - render that view here
            // instead of the retired BasketSummary.cshtml.
            string basketSummary = RenderPartialViewToString("~/Views/Shared/MiniBasket.cshtml", model);

            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = basketSummary,
                basketQuantity = bt.Quantity.ToString("##0"),
                //basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : bt.TotalExcVat.ToString("#,###,##0.00"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1" ? bt.TotalIncVat.ToString("#,###,##0.00") : (bt.TotalExcVat - bt.Delivery).ToString("#,###,##0.00")
            });
        }

        [HttpPost]
        public ActionResult Comparison(string productsToCompare)
        {
            var model = new ProductViewModel();
            model.Compare(productsToCompare);

            return PartialView(model);
        }
        [HttpPost]
        public JsonResult BasketReplaceAll()
        {
            SaveReturn sr = new SaveReturn();
            // FIX: same missing initialization as BasketReplace above - see that comment. Needed
            // here for the same reason: MiniBasket.cshtml is rendered below with this null model,
            // which throws when it reads Model.IsMobile.
            model = new ProductViewModel();

            // Prevent changes during checkout
            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            // Current basket
            List<BasketContents> basket =
                Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            if (basket == null || basket.Count == 0)
            {
                sr.IsSuccess = false;
                sr.Message = "Basket is empty.";

                return Json(new
                {
                    savereturn = sr
                });
            }

            // Get only the products that can actually be switched
            List<BasketContents> switchProducts = basket
                .Where(x =>
                    x.ItemType == BasketItemType.Item &&
                    !x.IsCompatible &&
                    !x.ExcludeFromUpSell &&
                    !string.IsNullOrEmpty(x.CrossSellingStockRef) &&
                    (x.CrossSellingAvailability == 1 || x.CrossSellingAvailability == 7) &&
                    x.CrossSellingPriceEx < x.PriceEx)
                .ToList();

            foreach (BasketContents item in switchProducts)
            {
                BasketContents bc = new BasketContents();

                bc.StockRef = item.CrossSellingStockRef;
                bc.Quantity = item.Quantity;
                bc.QtyStart = item.Quantity;
                bc.PriceEx = 0;          // Use live website price
                bc.PriceInc = 0;
                bc.ItemType = BasketItemType.Item;
                bc.Type = item.Type;
                bc.LineUid = item.LineUid;

                // Add replacement product
                sr = Basket.Update(bc);

                // Remove original product
                if (sr.IsSuccess)
                {
                    Basket.Delete(item.StockRef);
                }
                else
                {
                    break;
                }
            }

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            // basketSummary feeds the mini-cart widget (#minibasket-widget in MiniBasket.cshtml)
            // via site.js's $('#minibasket-widget').replaceWith(...) - render that view here
            // instead of the retired BasketSummary.cshtml.
            string basketSummary =
                RenderPartialViewToString(
                    "~/Views/Shared/MiniBasket.cshtml",
                    model);

            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketSummary = basketSummary,
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1"
                    ? bt.TotalIncVat.ToString("#,###,##0.00")
                    : (bt.TotalExcVat - bt.Delivery).ToString("#,###,##0.00")
            });
        }
    }
}
