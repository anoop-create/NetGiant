using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Web.UI.WebControls;
using System.Reflection;
using System.Web.Mvc.Ajax;
using DP001DataAccess.Entities;

namespace DP001Website.Models
{
    public class Helper
    {
    }

    public static class LinkExtensions
    {
        public static MvcHtmlString TableHeaderLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            Dictionary<string, string> routeValues
        )
        {
            var anchor = new TagBuilder("a");
            var span = new TagBuilder("span");
            string searchString = "";
            string sortDir = "";
            bool firstTime = true;

            switch (routeValues.FirstOrDefault(x => x.Key == "sdir").Value.ToString())
            {
                case "asc":
                    span.AddCssClass("fa");
                    span.AddCssClass("fa-sort-down");
                    sortDir = "desc";
                    break;
                case "desc":
                    span.AddCssClass("fa");
                    span.AddCssClass("fa-sort-up");
                    sortDir = "asc";
                    break;
                default:
                    span.AddCssClass("fa");
                    span.AddCssClass("fa-sort");
                    sortDir = "asc";
                    break;
            }

            foreach (var entry in routeValues)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    searchString += "&";
                }
                if (entry.Key == "sdir")
                {
                    searchString += entry.Key + "=" + sortDir;
                }
                else
                {
                    searchString += entry.Key + "=" + entry.Value;
                }
            }

            anchor.InnerHtml = linkText + " " + span.ToString();
            anchor.MergeAttribute("href", "/" + controllerName + "/" + actionName + "?" + searchString);
            anchor.MergeAttribute("aria-hidden", "true");

            return MvcHtmlString.Create(anchor.ToString());
        }

        public static MvcHtmlString MenuLink(
                this HtmlHelper htmlHelper,
                string linkText,
                string linkUrl,
                string cssClass
            )
        {
            var listItem = new TagBuilder("li");
            listItem.AddCssClass("active");

            var anchor = new TagBuilder("a");
            anchor.Attributes["href"] = linkUrl;
            if (cssClass != "")
                anchor.AddCssClass(cssClass);
            anchor.SetInnerText(linkText);

            listItem.InnerHtml = anchor.ToString();
            return MvcHtmlString.Create(listItem.ToString());
        }

        public static MvcHtmlString SubActionLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            int parentLinkID
        )
        {
            var currentAction = htmlHelper.ViewContext.RouteData.GetRequiredString("action");
            var currentController = htmlHelper.ViewContext.RouteData.GetRequiredString("controller");
            if (actionName == currentAction && controllerName == currentController)
            {
                var anchor = new TagBuilder("a");
                anchor.Attributes["href"] = "#";
                anchor.AddCssClass("helperActive");
                anchor.SetInnerText(linkText);
                anchor.Attributes.Add("parentLinkID", parentLinkID.ToString());

                return MvcHtmlString.Create(anchor.ToString());
            }
            return htmlHelper.ActionLink(linkText, actionName, controllerName, null, new { @parentLinkID = parentLinkID });
        }

        public static MvcHtmlString ButtonLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            object htmlAttributes
        //Dictionary<string, string> htmlAttributes
        )
        {
            var button = new TagBuilder("button");
            var anchor = new TagBuilder("a");
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var urlHelper = new UrlHelper(htmlHelper.ViewContext.RequestContext);

            foreach (var att in attributes)
            {
                button.Attributes[att.Key] = att.Value.ToString();
            }
            button.InnerHtml = linkText;

            anchor.Attributes["href"] = urlHelper.Action(actionName, controllerName);
            anchor.InnerHtml = button.ToString();

            return MvcHtmlString.Create(anchor.ToString());
        }

        public static string ReflectionGetProperty(
            this HtmlHelper htmlHelper,
            string dataProperty,
            object data
        )
        {
            PropertyInfo prop = data.GetType().GetProperty(dataProperty);
            return prop.GetValue(data, null).ToString();
        }

        public static MvcHtmlString SortingTableHeaderLink(
            this HtmlHelper htmlHelper,
            string name,
            int page,
            string sortField,
            string curSortOrder,
            bool isDefault)
        {
            var currentAction = htmlHelper.ViewContext.RouteData.GetRequiredString("action");
            var currentController = htmlHelper.ViewContext.RouteData.GetRequiredString("controller");
            var span = new TagBuilder("span");
            var anchor = new TagBuilder("a");
            var href = "/" + currentController + "/" + currentAction + "?page=" + page + "&sortOrder=" + sortField;

            if (!String.IsNullOrEmpty(htmlHelper.ViewBag.SearchTerm))
            {
                href = href + "&searchTerm=" + htmlHelper.ViewBag.SearchTerm;
            }

            anchor.Attributes["href"] = href;

            if (curSortOrder.Contains(sortField.Split('_')[0]))
            {
                if (curSortOrder.Contains("desc"))
                {
                    span.AddCssClass("fa-sort-down fa");
                }
                else
                {
                    span.AddCssClass("fa-sort-up fa");
                }
            }
            else
            {
                if (isDefault && String.IsNullOrEmpty(curSortOrder))
                {
                    span.AddCssClass("fa-sort-up fa");
                }
                else
                {
                    span.AddCssClass("fa-sort fa");
                }
            }

            anchor.InnerHtml = name + span.ToString();

            return MvcHtmlString.Create(anchor.ToString());
        }

        public static MvcHtmlString TooltipLink(
            this HtmlHelper htmlHelper,
            string displayText,
            object htmlAttributes = null,
            bool applyTooltip = true,
            string dataToggleName = "")
        {
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var anchor = displayText;

            if (applyTooltip)
            {
                anchor = htmlHelper.ActionLink(displayText, "#", "#", htmlAttributes).ToString();
            }

            return MvcHtmlString.Create(anchor);
        }

        public static MvcHtmlString TooltipActionLink(
            this HtmlHelper htmlHelper,
            string displayText,
            string actionName,
            string controllerName,
            object routeValues,
            object htmlAttributes = null,
            bool applyTooltip = true)
        {
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var anchor = displayText;

            if (applyTooltip)
            {
                anchor = htmlHelper.ActionLink(displayText, actionName, controllerName, routeValues, htmlAttributes).ToString();
            }

            return MvcHtmlString.Create(anchor);
        }

        public static MvcHtmlString HtmlActionLink(
            this HtmlHelper htmlHelper,
            string linkText,
            string actionName,
            string controllerName,
            object routeValues,
            object htmlAttributes)
        {
            var tagActionLink = htmlHelper.ActionLink("[replace]", actionName, controllerName, routeValues, htmlAttributes).ToHtmlString();
            return MvcHtmlString.Create(tagActionLink.Replace("[replace]", linkText));
        }

        public static MvcHtmlString ChannelDropDown(
            this HtmlHelper htmlHelper,
            List<Channel> channelList,
            string currentChannel,
            string addClass = "",
            string uid = "0")
        {
            string tag = "<select class=\"selectpicker " + addClass + "\" data-style=\"btn-header\" id=\"currentChannel" + uid + "\" name=\"currentChannel" + uid + "\" onchange=\"changeChannel(this.value)\">";
            tag += "<optgroup label=\"Channel Selection\">";
            //tag += "<option data-divider=\"true\"></ option >";
            foreach (Channel ch in channelList)
            {
                string sel = "";
                string glyph = "fa fa-circle-o";
                if (ch.ChannelID.ToString() == currentChannel)
                {
                    sel = "selected=\"selected\"";
                    glyph = "fa fa-check-circle";
                }
                if (uid.Substring(0,1) != "0")
                {
                    glyph = "";
                }
                tag += "<option value=\"" + ch.ChannelID.ToString() + "\" class=\"" + (ch.IsActive ? "" : "grey") + "\" data-icon=\"" + glyph + "\" " + sel + ">" + ch.ChannelName + "</option>";
            }
            tag += "</optgroup>";
            tag += "</select>";
            return MvcHtmlString.Create(tag);
        }

        public static MvcHtmlString HtmlMakePriceRuleTooltip(
            this HtmlHelper htmlHelper,
            PriceRule pr)
        {
            string tt = "";
            if (pr != null)
            {
                switch (pr.Lookup.LookupName)
                {
                    case "Cost Base":
                        tt = "<table><tr>" +
                        "<td width=\"100\" class=\"text-left\">Min Margin</td>" +
                        "<td class=\"text-right\">" + pr.MinMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Max Margin</td>" +
                        "<td class=\"text-right\">" + pr.MaxMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Beat Rate</td>" +
                        "<td class=\"text-right\">" + pr.BeatRateMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Cost Uplift</td>" +
                        "<td class=\"text-right\">" + pr.CostUplift.ToString("N2") + (pr.UpliftIsPc ? "%</td>" : "</td>") +
                        "</tr></table>";
                        break;
                    case "Related Product Base":
                        tt = "<table><tr>" +
                        "<td width=\"100\" class=\"text-left\">Min Margin</td>" +
                        "<td class=\"text-right\">" + pr.MinMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Max Margin</td>" +
                        "<td class=\"text-right\">" + pr.MaxMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Related Product Discount</td>" +
                        "<td class=\"text-right\">" + pr.CompatDiscountMod.ToString("N2") + "%</td>" +
                        "</tr</table>";
                        break;
                    case "Fixed Price":
                        tt = "<table><tr>" +
                        "<td width=\"100\" class=\"text-left\">Fixed Price Overide</td>" +
                        "<td class=\"text-right\">" + pr.FixedPriceOverride.ToString("N2") + "%</td>" +
                        "</tr</table>";
                        break;
                    default:
                        tt = "<table><tr>" +
                        "<td width=\"100\" class=\"text-left\">Min Margin</td>" +
                        "<td class=\"text-right\">" + pr.MinMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Max Margin</td>" +
                        "<td class=\"text-right\">" + pr.MaxMarginMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Beat Rate</td>" +
                        "<td class=\"text-right\">" + pr.BeatRateMod.ToString("N2") + "%</td>" +
                        "</tr><tr>" +
                        "<td width=\"100\" class=\"text-left\">Cost Uplift</td>" +
                        "<td class=\"text-right\">" + pr.CostUplift.ToString("N2") + (pr.UpliftIsPc ? "%</td>" : "</td>") +
                        "</tr></table>";
                        break;
                }

            }
            return MvcHtmlString.Create(tt);
        }


    }

    public static class InputExtensions
    {
        public static MvcHtmlString AutoCompleteFor<TModel, TProperty>(
            this HtmlHelper<TModel> html,
            Expression<Func<TModel, TProperty>> expression,
            string name,
            string actionName,
            string controllerName,
            string successFunction,
            string inputText)
        {
            var autocompleteUrl = UrlHelper.GenerateUrl(null, actionName, controllerName,
                                                           null,
                                                           html.RouteCollection,
                                                           html.ViewContext.RequestContext,
                                                           includeImplicitMvcValues: true);


            var value = ModelMetadata.FromLambdaExpression(
                expression, html.ViewData
            ).Model;

            var hidden = html.HiddenFor(expression);
            var hiddenId = html.IdFor(expression);

            return MvcHtmlString.Create(html.TextBox(name,
                inputText,
                new
                {
                    data_autocomplete_url = autocompleteUrl,
                    data_ajax_success = successFunction,
                    data_ajax_hiddenId = hiddenId,
                    @class = "form-control",
                    placeholder = "Type something ...."

                }).ToString()
                + hidden.ToString());
        }

        //public static MvcHtmlString TextBoxValFor<TModel, TProperty>(
        //    this HtmlHelper<TModel> html, 
        //    Expression<Func<TModel, TProperty>> expression, 
        //    object htmlAttributes)
        //{
        //    object newHtmlAttributes = new { data_val_required = "The First Name field is required.", data_val = "true" };
        //    return html.TextBoxFor(expression, newHtmlAttributes);
        //}

    }

    public static class CalculationExtensions
    {
        public static MvcHtmlString GrossMargin(
            this HtmlHelper htmlHelper,
            decimal net,
            decimal cost)
        {
            var grossMargin = "0";

            if (net > 0)
                grossMargin = Math.Round(((net - cost) / net) * 100, 2).ToString() + "%";

            return MvcHtmlString.Create(grossMargin);
        }

        public static MvcHtmlString GrossProfit(
            this HtmlHelper htmlHelper,
            decimal net,
            decimal cost)
        {
            var grossProfit = "0";

            if (net > 0)
                grossProfit = Math.Round((net - cost), 2).ToString();

            return MvcHtmlString.Create(grossProfit);
        }

        public static MvcHtmlString Difference(
            this HtmlHelper htmlHelper,
            decimal a,
            decimal b)
        {
            var diff = "0";
            diff = Math.Round(Math.Abs(a - b), 2).ToString();

            return MvcHtmlString.Create(diff);
        }

    }
}