using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using Kendo.Mvc.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.EnterpriseServices.CompensatingResourceManager;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Routing;
using VMerchantWrapper.Entities;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;

namespace CommonUI.Models
{
    public class Helper
    {
    }

    public static class LinkExtensions
    {
        /// <summary>
        /// Render an action link with HTML in the button text
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="linkText"></param>
        /// <param name="actionName"></param>
        /// <param name="controllerName"></param>
        /// <param name="routeValues"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static MvcHtmlString HtmlActionLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            object routeValues,
            object htmlAttributes)
        {
            var tagActionLink = htmlHelper
                .ActionLink("[replace]", actionName, controllerName, routeValues, htmlAttributes).ToHtmlString();
            return MvcHtmlString.Create(tagActionLink.Replace("[replace]", linkText));
        }

        /// <summary>
        /// Render Raw HTML from a dictionary element
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="replacements"></param>
        /// <returns></returns>
        public static IHtmlString RawFromDict(
            this HtmlHelper htmlHelper,
            Dictionary<string, string> dict,
            string key,
            Dictionary<string, string> replacements = null,
            string wrapper = null)
        {
            if (dict.ContainsKey(key))
            {
                string s = dict[key];
                if (!String.IsNullOrEmpty(wrapper))
                {
                    s = "<div class=\"" + wrapper + "\">" + s + "</div>";
                }
                replacements = Utilities.AddStandardReplacements(replacements);
                if (replacements != null)
                {
                    foreach (KeyValuePair<string, string> kvp in replacements)
                    {
                        s = s.Replace(kvp.Key, kvp.Value);
                    }
                }
                return htmlHelper.Raw(s);
            }
            return new HtmlString("");
        }

        public static MvcHtmlString CustomValidationMessage<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression,
            string validationMessage,
            object htmlAttributes)
        {
            TagBuilder icon = new TagBuilder("i");
            icon.Attributes.Add("class", "fa fa-exclamation-triangle g-p-r-5");
            var tagValidationMessage = htmlHelper.ValidationMessageFor(expression, validationMessage, htmlAttributes)
                .ToHtmlString();
            return MvcHtmlString.Create(tagValidationMessage + icon.ToString());
        }

        public static MvcHtmlString ExtdDropDownList(
            this HtmlHelper htmlHelper,
            string name,
            IEnumerable<ExtdSelectListItem> selectList,
            string optionLabel,
            object htmlAttributes)
        {
            TagBuilder dropdown = new TagBuilder("select");
            dropdown.Attributes.Add("id", name);
            dropdown.Attributes.Add("name", name);
            dropdown.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));

            StringBuilder options = new StringBuilder();
            if (optionLabel != null)
            {
                options = options.Append("<option value='" + String.Empty + "'>" + optionLabel + "</option>");
            }
            foreach (ExtdSelectListItem item in selectList)
            {
                TagBuilder option = new TagBuilder("option");
                option.Attributes.Add("value", item.Value);
                option.InnerHtml = item.Text;
                var addatt = HtmlHelper.AnonymousObjectToHtmlAttributes(item.Data);
                foreach (var kvp in addatt)
                {
                    option.Attributes.Add(kvp.Key, kvp.Value.ToString());
                }
                options = options.Append(option.ToString());
            }
            dropdown.InnerHtml = options.ToString();
            return MvcHtmlString.Create(dropdown.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString ProductAvailability(
            this HtmlHelper htmlHelper,
            int availability)
        {
            string tag = "";
            switch (availability)
            {
                case 1:
                case 7:
                    tag = "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #1eb271; color: #ffffff; padding: 8px; text-align: center \">In stock</p></td>";
                    break;
                default:
                    tag = "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #fc6365; color: #ffffff; padding: 8px; text-align: center \">Out of Stock</p></td>";
                    break;
            }

            return MvcHtmlString.Create(tag);
        }

        public static MvcHtmlString ProductAvailability(
            this HtmlHelper htmlHelper,
            int availability,
            bool deliveryOverride,
            bool useShortDescription = false)
        {
            string tag = "";
            if (useShortDescription)
            {
                switch (availability)
                {
                    case 0:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Discontinued</div>";
                        break;
                    case 1:
                        tag = "<div class=\"g-bc-pos g-fc-st\">In Stock</div>";
                        break;
                    case 2:
                        tag = "<div class=\"g-bc-pos g-fc-st\">Availability 2-3 Days</div>";
                        break;
                    case 3:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>";
                        break;
                    case 4:
                        tag = "<div class=\"g-bc-neg\">Availability 2-3 Days</div>";
                        break;
                    case 7:
                        tag = "<div class=\"g-bc-pos g-fc-st\">In Stock</div>";
                        break;
                    case 8:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>";
                        break;
                    case 10:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Special Order Item</div>";
                        break;
                    case 11:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Out of Stock</div>";
                        break;
                    case 12:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>";
                        break;
                    case 13:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Out of Stock</div>";
                        break;
                    default:
                        tag = "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>";
                        break;
                }
            }
            else
            {
                Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
                switch (availability)
                {
                    case 0:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Discontinued Item</div>" +
                              "<div class=\"g-i\">Item is no longer available</div>" +
                              "</div>";
                        break;
                    case 1:
                        tag = "<div class=\"g-b-1-pos\">" +
                              "<div class=\"g-bc-pos g-fc-st\"><strong>In Stock</strong> - Delivered " +
                              HttpContext.Current.Session["D_standardDeliveryDay"].ToString() + "</div>" +
                              (deliveryOverride ? "" : "<div class=\"g-i\">Order Within <strong><span class=\"cutoffCountdownFalse\"></span></strong></div>") +
                              "</div>";
                        break;
                    case 2:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Availability 2-3 Days</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                    case 3:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                    case 4:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Availability 2-3 Days</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                    case 7:
                        tag = "<div class=\"g-b-1-pos\">" +
                              "<div class=\"g-bc-pos g-fc-st\"><strong>In Stock</strong> - Delivered " +
                              HttpContext.Current.Session["D_standardDeliveryDay"].ToString() + "</div>" +
                              (deliveryOverride ? "" : "<div class=\"g-i\">Order Within <strong><span class=\"cutoffCountdownFalse\"></span></strong></div>") +
                              "</div>";
                        break;
                    case 8:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                    case 10:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-fc-orange\">Special Order Item</div>" +
                              "<div class=\"g-i\">Call <a href=\"tel:" + Utilities.GetItemFromDict(commondata, "TelephoneNumber") + "\">" + Utilities.GetItemFromDict(commondata, "TelephoneNumber") + "</a> for info</div>" +
                              "</div>";
                        break;
                    case 11:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Out of Stock</div>" +
                              "<div class=\"g-i\">Currently unavailable</div>" +
                              "</div>";
                        break;
                    case 12:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                    case 13:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Out of Stock</div>" +
                              "<div class=\"g-i\">Currently unavailable</div>" +
                              "</div>";
                        break;
                    default:
                        tag = "<div class=\"g-b-1-neg\">" +
                              "<div class=\"g-bc-neg g-fc-st\">Back In Stock Soon</div>" +
                              "<div class=\"g-i\">Dispatched when stock returns</div>" +
                              "</div>";
                        break;
                }
            }

            return MvcHtmlString.Create(tag);
        }

        public static MvcHtmlString NewProductAvailability(
            this HtmlHelper htmlHelper,
            int availability,
            bool deliveryOverride,
            bool useShortDescription = false)
        {
            string tag = "";
            if (useShortDescription)
            {
                // Currently not in use
            }
            else
            {
                Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
                switch (availability)
                {
                    case 0:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-times g-fs-lg g-fc-nm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                            "<div><strong>Discontinued Item</strong></div>" +
                            "<div><strong>Currently unavailable</strong></div>" +
                            "</div></div>";
                        break;
                    case 1:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-check g-fs-lg g-fc-pm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-pm\"><strong>In Stock - Delivered " + HttpContext.Current.Session["D_StandardDeliveryDay"] + "</strong></div>" +
                              (deliveryOverride ? "" : "<div>Order Within <strong><span class=\"cutoffCountdownFalse\"></span></strong></div>") +
                            "</div></div>";
                        break;
                    case 2:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-circle-o g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Availability 2-3 Days</strong></div>" +
                              "<div>Dispatched when stock Returns</div>" +
                            "</div></div>";
                        break;
                    case 3:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-history g-fs-lg g-fc-nm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Back in Stock soon</strong></div>" +
                              "<div>Dispatched when stock Returns</div>" +
                            "</div></div>";
                        break;
                    case 4:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-circle-o g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                            "<div class=\"g-fc-orange\"><strong>Availability 2-3 Days</strong></div>" +
                            "<div>Dispatched when stock Returns</div>" +
                            "</div></div>";
                        break;
                    case 7:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-check g-fs-lg g-fc-pm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-pm\"><strong>In Stock - Delivered " + HttpContext.Current.Session["D_StandardDeliveryDay"] + "</strong></div>" +
                              (deliveryOverride ? "" : "<div>Order Within <strong><span class=\"cutoffCountdownFalse\"></span></strong></div>") +
                            "</div></div>";
                        break;
                    case 8:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-history g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Back in Stock soon</strong></div>" +
                              "<div>Dispatched when stock Returns</span></div>" +
                            "</div></div>";
                        break;
                    case 10:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-circle-o g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Special Order Item</strong></div>" +
                              "<div class=\"g-i\">Call <a href=\"tel:" + Utilities.GetItemFromDict(commondata, "TelephoneNumber") + "\">" + Utilities.GetItemFromDict(commondata, "TelephoneNumber") + "</a> for info</div>" +
                            "</div></div>";
                        break;
                    case 11:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-times g-fs-lg g-fc-nm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-nm\"><strong>Out of Stock</strong></div>" +
                              "<div><strong>Currently unavailable</strong></div>" +
                            "</div></div>";
                        break;
                    case 12:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-circle-o g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Back in Stock soon</strong></div>" +
                              "<div>Dispatched when stock Returns</div>" +
                            "</div></div>";
                        break;
                    case 13:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-times g-fs-lg g-fc-nm g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-nm\"><strong>Out of Stock</strong></div>" +
                              "<div><strong>Currently unavailable</strong></div>" +
                            "</div></div>";
                        break;
                    default:
                        tag = "<div class=\"row g-m-b-20\"> " +
                            "<div class=\"col-xs-2\">" +
                            "<i class=\"fa fa-history g-fs-lg g-fc-orange g-p-l-5\"></i>" +
                            "</div>" +
                            "<div class=\"col-xs-10\">" +
                              "<div class=\"g-fc-orange\"><strong>Back in Stock soon</strong></div>" +
                              "<div>Dispatched when stock Returns</div>" +
                            "</div></div>";
                        break;
                }
            }

            return MvcHtmlString.Create(tag);
        }

        public static MvcHtmlString FilterAttributes(
            this HtmlHelper htmlHelper,
            List<ProductAttribute> attList)
        {
            StringBuilder atts = new StringBuilder();
            foreach (ProductAttribute pa in attList)
            {
                atts = atts.Append(" data-att-" + pa.Number.ToString("000") + "=\"#" + pa.ValueId.Replace("(", "").Replace(")", "").Replace(".", "_") + "#\"");
            }

            return MvcHtmlString.Create(atts.ToString());
        }

        public static MvcHtmlString DisplayBasketCookie(
            this HtmlHelper htmlHelper,
            string cookie)
        {
            List<string> ba = new List<string>();
            List<string> dela = new List<string>();
            StringBuilder basket = new StringBuilder();

            ba = cookie.Split(new string[] {"|"}, StringSplitOptions.None).ToList();
            if (ba.Last().Contains(":"))
            {
                dela = ba.Last().Split(new string[] {"::"}, StringSplitOptions.None).ToList();
            }

            StringBuilder ret = new StringBuilder();
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                    <tr>
                    <td>");
            if (ba.Count > 8)
            {
                int i = ba.Count / 10;
                for (int j = 0; j < i; j++)
                {
                    ret = ret.Append(ba[j * 10] + "|" + ba[(j * 10) + 1] + "|" + ba[(j * 10) + 2] + "|" +
                                     ba[(j * 10) + 3] + "|" + ba[(j * 10) + 4] + "|" + ba[(j * 10) + 5]);
                    ret = ret.Append(ba[(j * 10) + 6] + "|" + ba[(j * 10) + 7] + "|" + ba[(j * 10) + 8] + "|" +
                                     ba[(j * 10) + 9] + "|" + "<br />");
                }
                ret = ret.Append(ba[ba.Count - 1].ToSafeString());
            }
            ret = ret.Append("</td></tr></table></div>");

            return MvcHtmlString.Create(ret.ToString());
        }

        public static MvcHtmlString DisplayBasketContents(
            this HtmlHelper htmlHelper,
            List<BasketContents> lbc)
        {
            StringBuilder ret = new StringBuilder();
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Ref</th>
                                <th>Id</th>
                                <th>Quantity</th>
                                <th>St. Qty</th>
                                <th>Part No</th>
                                <th>Group No</th>
                                <th>Category No</th>
                                <th>Group Name</th>
                                <th>Affiliate Commission Group</th>
                                <th>Price Exc</th>
                                <th>Price Inc</th>
                                <th>Description</th>
                                <th>Availability</th>
                                <th>Image</th>
                            </tr>
                        </thead>");
            foreach (BusinessLogic.BasketContents bc in lbc)
            {
                ret = ret.Append(@"
                    <tr>
                        <td>" + bc.StockRef + @"</td>
                        <td>" + bc.ProductId + @"</td>
                        <td>" + bc.Quantity + @"</td>
                        <td>" + bc.QtyStart + @"</td>
                        <td>" + bc.PartNo + @"</td>
                        <td>" + bc.GroupNo + @"</td>
                        <td>" + bc.CategoryNo + @"</td>
                        <td>" + bc.GroupName + @"</td>
                        <td>" + bc.AffiliateCommissionGroup + @"</td>
                        <td> " + bc.PriceEx + @"</td>
                        <td> " + bc.PriceInc + @"</td>
                        <td> " + bc.Description + @"</td>
                        <td> " + bc.Availability + @"</td>
                        <td> " + bc.ImageUrl + @"</td>
                    </tr>");
            }
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Ref</th>
                                <th>Url</th>
                                <th>Is Compatible</th>
                                <th>Compatible Ink</th>
                                <th>Is Bulky</th>
                                <th>Is Special Order</th>
                                <th>Is VAT Exempt</th>
                                <th>Upsell Active</th>
                                <th>Voucher Qualifies</th>
                                <th>Voucher Type</th>
                                <th>Voucher Amt</th>
                                <th>Item Type</th>
                                <th>Delivery Method</th>
                                <th>Is Free Gift</th>
                            </tr>
                        </thead>");
            foreach (BusinessLogic.BasketContents bc in lbc)
            {
                ret = ret.Append(@"
                    <tr>
                        <td>" + bc.StockRef + @"</td>
                        <td> " + bc.ProductUrl + @"</td>
                        <td> " + bc.IsCompatible + @"</td>
                        <td> " + bc.IsCompatibleInk + @"</td>
                        <td> " + bc.IsBulky + @"</td>
                        <td> " + bc.IsSpecialOrder + @"</td>
                        <td> " + bc.IsVatExempt + @"</td>
                        <td> " + bc.IsUpsellTriggered + @"</td>
                        <td> " + bc.IsVoucherQualifyingItem + @"</td>
                        <td> " + bc.VoucherType + @"</td>
                        <td> " + bc.VoucherAmount + @"</td>
                        <td> " + bc.ItemType.ToString() + @"</td>
                        <td> " + bc.DeliveryMethod + @"</td>
                        <td> " + bc.IsFreeGift + @"</td>
                    </tr>");
            }
            ret = ret.Append("</table></div>");

            return MvcHtmlString.Create(ret.ToString());
        }

        public static MvcHtmlString DisplayBasketTotals(
            this HtmlHelper htmlHelper,
            BasketTotals bt)
        {
            StringBuilder ret = new StringBuilder();
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Quantity</th>
                                <th>Total Inc</th>
                                <th>Total Exc</th>
                                <th>Grand Total Inc</th>
                                <th>Grand Total Exc</th>
                                <th>VAT</th>
                                <th>Voucher</th>
                                <th>Voucher VAT</th>
                                <th>Delivery</th>
                            </tr>
                        </thead>
                        <tr>
                            <td>" + bt.Quantity + @"</td>
                            <td> " + bt.TotalIncVat + @"</td>
                            <td> " + bt.TotalExcVat + @"</td>
                            <td> " + bt.GrandTotalIncVat + @"</td>
                            <td> " + bt.GrandTotalExcVat + @"</td>
                            <td> " + bt.Vat + @"</td>
                            <td> " + bt.Voucher + @"</td>
                            <td> " + bt.VoucherVat + @"</td>
                            <td> " + bt.Delivery + @"</td>

                        </tr>
                    </table>
                </div>");

            return MvcHtmlString.Create(ret.ToString());
        }

        public static MvcHtmlString DisplayVoucherDetails(
            this HtmlHelper htmlHelper,
            VoucherPromo v)
        {
            StringBuilder ret = new StringBuilder();
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Code</th>
                                <th>Stock Ref</th>
                                <th>Is Personal</th>
                                <th>Account No.</th>
                                <th>Is Global</th>
                                <th>Is Used</th>
                                <th>Min Basket Value</th>
                                <th>Min Qual Value</th>
                                <th>Type</th>
                                <th>Percentage</th>
                                <th>Amount</th>
                                <th>Total Purchases</th>
                                <th>Discounted Purchase</th>
                                <th>Description</th>
                            </tr>
                        </thead>");

            ret = ret.Append(@"
                    <tr>
                        <td>" + v.VoucherCode + @"</td>
                        <td>" + v.StockRef + @"</td>
                        <td>" + (string.IsNullOrEmpty(v.AccountNumber) ? "Global" : "Personal") + @"</td>
                        <td>" + v.AccountNumber + @"</td>
                        <td>" + v.IsGlobal + @"</td>
                        <td>" + v.IsUsed + @"</td>
                        <td>" + v.MinBasketValue + @"</td>
                        <td>" + v.MinQualValue + @"</td>
                        <td>" + (VmVoucherType)v.VoucherTypeFk + @"</td>
                        <td>" + v.Percentage + @"</td>
                        <td>" + v.Amount + @"</td>
                        <td>" + v.MultiBuyQualNo + @"</td>
                        <td>" + v.MultiBuyNoDiscounted + @"</td>
                        <td>" + v.Description + @"</td>
                    </tr>");
            ret = ret.Append("</table></div>");

            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th style=""width: 40%;"">Categories</th>
                                <th style=""width: 40%;"">Groups</th>
                            </tr>
                        </thead>");

            ret = ret.Append(@"
                    <tr>
                        <td>" + (v.Categories == null ? "" : String.Join(",", v.Categories)) + @"</td>
                        <td>" + (v.Groups == null ? "" : String.Join(",", v.Groups)) + @"</td>
                    </tr>");
            ret = ret.Append("</table></div>");

            return MvcHtmlString.Create(ret.ToString());
        }

        public static MvcHtmlString DisplayAddressDetails(
            this HtmlHelper htmlHelper,
            BusinessLogic.Address add)
        {
            StringBuilder ret = new StringBuilder();
            if (add.Line1 != "")
            {
                ret = ret.Append(add.Line1 + "<br/>");
            }
            if (add.Line2 != "")
            {
                ret = ret.Append(add.Line2 + "<br/>");
            }
            if (add.Line3 != "")
            {
                ret = ret.Append(add.Line3 + "<br/>");
            }
            if (add.Line4 != "")
            {
                ret = ret.Append(add.Line4 + "<br/>");
            }
            if (add.Line5 != "")
            {
                ret = ret.Append(add.Line5 + "<br/>");
            }
            if (add.PostCode != "")
            {
                ret = ret.Append(add.PostCode);
            }

            return MvcHtmlString.Create(ret.ToString());
        }

        public static MvcHtmlString DisplayCheckoutDetails(
            this HtmlHelper htmlHelper,
            CheckoutDetails cd)
        {
            StringBuilder ret = new StringBuilder();
            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Recipient</th>
                                <th>Delivery</th>
                                <th>Billing</th>
                                <th>Tel.</th>
                                <th>Email</th>
                                <th>Pay Meth</th>
                                <th>Reference</th>
                                <th>BO Ref</th>
                                <th>PayPalRef</th>                                
                                <th>SageRef</th>
                                <th>Save Card</th>
                                <th>SageCardID</th>
                                <th>IsNewCustomer</th>
                                <th>Newsletter</th>
                                <th>Customer Type</th>
                            </tr>
                        </thead>
                        <tr>");
            ret = ret.Append(@"<td>");
            if (cd.Name != null)
            {
                ret = ret.Append(
                    cd.Name.Title.ToSafeString() + " " + cd.Name.Firstname.ToSafeString() + " " +
                    cd.Name.Surname.ToSafeString());
            }
            ret = ret.Append(@"</td><td>");
            if (cd.RecipientName != null)
            {
                ret = ret.Append(
                    cd.RecipientName.Title.ToSafeString() + " " + cd.RecipientName.Firstname.ToSafeString() + " " +
                    cd.RecipientName.Surname.ToSafeString());
            }
            ret = ret.Append(@"</td><td>");
            if (cd.DeliveryAddress != null)
            {
                ret = ret.Append(
                    cd.DeliveryAddress.Line1.ToSafeString() + "<br />" + cd.DeliveryAddress.Line2.ToSafeString() +
                    @"<br />" + cd.DeliveryAddress.Line3.ToSafeString() + "<br />" +
                    cd.DeliveryAddress.Line4.ToSafeString() + "<br />" + cd.DeliveryAddress.PostCode.ToSafeString());
            }
            ret = ret.Append(@"</td><td>");
            if (cd.BillingAddress != null)
            {
                ret = ret.Append(
                    cd.BillingAddress.Line1.ToSafeString() + "<br />" + cd.BillingAddress.Line2.ToSafeString() +
                    "<br />" + cd.BillingAddress.Line3.ToSafeString() + "<br />" +
                    cd.BillingAddress.Line4.ToSafeString() + "<br />" + cd.BillingAddress.PostCode.ToSafeString());
            }
            ret = ret.Append("</td>");
            ret = ret.Append("<td>" + cd.TelephoneNumber.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.Email.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.PaymentMethod.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.Reference.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.BackOfficeCustRef.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.PayPalRef.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.SagePayRef.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.SaveThisCard.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.SagePayCardId.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.IsNewCustomer.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.Newsletter.ToSafeString() + "</td>");
            ret = ret.Append(@"
                </tr>
                </table>
                </div>");

            ret = ret.Append(@"
                <div class=""table-responsive"">
                    <table class=""table table-bordered g-w-a"">
                        <thead>
                            <tr>
                                <th>Account Number</th>
                                <th>Account Record</th>
                                <th>Account Contact</th>
                                <th>Account TelNo</th>
                                <th>Account Email</th>
                                <th>Account Invoice Address</th>
                                <th>Password</th>
                                <th>Total Inc.</th>
                                <th>Zero Stock</th>
                                <th>Spcial Order</th>
                                <th>Save Card</th>
                            </tr>
                        </thead>
                        <tr>");
            ret = ret.Append("<td>" + cd.AccountNumber.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.AccountRecord.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.AccountContact.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.AccountTelNo.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.AccountEmail.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.AccountInvoiceAddress.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.Password.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.TotalIncVat.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.ZeroStock.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.IsSpecialOrder.ToSafeString() + "</td>");
            ret = ret.Append("<td>" + cd.SaveThisCard.ToSafeString() + "</td>");
            ret = ret.Append(@"
                </tr>
                </table>
                </div>");

            return MvcHtmlString.Create(ret.ToString());
        }

        /// <summary>
        /// Wraps matched strings in HTML span elements styled with desired css class
        /// </summary>
        public static MvcHtmlString HighlightKeyWords(this HtmlHelper htmlHelper, string text, string keywords,
            string cssClass, bool fullMatch)
        {
            if (string.IsNullOrEmpty(keywords) || keywords == String.Empty || cssClass == String.Empty)
                return MvcHtmlString.Create(text);
            keywords = Utilities.RemoveSpecialCharacters(keywords);
            var words = keywords.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            var returnString = "";

            if (!fullMatch)
            {
                returnString += Regex.Replace(text,
                    String.Join("|", words),
                    string.Format("<span class=\"{0}\">{1}</span>",
                        cssClass,
                        "$0"),
                    RegexOptions.IgnoreCase);
            }
            else
            {
                returnString = words.Select(word => "\\b" + word.Trim() + "\\b")
                    .Aggregate(text, (current, pattern) =>
                        Regex.Replace(current,
                            pattern,
                            string.Format("<span class=\"{0}\">{1}</span>",
                                cssClass,
                                "$0"),
                            RegexOptions.IgnoreCase));
            }

            return MvcHtmlString.Create(returnString);
        }

        public static MvcHtmlString ShowSaleInfoButton(
            this HtmlHelper htmlHelper,
            bool IsCompatibleSale,
            bool IsOEMSale,
            bool IsStationerySale)
        {
            StringBuilder ret = new StringBuilder();
            if (IsCompatibleSale || IsOEMSale || IsStationerySale)
            {
                ret = ret.Append(@"
                <div class=""g-m-t-20 g-fs-md"">
                    <button class=""popup g-cur-p g-butt-primary-inv g-butt-lg""
                            data-popupname=""SaleInfo""
                            data-replacements=""""
                            data-toggle=""modal""
                            data-target=""#popup"">
                        <strong>Sale Details</strong>
                    </button>
                </div>");
            }

            return MvcHtmlString.Create(ret.ToString());
        }

        public static string CleanUrl(
            this HtmlHelper htmlHelper,
            string url)
        {
            return Utilities.CleanUrl(url);
        }

        public static MvcHtmlString InsertScript(
            this HtmlHelper htmlHelper,
            string url,
            string technique,
            bool isActive)
        {
            string ret = "";
            if (isActive)
            {
                ret = "<script type=\"text/javascript\" src=\"" + url + "\" " + technique + "></script>";
            }
            return MvcHtmlString.Create(ret);
        }

        public static IHtmlString GoogleCaptcha(
            this HtmlHelper helper)
        {
            string publicSiteKey = ConfigurationManager.AppSettings["ReCaptchaSiteKey"];
            var mvcHtmlString = new TagBuilder("div")
            {
                Attributes =
            {
                new KeyValuePair<string, string>("class", "g-recaptcha"),
                new KeyValuePair<string, string>("data-sitekey", publicSiteKey)
            }
            };

            const string googleCaptchaScript = "<script src='https://www.google.com/recaptcha/api.js'></script>";
            var renderedCaptcha = mvcHtmlString.ToString(TagRenderMode.Normal);

            return MvcHtmlString.Create($"{googleCaptchaScript}{renderedCaptcha}");
        }

        public static IHtmlString InvalidGoogleCaptchaLabel(
            this HtmlHelper helper, 
            string errorText)
        {
            var invalidCaptchaObj = helper.ViewContext.Controller.TempData["InvalidCaptcha"];

            var invalidCaptcha = invalidCaptchaObj?.ToString();
            if (string.IsNullOrWhiteSpace(invalidCaptcha)) return MvcHtmlString.Create("");

            var buttonTag = new TagBuilder("span")
            {
                Attributes =
            {
                new KeyValuePair<string, string>("class", "text text-danger")
            },
                InnerHtml = errorText ?? invalidCaptcha
            };

            return MvcHtmlString.Create(buttonTag.ToString(TagRenderMode.Normal));
        }

        public static string Controller(this HtmlHelper htmlHelper)
        {
            var routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;

            if (routeValues.ContainsKey("controller"))
                return (string) routeValues["controller"].ToString().ToLower();

            return string.Empty;
        }

        public static string Action(this HtmlHelper htmlHelper)
        {
            var routeValues = HttpContext.Current.Request.RequestContext.RouteData.Values;

            if (routeValues.ContainsKey("action"))
                return (string) routeValues["action"].ToString().ToLower();

            return string.Empty;
        }
    }

    public static class ObjectExtensions
    {
        public static string ToSafeString(this object value)
        {
            return (value ?? string.Empty).ToString();
        }
    }
}