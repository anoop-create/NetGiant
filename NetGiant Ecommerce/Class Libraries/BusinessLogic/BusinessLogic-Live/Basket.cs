using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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

                ba = basketCookie.Split(new string[] {"|"}, StringSplitOptions.None).ToList();
                if (ba.Last().Contains(":"))
                {
                    dela = ba.Last().Split(new string[] {"::"}, StringSplitOptions.None).ToList();
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

                            lbc.Add(bc);
                        }
                        catch
                        {
                            //ignore this element and move on to the next
                        }
                    }
                    //Update Summary/Prices in the lbc
                    lbc = UpdateSummary(lbc,
                        HttpContext.Current.Session["U_AccountNo"] != null
                            ? HttpContext.Current.Session["U_AccountNo"].ToString()
                            : " ", false);

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

            UpdateBasketSession(lbc);

            if(((BasketTotals)HttpContext.Current.Session["B_BasketTotals"]).Delivery == 0)
            {
                GetBallparkDelivery();
            }
        }

        public static SaveReturn GetBallparkDelivery()
        {
            BasketTotals bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];

            deliveryService ds = EntityAccess.ReadDeliveryService(x => x.ThresholdStart <= bt.TotalExcVat && x.ThresholdEnd >= bt.TotalExcVat || x.ThresholdStart == null && x.ThresholdEnd == null)
                .FirstOrDefault();

            ds.Price = Convert.ToBoolean(HttpContext.Current.Session["B_IsBulky"]) ? 40m : ds.Price;
            ds.Price = Convert.ToBoolean(HttpContext.Current.Session["B_CompatibleInkOnly"]) ? 0 : ds.Price;

            BasketContents bc = new BasketContents
            {
                IsDelivery = true,
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
                IsSpecialOrder = false,
                DeliveryMethod = ds.DeliveryMethod
            };

            return Update(bc);
        }

        public static void ResetBasket()
        {
            HttpContext.Current.Session.Remove("B_Basket");
            HttpContext.Current.Session["B_BasketTotals"] = new BasketTotals();
            HttpContext.Current.Session.Remove("B_BasketArray");
            HttpContext.Current.Session.Remove("B_BasketSummary");
            HttpContext.Current.Session.Remove("B_VoucherCode");
            HttpContext.Current.Session.Remove("B_CompatibleInkOnly");
            HttpContext.Current.Session.Remove("B_IsBulky");

            HttpCookie basket = new HttpCookie("basket");
            basket.Expires = DateTime.Now.AddDays(-1);
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
                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
                }
                var i = lbc.FindIndex(x => x.StockRef == basketContents.StockRef);
                bool isNewItem = true;
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
                    isNewItem = false;
                }
                else
                {
                    //new item add it to the Session object
                    lbc.Add(basketContents);
                }

                //Update Summary/Prices in the lbc
                lbc = UpdateSummary(lbc,
                    HttpContext.Current.Session["U_AccountNo"] != null
                        ? HttpContext.Current.Session["U_AccountNo"].ToString()
                        : " ", isNewItem);
                HttpContext.Current.Session["B_BasketArray"] = lbc;
                sr.Html = HttpContext.Current.Session["B_BasketSummary"].ToString();

                UpdateBasketSession(lbc);

                BasketContents lbcEntry = lbc.FirstOrDefault(x => x.StockRef == basketContents.StockRef);
                if (lbcEntry != null)
                {
                    if (HttpContext.Current.Session["B_VoucherCode"] != null && !lbcEntry.IsFreeGift)
                    {
                        ApplyVoucher();
                    }
                }

                Basket.UpdateCookie(sr);
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
                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
                }
                var i = lbc.FindIndex(x => x.StockRef == stockref);
                //if basket already contains item
                if (i >= 0)
                {
                    //product exists update the item
                    lbc[i].Quantity = qty;
                }
                else
                {
                    //error
                    sr.Message = "The item is not in the basket";
                    sr.IsSuccess = false;
                }

                //Update Summary/Prices in the lbc
                lbc = UpdateSummary(lbc,
                    HttpContext.Current.Session["U_AccountNo"] != null
                        ? HttpContext.Current.Session["U_AccountNo"].ToString()
                        : " ", false);
                HttpContext.Current.Session["B_BasketArray"] = lbc;
                sr.Html = HttpContext.Current.Session["B_BasketSummary"].ToString();

                UpdateBasketSession(lbc);
                sr.Html = "";
                if (HttpContext.Current.Session["B_VoucherCode"] != null)
                {
                    sr.Message = ApplyVoucher();
                    if (sr.Message != "")
                    {
                        sr.Html ="<div class=\"g-fc-nm\"><i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">" + sr.Message + "</span></div>";
                    }
                }

                Basket.UpdateCookie(sr);
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
        /// <param name="productId"></param>
        /// <returns></returns>
        public static SaveReturn Delete(string stockref)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
                }
                var i = lbc.FindIndex(x => x.StockRef == stockref);
                //find the item to delete
                if (i >= 0)
                {
                    lbc.Remove(lbc[i]);
                }

                //Update Summary/Prices in the lbc
                lbc = UpdateSummary(lbc,
                    HttpContext.Current.Session["U_AccountNo"] != null
                        ? HttpContext.Current.Session["U_AccountNo"].ToString()
                        : " ", true);
                HttpContext.Current.Session["B_BasketArray"] = lbc;
                sr.Html = HttpContext.Current.Session["B_BasketSummary"].ToString();

                UpdateBasketSession(lbc);

                if (!lbc[i].IsDelivery && !lbc[i].IsVoucher && !lbc[i].IsFreeGift && !lbc[i].IsAdminDiscount)
                {
                    if (HttpContext.Current.Session["B_VoucherCode"] != null)
                    {
                        ApplyVoucher();
                    }
                }

                Basket.UpdateCookie(sr);
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
                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>)HttpContext.Current.Session["B_BasketArray"];
                }

                foreach (BasketContents bc in lbc)
                {
                    if (!bc.IsDelivery && !bc.IsVoucher && !bc.IsFreeGift && !bc.IsAdminDiscount)
                    {
                        basketCookie += bc.StockRef + "|" + bc.Quantity + "|||" + bc.Type + "|" + bc.LineUid +
                                        "||||False|";
                    }
                }
                basketCookie += "::1::1::";

                //Update Cookie
                DateTime expiry = System.DateTime.Now.Add(new System.TimeSpan(365, 0, 0, 0));

                HttpCookie basket = new HttpCookie("basket");
                basket.Value = basketCookie;
                basket.Expires = expiry;
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

            foreach (BasketContents entry in lbc)
            {
                int mult = entry.IsAdminDiscount ? -1 : 1;

                if (!entry.IsVoucher)
                {
                    //basketTotal += Math.Round(entry.PriceInc / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]), 2) * entry.Quantity * mult;
                    basketTotal += entry.PriceEx * entry.Quantity * mult;
                }
                if (!entry.IsVoucher && !entry.IsDelivery)
                {
                    basketQty += entry.Quantity;
                }
                if (!entry.IsCompatibleInk && !entry.IsVoucher && !entry.IsDelivery)
                {
                    compatibleInkOnly = false;
                }
                if (!entry.IsSpecialOrder && !entry.IsVoucher && !entry.IsDelivery)
                {
                    specialOrderOnly = false;
                }
                if (entry.IsBulky)
                {
                    isbulky = true;
                }
            }

            BasketTotals bt = new BasketTotals();
            if (HttpContext.Current.Session["B_BasketTotals"] != null)
            {
                bt = (BasketTotals) HttpContext.Current.Session["B_BasketTotals"];
            }

            var deliveryPrice = lbc.FirstOrDefault(x => x.IsDelivery);
            if (deliveryPrice != null)
                bt.Delivery = deliveryPrice.PriceEx;

            bt.Quantity = basketQty;
            bt.TotalExcVat = basketTotal;
            bt.GrandTotalExcVat = lbc.Sum(x => x.IsAdminDiscount ? Math.Round(x.PriceEx, 2) * -1 : Math.Round(x.PriceEx, 2) * x.Quantity);
            if (isVatExempt)
            {
                bt.Vat = 0;
                bt.TotalIncVat = bt.TotalExcVat;
                bt.GrandTotalIncVat = bt.GrandTotalExcVat;
            }
            else
            {
                bt.Vat = lbc.Sum(x => x.IsAdminDiscount ? Math.Round((x.PriceInc - x.PriceEx), 2) * -1 : Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100);
                bt.TotalIncVat = bt.TotalExcVat + lbc.Sum(x => x.IsVoucher ? 0 : Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100);
                bt.GrandTotalIncVat = bt.GrandTotalExcVat + bt.Vat;
            }

            HttpContext.Current.Session["B_BasketTotals"] = bt;
            HttpContext.Current.Session["B_CompatibleInkOnly"] = compatibleInkOnly;
            HttpContext.Current.Session["B_SpecialOrderOnly"] = specialOrderOnly;
            HttpContext.Current.Session["B_IsBulky"] = isbulky;
        }

        public static string ApplyVoucher()
        {
            if (((BasketTotals)HttpContext.Current.Session["B_BasketTotals"]).Voucher != 0)
            {
                RemoveVoucher();
            }

            List<BasketContents> lbc = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
            }
            BasketTotals bt = new BasketTotals();
            if (HttpContext.Current.Session["B_BasketTotals"] != null)
            {
                bt = (BasketTotals) HttpContext.Current.Session["B_BasketTotals"];
            }
            VoucherPromo v = new VoucherPromo();
            if (HttpContext.Current.Session["V_Voucher"] != null)
            {
                v = (VoucherPromo) HttpContext.Current.Session["V_Voucher"];
            }

            if (HttpContext.Current.Session["B_VoucherCode"] == null)
            {
                //lbc.RemoveAll(x => x.IsVoucher || x.IsFreeGift);
                // Decided on using a loop as we want to also set a property
                for (int i = lbc.Count - 1; i >= 0; i--)
                {
                    lbc[i].IsVoucherQualifyingItem = false;
                    if (lbc[i].IsFreeGift || lbc[i].IsVoucher)
                    {
                        Delete(lbc[i].StockRef);
                    }
                }

                bt.Voucher = 0;
                bt.VoucherVat = 0;

                bt.Vat = lbc.Sum(x => x.IsAdminDiscount ? Math.Round((x.PriceInc - x.PriceEx), 2) * -1 : Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100);
                bt.GrandTotalExcVat = lbc.Sum(x => x.IsAdminDiscount ? Math.Round(x.PriceEx, 2) * -1 : Math.Round(x.PriceEx, 2) * x.Quantity);
                bt.GrandTotalIncVat = bt.GrandTotalExcVat + bt.Vat;
                HttpContext.Current.Session["B_BasketTotals"] = bt;
                
                HttpContext.Current.Session["B_BasketArray"] = lbc;

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
                           sCFound =  sCFound || v.Categories.IndexOf(scl.categoryCodeFK ?? 0) != -1;
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
                            Delete(v.GiftStockRef);
                        }
                        // add the free gift to the basket

                        BasketContents bc = new BasketContents();
                        bc.IsFreeGift = true;
                        bc.StockRef = v.GiftStockRef;
                        bc.Quantity = 1;
                        bc.PriceEx = 0;
                        bc.PriceInc = 0;
                        bc.Type = 0;
                        bc.LineUid = 0;

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
                                voucherValue += bc.PriceInc * quantityRemaining;
                                quantityRemaining = 0;
                            }
                            else
                            {
                                bc.VoucherAmount = Math.Round(
                                    Math.Round(bc.PriceInc, 2) * ((v.Percentage ?? default(decimal)) / 100) /
                                    Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) *
                                    bc.Quantity, 2);
                                voucherValue += bc.PriceInc;
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

            // Update basket totals
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

        private static void RemoveVoucher()
        {
            List<BasketContents> lbc = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>)HttpContext.Current.Session["B_BasketArray"];
            }

            var i = lbc.FindIndex(x => x.IsVoucher);
            if (i >= 0)
            {
                //voucher exists delete the item
                lbc.Remove(lbc[i]);
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
        public static List<BasketContents> UpdateSummary(List<BasketContents> lbc, string account,
            bool newItemsOnly = false)
        {
            string refArray = Utilities.GetStockRefArray(lbc);
            bool isVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null && Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);

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
            DataTable dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSummaryData", sqlParms,
                "summarydata").Tables[0];

            StringBuilder summary = new StringBuilder();

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

                    //if (!newItemsOnly || (newItemsOnly && lbcEntry.PriceEx == 0))
                    //{
                    lbcEntry.ProductUrl = url;
                    if (!lbcEntry.IsFreeGift)
                    {
                        if (lbcEntry.Quantity > Convert.ToDecimal(dr["breakQuantity2"]))
                        {
                            lbcEntry.PriceEx = Math.Ceiling(Convert.ToDecimal(dr["breakPrice3ExVat"]) * 100) / 100;
                            lbcEntry.PriceInc = Convert.ToDecimal(dr["breakPrice3IncVat"]);
                        }
                        else
                        {
                            if (lbcEntry.Quantity > Convert.ToDecimal(dr["breakQuantity1"]))
                            {
                                lbcEntry.PriceEx = Math.Ceiling(Convert.ToDecimal(dr["breakPrice2ExVat"]) * 100) / 100;
                                lbcEntry.PriceInc = Convert.ToDecimal(dr["breakPrice2IncVat"]);
                            }
                            else
                            {
                                lbcEntry.PriceEx = Math.Ceiling(Convert.ToDecimal(dr["tradePriceExVat"]) * 100) / 100;
                                lbcEntry.PriceInc = Convert.ToDecimal(dr["tradePriceIncVat"]);
                            }
                        }
                    }
                    else
                    {
                        lbcEntry.Description = desc;
                    }
                    //}
                    lbcEntry.IsVatExempt = isVatExempt;

                    Tuple<string, decimal> price;
                    if (isVatExempt)
                    {
                        decimal p = Math.Round(Math.Round(lbcEntry.PriceInc, 2) / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) * lbcEntry.Quantity, 2);
                        price = Utilities.SetPrice(p, p);
                    }
                    else
                    {
                        decimal p1 = Math.Round(Math.Round(lbcEntry.PriceInc, 2) * lbcEntry.Quantity, 2);
                        decimal p2 = Math.Round(Math.Round(lbcEntry.PriceInc, 2) / Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) * lbcEntry.Quantity, 2);
                        price = Utilities.SetPrice(p1, p2);
                    }

                    // Build the basket Summary
                    summary.Append("<hr class=\"g-m-tb-10\" />");
                    summary.Append(
                        "<div class=\"basket-entry g-m-t-10 clearfix g-ps-r\" data-productid=\"" +
                        lbcEntry.StockRef + "\">");
                    summary.Append("<div class=\"pull-left\"><a href=\"" + url + "\">");
                    summary.Append("<img src=\"/Content/Images/1pxTrans.png\" class=\"deferImage\" data-original=\"" + imageUrl + "\" alt=\"" + desc + "\">");
                    summary.Append("</a></div>");
                    summary.Append("<div class=\"pull-left g-m-l-10 g-w-200\">");
                    summary.Append("<div><a class=\"primary\" href=\"" + url + "\">" + desc + "</a></div>");
                    summary.Append("<div class=\"g-m-t-5\"><strong>Quantity: </strong> " + lbcEntry.Quantity + "</div>");
                    summary.Append("<div><strong>Sub total: </strong> &pound;" + price.Item2.ToString("#,###,##0.00") + " " +
                                   price.Item1 + "</div>");
                    summary.Append("</div>");
                    if (lbcEntry.PriceEx != 0)
                    {
                        summary.Append("<div class=\"delete g-d-n g-ps-a\" data-productid=\"" + lbcEntry.StockRef +
                                       "\" style=\"right: 0px; bottom: 4px;\">");
                        summary.Append("<i class=\"fa fa-times\"></i>");
                        summary.Append("</div>");
                    }
                    summary.Append("</div>");
                }
                catch (Exception e) 
                {
                    Utilities.ProcessException(e);
                }
            }

            // Remove items from lbc with 0 price (discontinued items)
            lbc.RemoveAll(x => x.PriceEx == 0 && !x.IsVoucher && !x.IsDelivery && !x.IsFreeGift);

            HttpContext.Current.Session["B_BasketSummary"] = summary.ToString();

            foreach (BasketContents bc in lbc)
            {
                if (bc.IsDelivery || bc.IsVoucher || bc.IsAdminDiscount)
                {
                    bc.IsVatExempt = isVatExempt;
                }
            }

            return lbc;
        }

        public static int ProcessDelivery(int serviceId)
        {
            deliveryService ds = EntityAccess.ReadDeliveryService(x => x.DeliveryServiceId == serviceId)
                .FirstOrDefault();

            // Delete the existing DELIVERY item from the basket
            RemoveDelivery();

            // Add the delivery to the basket
            if (ds != null)
            {
                BasketContents bc = new BasketContents
                {
                    IsDelivery = true,
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
                    DeliveryMethod = ds.DeliveryMethod
                };

                Update(bc);

                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>)HttpContext.Current.Session["B_BasketArray"];
                }
                lbc = UpdateSummary(lbc,
                    HttpContext.Current.Session["U_AccountNo"] != null
                        ? HttpContext.Current.Session["U_AccountNo"].ToString()
                        : " ", false);
                HttpContext.Current.Session["B_BasketArray"] = lbc;

                UpdateBasketSession(lbc);
            }

            return ds?.DeliveryMethod ?? 0;
        }

        public static void RemoveDelivery()
        {
            List<BasketContents> lbc = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>)HttpContext.Current.Session["B_BasketArray"];
            }
            BasketTotals bt = new BasketTotals();
            if (HttpContext.Current.Session["B_BasketTotals"] != null)
            {
                bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            }

            var i = lbc.FindIndex(x => x.IsDelivery);
            if (i >= 0)
            {
                //delivery exists delete the item
                lbc.Remove(lbc[i]);
            }
            bt.Delivery = 0;
            UpdateBasketSession(lbc);

            HttpContext.Current.Session["B_BasketArray"] = lbc;
            HttpContext.Current.Session["B_BasketTotals"] = bt;
        }
    }
}