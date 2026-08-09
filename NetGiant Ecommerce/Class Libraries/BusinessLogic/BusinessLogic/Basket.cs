using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using VMerchantWrapper.Entities;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;

namespace BusinessLogic
{
    public class Basket
    {
        /// <summary>
        /// Load the basket Cookie into the session
        /// </summary>
        public static void LoadCookie()
        {
            string basket = HttpContext.Current.Request.Cookies["basket"] == null
                ? ""
                : HttpContext.Current.Request.Cookies["basket"].Value;
            //if (basket != "")
            //{
            InitialiseSession(basket);
            //}            
        }

        public static void LoadCookie(string basketString)
        {
            InitialiseSession(basketString);
        }

        /// <summary>
        /// Initialise Session Variables
        /// </summary>
        /// <param name="basketCookie"></param>
        private static void InitialiseSession(string basketCookie)
        {
            List<BasketContents> lbc = new List<BasketContents>();
            if (basketCookie != "")
            {
                List<string> ba = new List<string>();
                List<string> dela = new List<string>();

                ba = basketCookie.Split(new string[] { "|" }, StringSplitOptions.None).ToList();
                if (ba.Last().Contains(":"))
                {
                    dela = ba.Last().Split(new string[] { "::" }, StringSplitOptions.None).ToList();
                }

                if (ba.Count > 8)
                {
                    //There are basket contents
                    int i = ba.Count / 10;
                    for (int j = 0; j < i; j++)
                    {
                        BasketContents bc = new BasketContents();
                        try
                        {
                            bc.StockRef = ba[j * 10];
                            bc.Quantity = int.Parse(ba[(j * 10) + 1]);
                            bc.Type = int.Parse(ba[(j * 10) + 4]);
                            bc.LineUid = int.Parse(ba[(j * 10) + 5]);
                            bc.ItemType = BasketItemType.Item;

                            lbc.Add(bc);
                        }
                        catch
                        {
                            //ignore this element and move on to the next
                        }
                    }
                    //Update Summary/Prices in the lbc
                    lbc = ExpandBasketContents(lbc,
                        HttpContext.Current.Session["U_AccountNo"] != null
                            ? HttpContext.Current.Session["U_AccountNo"].ToString()
                            : " ",
                        false);

                    HttpContext.Current.Session["B_BasketArray"] = lbc;
                }
                if (dela.Count > 3)
                {
                    //There is delivery data
                    try
                    {
                        HttpContext.Current.Session["D_DeliveryCountry"] = int.Parse(dela[1]);
                        HttpContext.Current.Session["D_DeliveryZone"] = int.Parse(dela[2]);
                    }
                    catch
                    {
                        //ignore this element
                    }
                }

                UpdateCookie(new SaveReturn());
                HttpContext.Current.Session["B_Basket"] = basketCookie;
            }
            else
            {
                //No (valid) basket Cookie
                HttpContext.Current.Session.Remove("B_Basket");
            }

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"]))
            {
                ApplyCompatibleDiscount();
            }

            UpdateBasketSession(lbc);

            if (((BasketTotals)HttpContext.Current.Session["B_BasketTotals"]).Delivery == 0)
            {
                GetBallparkDelivery();
            }
        }

        public static SaveReturn GetBallparkDelivery()
        {
            BasketTotals bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            bool isCompatibleInk = Convert.ToBoolean(HttpContext.Current.Session["B_CompatibleInkOnly"]);

            deliveryService ds = DataCache.GetDeliveryService()
                .OrderBy(x => x.Price)
                .FirstOrDefault(x =>
                (x.DeliveryMethod == 1 || x.DeliveryMethod == 19) &&
                (!x.IsCompatibleInkOnly || (isCompatibleInk && x.IsCompatibleInkOnly)) && (
                    (x.ThresholdStart <= bt.TotalExcVat && x.ThresholdEnd >= bt.TotalExcVat) ||
                    (x.ThresholdStart == null && x.ThresholdEnd == null)
                ));

            decimal price = ds.Price;
            if (Convert.ToBoolean(HttpContext.Current.Session["B_IsBulky"]))
            {
                price = 40m;
            }
            //if (Convert.ToBoolean(HttpContext.Current.Session["B_CompatibleInkOnly"]))
            //{
            //    price = 0;
            //}
            if (bt.Quantity == 0)
            {
                price = 0;
            }

            BasketContents bc = new BasketContents
            {
                //IsDelivery = true,
                Quantity = 1,
                Description = ds.ServiceName,
                StockRef = ds.StockRef,
                Type = 0,
                LineUid = 0,
                PriceInc = Math.Round(price * Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]), 2),
                PriceEx = price,
                Availability = 1,
                ImageUrl = "",
                ProductUrl = "",
                IsCompatibleInk = false,
                IsBulky = false,
                IsSpecialOrder = false,
                DeliveryMethod = ds.DeliveryMethod,
                ItemType = BasketItemType.Delivery
            };

            return Update(bc);
        }

        public static void ResetBasket()
        {
            HttpContext.Current.Session.Remove("B_Basket");
            HttpContext.Current.Session["B_BasketTotals"] = new BasketTotals();
            HttpContext.Current.Session.Remove("B_BasketArray");
            HttpContext.Current.Session.Remove("B_VoucherCode");
            HttpContext.Current.Session.Remove("B_CompatibleInkOnly");
            HttpContext.Current.Session.Remove("B_IsBulky");

            HttpCookie basket = new HttpCookie("basket")
            {
                Expires = DateTime.Now.AddDays(-1),
                SameSite = SameSiteMode.Lax,
                Secure = true
            };
            HttpContext.Current.Response.Cookies.Add(basket);
        }

        /// <summary>
        /// Add or Update an items in the basket
        /// </summary>
        /// <param name="basketContents"></param>
        /// <returns></returns>
        public static SaveReturn Update(BasketContents basketContents)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                var i = lbc.FindIndex(x => x.StockRef == basketContents.StockRef);
                bool bypassSql = false;
                //if basket already contains item
                if (i >= 0)
                {
                    //product exists update the item
                    //Can't add more than 1000 of an item
                    if (lbc[i].Quantity + basketContents.Quantity > 1000)
                    {
                        sr.IsSuccess = false;
                        sr.Message = "You cannot add more than a 1000 items to your basket, please contact Customer Support for further assistance.";
                        return sr;
                    }
                    lbc[i].Quantity = lbc[i].Quantity + basketContents.Quantity;
                    bypassSql = false;  // Potentailly set to true as product should have already been extended
                }
                else
                {
                    //new item add it to the Session object
                    lbc.Add(basketContents);
                }

                //Update Summary/Prices in the lbc
                if (basketContents.ItemType == BasketItemType.Item)
                {
                    lbc = ExpandBasketContents(lbc,
                        HttpContext.Current.Session["U_AccountNo"] != null
                            ? HttpContext.Current.Session["U_AccountNo"].ToString()
                            : " "
                        , bypassSql);
                }
                HttpContext.Current.Session["B_BasketArray"] = lbc;

                UpdateBasketSession(lbc);

                BasketContents lbcEntry = lbc.FirstOrDefault(x => x.StockRef == basketContents.StockRef);
                if (lbcEntry != null && (lbcEntry.ItemType == BasketItemType.Item || lbcEntry.ItemType == BasketItemType.AdminDiscount) && !lbcEntry.IsFreeGift)
                //if (lbcEntry != null && lbcEntry.ItemType == BasketItemType.Item && !lbcEntry.IsFreeGift)
                {
                    RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                    if (HttpContext.Current.Session["B_VoucherCode"] != null)
                    {
                        ApplyVoucher();
                    }
                    else
                    {
                        if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"]))
                        {
                            ApplyCompatibleDiscount();
                        }
                    }
                }

                Basket.UpdateCookie(sr);
                Task t = Touchpoints.MailChimpUpdateCartAsync();
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
                sr.IsSuccess = false;
                Utilities.ProcessException(e);
            }

            return sr;
        }

        public static SaveReturn UpdateQty(string stockref, int qty)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                var i = lbc.FindIndex(x => x.StockRef == stockref);
                //if basket already contains item
                if (i >= 0)
                {
                    // Is the basket quantity greater than the quantity when they launched viewbasket?
                    //if (qty > lbc[i].QtyStart && lbc[i].IsCompatible && Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"]))
                    //{
                    //    lbc[i].IsUpsellTriggered = true;
                    //}
                    lbc[i].Quantity = qty;
                }
                else
                {
                    //error
                    sr.Message = "The item is not in the basket";
                    sr.IsSuccess = false;
                }

                HttpContext.Current.Session["B_BasketArray"] = lbc;

                UpdateBasketSession(lbc);

                RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                if (HttpContext.Current.Session["B_VoucherCode"] != null)
                {
                    sr.Message = ApplyVoucher();
                    if (sr.Message != "")
                    {
                        sr.Html = "<div class=\"g-fc-nm\"><i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">" + sr.Message + "</span></div>";
                    }
                }
                else
                {
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"])
                        && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                    {
                        ApplyCompatibleDiscount();
                    }
                }

                Basket.UpdateCookie(sr);
                Task t = Touchpoints.MailChimpUpdateCartAsync();
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
                sr.IsSuccess = false;
            }

            return sr;
        }

        /// <summary>
        /// Delete an item from the basket
        /// </summary>
        /// <param name="stockref"></param>
        /// <returns></returns>
        public static SaveReturn Delete(string stockref)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                var i = lbc.FindIndex(x => x.StockRef == stockref);
                //find the item to delete
                if (i >= 0)
                {
                    lbc.Remove(lbc[i]);
                }

                HttpContext.Current.Session["B_BasketArray"] = lbc;

                UpdateBasketSession(lbc);

                RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                if (HttpContext.Current.Session["B_VoucherCode"] != null)
                {
                    ApplyVoucher();
                }
                else
                {
                    if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"])
                        && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                    {
                        ApplyCompatibleDiscount();
                    }
                }

                Basket.UpdateCookie(sr);
                Task t = Touchpoints.MailChimpUpdateCartAsync();
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
                sr.IsSuccess = false;
            }

            return sr;
        }

        private static SaveReturn UpdateCookie(SaveReturn sr)
        {
            sr.IsSuccess = true;
            if (!Convert.ToBoolean(HttpContext.Current.Session["U_IsCustServices"]))
            {
                string basketCookie = "";
                // Take Session object and convert it to a basket cookie
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

                foreach (BasketContents bc in lbc)
                {
                    if (bc.ItemType == BasketItemType.Item && !bc.IsFreeGift)
                    {
                        basketCookie += bc.StockRef + "|" + bc.Quantity + "|||" + bc.Type + "|" + bc.LineUid +
                                        "||||False|";
                    }
                }
                basketCookie += "::1::1::";

                //Update Cookie
                DateTime expiry = System.DateTime.Now.Add(new System.TimeSpan(365, 0, 0, 0));

                HttpCookie basket = new HttpCookie("basket")
                {
                    Value = basketCookie,
                    Expires = expiry,
                    SameSite = SameSiteMode.Lax,
                    Secure = true
                };
                HttpContext.Current.Response.Cookies.Add(basket);
            }

            return sr;
        }

        /// <summary>
        /// Update the Basket Session variables for Total and Quantity
        /// </summary>
        /// <param name="lbc"></param>
        /// <returns></returns>
        public static void UpdateBasketSession(List<BasketContents> lbc)
        {
            bool compatibleInkOnly = true;
            bool specialOrderOnly = true;
            bool isbulky = false;

            bool isVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null && Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);

            decimal basketTotal = 0;
            int basketQty = 0;

            foreach (BasketContents bc in lbc)
            {
                int mult = (bc.ItemType == BasketItemType.CompatibleDiscount || bc.ItemType == BasketItemType.AdminDiscount) ? -1 : 1;

                if (bc.ItemType != BasketItemType.Voucher)
                {
                    //basketTotal += Math.Round(entry.PriceInc / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]), 2) * entry.Quantity * mult;
                    basketTotal += bc.PriceEx * bc.Quantity * mult;
                }
                if (bc.ItemType == BasketItemType.Item || bc.ItemType == BasketItemType.AdminDiscount)
                {
                    basketQty += bc.Quantity;
                }
                if (!bc.IsCompatibleInk && (bc.ItemType == BasketItemType.Item || bc.ItemType == BasketItemType.AdminDiscount))
                {
                    compatibleInkOnly = false;
                }
                if (!bc.IsSpecialOrder && (bc.ItemType == BasketItemType.Item || bc.ItemType == BasketItemType.AdminDiscount))
                {
                    specialOrderOnly = false;
                }
                if (bc.IsBulky)
                {
                    isbulky = true;
                }
            }

            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");

            var deliveryPrice = lbc.FirstOrDefault(x => x.ItemType == BasketItemType.Delivery);
            if (deliveryPrice != null)
                bt.Delivery = deliveryPrice.PriceEx;

            bt.Quantity = basketQty;
            bt.TotalExcVat = basketTotal;
            bt.GrandTotalExcVat = lbc.Sum(x => (x.ItemType == BasketItemType.AdminDiscount || x.ItemType == BasketItemType.CompatibleDiscount)
                ? Math.Round(x.PriceEx, 2) * -1
                : Math.Round(x.PriceEx, 2) * x.Quantity);
            if (isVatExempt)
            {
                bt.Vat = 0;
                bt.TotalIncVat = bt.TotalExcVat;
                bt.GrandTotalIncVat = bt.GrandTotalExcVat;
            }
            else
            {
                bt.Vat = lbc.Sum(x => (x.ItemType == BasketItemType.AdminDiscount || x.ItemType == BasketItemType.CompatibleDiscount)
                    //? Math.Round((x.PriceInc - x.PriceEx), 2) * -1
                    ? Math.Round(((Math.Floor(x.PriceInc * 100) / 100) - Math.Round(x.PriceEx, 2)), 2) * -1
                    : Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100);
                decimal vatDiscount = lbc.Sum(x => (x.ItemType == BasketItemType.AdminDiscount || x.ItemType == BasketItemType.CompatibleDiscount)
                    ? Math.Floor((x.PriceInc - x.PriceEx) * 100) / 100
                    : 0);
                decimal vatItem = lbc.Sum(x => x.ItemType == BasketItemType.Item
                    ? Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100
                    : 0);
                bt.TotalIncVat = bt.TotalExcVat + vatItem - vatDiscount;
                bt.GrandTotalIncVat = bt.GrandTotalExcVat + bt.Vat;
            }

            HttpContext.Current.Session["B_BasketTotals"] = bt;
            HttpContext.Current.Session["B_CompatibleInkOnly"] = compatibleInkOnly;
            HttpContext.Current.Session["B_SpecialOrderOnly"] = specialOrderOnly;
            HttpContext.Current.Session["B_IsBulky"] = isbulky;
        }

        public static string ApplyVoucher()
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            VoucherPromo v = Utilities.LoadSession<VoucherPromo>("V_Voucher");

            if (HttpContext.Current.Session["B_VoucherCode"] == null)
            {
                //lbc.RemoveAll(x => x.IsVoucher || x.IsFreeGift);
                // Decided on using a loop as we want to also set a property
                for (int i = lbc.Count - 1; i >= 0; i--)
                {
                    lbc[i].IsVoucherQualifyingItem = false;
                    if (lbc[i].IsFreeGift || lbc[i].ItemType == BasketItemType.Voucher)
                    {
                        Delete(lbc[i].StockRef);
                    }
                }

                bt.Voucher = 0;
                bt.VoucherVat = 0;
                bt.Vat = lbc.Sum(x => (x.ItemType == BasketItemType.AdminDiscount || x.ItemType == BasketItemType.CompatibleDiscount)
                    ? Math.Round((x.PriceInc - x.PriceEx), 2) * -1
                    : Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100);
                bt.GrandTotalExcVat = lbc.Sum(x => (x.ItemType == BasketItemType.AdminDiscount || x.ItemType == BasketItemType.CompatibleDiscount)
                    ? Math.Round(x.PriceEx, 2) * -1
                    : Math.Round(x.PriceEx, 2) * x.Quantity);
                bt.GrandTotalIncVat = bt.GrandTotalExcVat + bt.Vat;

                HttpContext.Current.Session["B_BasketTotals"] = bt;

                // Update basket totals/summary
                UpdateBasketSession(lbc);
                //ExpandBasketContents(lbc, "", true);

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"]))
                {
                    ApplyCompatibleDiscount();
                }

                return "";
            }

            decimal basketTotal = bt.TotalIncVat;
            decimal basketQualValueInc = 0;
            decimal basketQualValueEx = 0;
            int qualifyingItems = 0;
            int noToDiscount = 0;
            List<BasketContents> qualList = new List<BasketContents>();

            foreach (BasketContents bc in lbc)
            {
                bc.IsVoucherQualifyingItem = true;
                if (!v.IsGlobal)
                {
                    if (v.Categories.Count > 0)
                    {
                        // Check Primary Category
                        bool pCFound = v.Categories.IndexOf(bc.CategoryNo) != -1;
                        // Check Secondary Categories
                        List<secondaryCategoryLookup> lscl =
                            EntityAccess.ReadSecondaryCategoryLookup(x => x.websiteInventory.productFK == bc.ProductId);
                        bool sCFound = false;
                        foreach (secondaryCategoryLookup scl in lscl)
                        {
                            sCFound = sCFound || v.Categories.IndexOf(scl.categoryCodeFK ?? 0) != -1;
                        }
                        bc.IsVoucherQualifyingItem = pCFound || sCFound;
                    }
                    else
                    {
                        bc.IsVoucherQualifyingItem = v.Groups.IndexOf(bc.GroupNo) != -1;
                    }
                }
                if (bc.IsVoucherQualifyingItem)
                {
                    basketQualValueInc += bc.PriceInc * bc.Quantity;
                    basketQualValueEx += Math.Round(Math.Round(bc.PriceInc, 2) / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) * bc.Quantity, 2);
                }
            }

            HttpContext.Current.Session["B_BasketArray"] = lbc;

            if (basketTotal >= v.MinBasketValue && basketQualValueInc >= v.MinQualValue)
            {
                // Apply discount to voucher items
                decimal voucherValue = 0;
                if (v.VoucherTypeFk == (int)VmVoucherType.Amount)
                {
                    decimal va = v.Amount ?? default(decimal);
                    voucherValue = va > basketQualValueInc ? basketQualValueInc : va;
                    if (string.IsNullOrEmpty(v.AccountNumber))
                    {
                        voucherValue = va > basketQualValueInc ? basketQualValueInc : va;
                    }
                }
                if (v.VoucherTypeFk == (int)VmVoucherType.Percentage)
                {
                    voucherValue = Math.Round(basketQualValueInc * (v.Percentage ?? default(decimal)) / 100, 2);
                }
                if (v.VoucherTypeFk == (int)VmVoucherType.FreeGift)
                {
                    if (lbc.Count(x => x.StockRef == v.StockRef) == 0)
                    {
                        // Delete the free gift if it's already in there
                        if (lbc.Count(x => x.StockRef == v.GiftStockRef) > 0)
                        {
                            //find the item to delete
                            var i = lbc.FindIndex(x => x.StockRef == v.GiftStockRef);
                            if (i >= 0)
                            {
                                lbc.Remove(lbc[i]);
                            }
                        }

                        // Add the free gift to the basket
                        BasketContents bc = new BasketContents
                        {
                            ItemType = BasketItemType.Item,
                            IsFreeGift = true,
                            StockRef = v.GiftStockRef,
                            Quantity = 1,
                            PriceEx = 0,
                            PriceInc = 0,
                            Type = 0,
                            LineUid = 0
                        };

                        SaveReturn sr = Basket.Update(bc);
                    }
                }

                if (v.VoucherTypeFk == (int)VmVoucherType.MultiBuy)
                {
                    // how many items in the basket qualify
                    foreach (BasketContents bc in lbc)
                    {
                        if (bc.IsVoucherQualifyingItem)
                        {
                            qualifyingItems += bc.Quantity;
                        }
                    }

                    // how many items should be discounted
                    noToDiscount = (qualifyingItems / (v.MultiBuyQualNo ?? default(int)) * (v.MultiBuyNoDiscounted ?? default(int)));

                    // take the n lowest priced items from the qualifying list
                    qualList = lbc.Where(x => x.IsVoucherQualifyingItem)
                            .OrderBy(x => x.PriceEx)
                            .Take(noToDiscount)
                            .ToList();

                    // work out the discount
                    int quantityRemaining = noToDiscount;
                    foreach (BasketContents bc in qualList)
                    {
                        if (quantityRemaining > 0)
                        {
                            if (bc.Quantity >= quantityRemaining)
                            {
                                bc.VoucherAmount = Math.Round(
                                    Math.Round(bc.PriceInc, 2) * ((v.Percentage ?? default(decimal)) / 100) /
                                    Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) *
                                    quantityRemaining, 2);
                                voucherValue += bc.PriceInc * ((v.Percentage ?? default(decimal)) / 100) * quantityRemaining;
                                quantityRemaining = 0;
                            }
                            else
                            {
                                bc.VoucherAmount = Math.Round(
                                    Math.Round(bc.PriceInc, 2) * ((v.Percentage ?? default(decimal)) / 100) /
                                    Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) *
                                    bc.Quantity, 2);
                                voucherValue += bc.PriceInc * ((v.Percentage ?? default(decimal)) / 100) * bc.Quantity;
                                quantityRemaining = quantityRemaining - bc.Quantity;
                            }
                        }
                    }
                }

                bt.Voucher = voucherValue * -1;
                bt.VoucherVat = voucherValue - Math.Ceiling(voucherValue / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) * 100) / 100;
                bool isVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null && Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);
                if (isVatExempt)
                {
                    bt.Voucher = (voucherValue - bt.VoucherVat) * -1;
                    bt.VoucherVat = 0;
                }

                // add voucher to basket array
                CheckoutViewModel.AddVoucherToBasket();
            }
            else
            {
                bt.Voucher = 0;
                if (v.VoucherTypeFk == (int)VmVoucherType.FreeGift)
                {
                    if (lbc.Count(x => x.StockRef == v.GiftStockRef) > 0)
                    {
                        Delete(v.GiftStockRef);
                    }
                }
            }

            // Update basket totals/summary
            UpdateBasketSession(lbc);

            // Voucher is valid, but is there an explanation as to why the voucher discount amount isn't as expected?
            string zeroAmountExplanation = "";
            if (bt.Voucher != 0)
            {
                // Apportion the voucher between the products bought
                if (v.VoucherTypeFk == (int)VmVoucherType.Percentage)
                {
                    // The amount is apportioned according to percentage
                    foreach (BasketContents bc in lbc)
                    {
                        if (bc.IsVoucherQualifyingItem)
                        {
                            bc.VoucherAmount = Math.Round(bc.PriceEx * bc.Quantity * (v.Percentage ?? default(decimal)) / 100, 2);
                        }
                    }
                }
                if (v.VoucherTypeFk == (int)VmVoucherType.MultiBuy)
                {
                    // Use qualList to update lbc
                    foreach (BasketContents bc in qualList)
                    {
                        lbc.Find(x => x.StockRef == bc.StockRef).VoucherAmount = bc.VoucherAmount;
                    }
                }
            }
            else
            {
                // Voucher is valid, but is there an explanation as to why the voucher discount amount isn't as expected?
                if (basketQualValueInc < v.MinQualValue)
                {
                    decimal amt = (v.MinQualValue - basketQualValueInc) /
                                  Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());
                    zeroAmountExplanation = "Please spend &pound;" + (amt).ToString("N2") + " (ex VAT) more to meet the minimum order value on qualifying items.";
                }
                if (noToDiscount == 0 && v.VoucherTypeFk == (int)VmVoucherType.MultiBuy)
                {
                    zeroAmountExplanation = "Qualifying product quantity not met.";
                }
                if (basketQualValueInc == 0)
                {
                    zeroAmountExplanation = "Voucher doesn't apply to the items in your basket.";
                }
                if (v.Amount > basketQualValueInc)
                {
                    zeroAmountExplanation = "Voucher amount exceeds basket total.";
                }
            }

            return zeroAmountExplanation;
        }

        public static decimal GetBasketTotal(List<BasketContents> lbc)
        {
            bool isVATExempt = Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);
            decimal itemTotal = 0;
            foreach (BasketContents bc in lbc)
            {
                if (bc.ItemType != BasketItemType.Delivery)
                {
                    int mult = (bc.ItemType == BasketItemType.CompatibleDiscount || bc.ItemType == BasketItemType.AdminDiscount) ? -1 : 1;
                    if (isVATExempt)
                    {
                        itemTotal += bc.PriceEx * bc.Quantity * mult;
                    }
                    else
                    {
                        itemTotal += bc.PriceInc * bc.Quantity * mult;
                    }
                }
            }
            return Math.Round(itemTotal, 2);
        }

        private static void ApplyCompatibleDiscount()
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            if (!lbc.Exists(x => x.ItemType == BasketItemType.AdminDiscount))
            {

                decimal discountToApply = decimal.Zero;
                foreach (BasketContents bc in lbc)
                {
                    //if (bc.IsCompatible && bc.IsUpsellTriggered)
                    if (bc.IsCompatible && bc.QtyStart > 0 && bc.Quantity > bc.QtyStart)
                    {
                        decimal discountRate = Utilities.GetUpsellRate();
                        decimal disc = Math.Round(bc.PriceEx * discountRate / 100, 2) * bc.Quantity;
                        discountToApply += disc;
                        if (disc > 0)
                        {
                            bc.VoucherAmount = disc;
                            bc.IsVoucherQualifyingItem = false;    // Do NOT set to true for MULTIBUY discount
                        }
                    }
                }
                if (discountToApply > 0)
                {
                    SaveReturn sr = Update(new BasketContents
                    {
                        StockRef = "MULTIBUY",
                        PartNo = "MULTIBUY",
                        Quantity = 1,
                        PriceEx = discountToApply,
                        PriceInc = discountToApply *
                                   Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString()),
                        ItemType = BasketItemType.CompatibleDiscount,
                        Availability = 1,
                        ImageUrl = ConfigurationManager.AppSettings["CDN"] + "/Images/noImage.jpg",
                        Description = "MultiBuy Discount",
                        Type = 0,
                        LineUid = 0
                    });


                }
            }
            // Update basket totals
            UpdateBasketSession(lbc);
        }

        public static void RemoveFromBasket(System.Predicate<BasketContents> where)
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            var i = lbc.FindIndex(where);
            if (i >= 0)
            {
                lbc.RemoveAll(where);
                UpdateBasketSession(lbc);
            }

            HttpContext.Current.Session["B_BasketArray"] = lbc;
        }

        /// <summary>
        /// Updates prices in the Basket Array from the database
        /// </summary>
        /// <param name="lbc"></param>
        /// <param name="account"></param>
        /// <param name="newItemsOnly"></param>
        /// <returns></returns>
        public static List<BasketContents> ExpandBasketContents(List<BasketContents> lbc, string account, bool bypassSql = false)
        {
            string refArray = Utilities.GetStockRefArray(lbc);
            bool isVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null && Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);
            StringBuilder summary = new StringBuilder();

            if (!bypassSql)
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ProductIDArray", SqlDbType.VarChar);
                sqlParm.Value = refArray;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
                sqlParm.Value = account;
                sqlParms.Add(sqlParm);
                DataTable dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSummaryData1", sqlParms,
                    "summarydata").Tables[0];

                foreach (DataRow dr in dt.Rows)
                {
                    string stockRef = "";
                    try
                    {
                        stockRef = "";
                        stockRef = dr["stockReference"].ToString();
                        string partNo = dr["partNo"].ToString();
                        int productId = int.Parse(dr["productID"].ToString());
                        string categoryNo = dr["CategoryNo"].ToString();
                        string groupNo = dr["GroupNo"].ToString();
                        string desc = dr["Description"].ToString();
                        string url = dr["ProductURL"].ToString();
                        string imageUrl = dr["ImageURL"].ToString();
                        int brandflag = int.Parse(dr["BrandFlag"].ToString());
                        string spec6 = dr["SpecLine6"].ToString();
                        int availability = int.Parse(dr["Availability"].ToString());

                        BasketContents lbcEntry = lbc.Find(x => x.StockRef == stockRef);

                        if (dr["tradePriceExVat"] is DBNull)
                        {
                            lbc.Remove(lbcEntry);
                            continue;
                        }

                        lbcEntry.IsCompatible = false;
                        if (brandflag == 2)
                        {
                            lbcEntry.IsCompatible = true;
                        }
                        lbcEntry.IsCompatibleInk = false;
                        if (brandflag == 2 && spec6 == "Ink")
                        {
                            lbcEntry.IsCompatibleInk = true;
                        }
                        lbcEntry.IsBulky = false;
                        if (spec6 == "Bulky")
                        {
                            lbcEntry.IsBulky = true;
                        }
                        lbcEntry.IsSpecialOrder = false;
                        if (availability == 10)
                        {
                            lbcEntry.IsSpecialOrder = true;
                        }
                        lbcEntry.ProductId = productId;
                        lbcEntry.PartNo = partNo;
                        lbcEntry.GroupNo = int.Parse(groupNo);
                        lbcEntry.CategoryNo = int.Parse(categoryNo);

                        lbcEntry.ProductUrl = url;
                        lbcEntry.ImageUrl = imageUrl;
                        lbcEntry.Description = desc;
                        if (!lbcEntry.IsFreeGift)
                        {
                            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                            {
                                lbcEntry.PriceEx =
                                    Math.Ceiling(Convert.ToDecimal(dr["tradePriceExVat"]) * 100) / 100;
                                lbcEntry.PriceInc = Convert.ToDecimal(dr["tradePriceIncVat"]);
                            }
                            else
                            {
                                lbcEntry.PriceEx =
                                    Math.Ceiling(Convert.ToDecimal(dr["retailPriceExVat"]) * 100) / 100;
                                lbcEntry.PriceInc = Convert.ToDecimal(dr["retailPriceIncVat"]);
                            }
                        }
                        else
                        {
                            lbcEntry.Description = desc;
                        }
                        lbcEntry.CrossSellingStockRef = dt.Columns.Contains("CrossSellingStockRef") && dr["CrossSellingStockRef"] != DBNull.Value
                            ? dr["CrossSellingStockRef"].ToString()
                            : "";
                        lbcEntry.CrossSellingPriceEx = dt.Columns.Contains("CrossSellingPriceEx") && dr["CrossSellingPriceEx"] != DBNull.Value
                            ? Convert.ToDecimal(dr["CrossSellingPriceEx"])
                            : 0;
                        lbcEntry.CrossSellingAvailability = dt.Columns.Contains("CrossSellingAvailability") && dr["CrossSellingAvailability"] != DBNull.Value
                            ? int.Parse(dr["CrossSellingAvailability"].ToString())
                            : 0;
                        lbcEntry.CrossSellingDescription = dt.Columns.Contains("CrossSellingDescription") && dr["CrossSellingDescription"] != DBNull.Value
                            ? dr["CrossSellingDescription"].ToString()
                            : "";
                        lbcEntry.CrossSellingImageURL = dt.Columns.Contains("CrossSellingImageURL") && dr["CrossSellingImageURL"] != DBNull.Value
                            ? dr["CrossSellingImageURL"].ToString()
                            : "";
                        lbcEntry.CrossSellingProductUrl = dt.Columns.Contains("CrossSellingProductURL") && dr["CrossSellingProductURL"] != DBNull.Value
                            ? dr["CrossSellingProductURL"].ToString()
                            : "";
                        lbcEntry.ExcludeFromUpSell = dt.Columns.Contains("ManufacturerName") && dr["ManufacturerName"].ToString() == "HP";
                        lbcEntry.IsVatExempt = isVatExempt;
                    }
                    catch (Exception e)
                    {
                        Utilities.ProcessException(e);
                    }
                }
            }

            // Remove items from lbc with 0 price (discontinued items)
            lbc.RemoveAll(x => x.PriceEx == 0 && !x.IsFreeGift && (x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.AdminDiscount));

            foreach (BasketContents bc in lbc)
            {
                if (bc.ItemType != BasketItemType.Item)
                {
                    bc.IsVatExempt = isVatExempt;
                }
            }

            return lbc;
        }

        public static int ProcessDelivery(int serviceId)
        {
            deliveryService ds = DataCache.GetDeliveryService().FirstOrDefault(x => x.DeliveryServiceId == serviceId);

            // Add the delivery to the basket
            if (ds != null)
            {
                // Delete the existing DELIVERY item from the basket
                RemoveDelivery();

                BasketContents bc = new BasketContents
                {
                    //IsDelivery = true,
                    Quantity = 1,
                    Description = ds.ServiceName,
                    StockRef = ds.StockRef,
                    Type = 0,
                    LineUid = 0,
                    PriceInc = Math.Round(ds.Price * Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]), 2),
                    PriceEx = ds.Price,
                    Availability = 1,
                    ImageUrl = "",
                    ProductUrl = "",
                    IsCompatibleInk = false,
                    IsBulky = false,
                    DeliveryMethod = ds.DeliveryMethod,
                    ItemType = BasketItemType.Delivery
                };

                Update(bc);

                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                lbc = ExpandBasketContents(lbc,
                    HttpContext.Current.Session["U_AccountNo"] != null
                        ? HttpContext.Current.Session["U_AccountNo"].ToString()
                        : " ",
                    false);
                HttpContext.Current.Session["B_BasketArray"] = lbc;

                UpdateBasketSession(lbc);
            }

            return ds?.DeliveryMethod ?? 0;
        }

        public static void RemoveDelivery()
        {
            RemoveFromBasket(x => x.ItemType == BasketItemType.Delivery);

            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            bt.Delivery = 0;
            HttpContext.Current.Session["B_BasketTotals"] = bt;
        }
    }
}