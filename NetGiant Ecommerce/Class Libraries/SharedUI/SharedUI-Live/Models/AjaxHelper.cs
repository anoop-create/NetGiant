using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace SharedUI.Models
{
    public static class AjaxExtensions
    {
        public static MvcHtmlString ButtonLink(
            this AjaxHelper ajaxHelper,
            string linkText,
            string actionName,
            string controllerName,
            AjaxOptions ajaxOptions,
            object htmlAttributes)
        {
            var button = new TagBuilder("button");
            var anchor = new TagBuilder("a");
            var attributes = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);
            var urlHelper = new UrlHelper(ajaxHelper.ViewContext.RequestContext);

            foreach (var att in attributes)
            {
                button.Attributes[att.Key] = att.Value.ToString();
            }
            button.InnerHtml = linkText;

            anchor.MergeAttributes((ajaxOptions ?? new AjaxOptions()).ToUnobtrusiveHtmlAttributes());
            anchor.Attributes["href"] = urlHelper.Action(actionName, controllerName);
            anchor.InnerHtml = button.ToString();

            return MvcHtmlString.Create(anchor.ToString());
        }

        public static MvcHtmlString HtmlActionLink(
            this AjaxHelper ajaxHelper,
            string linkText,
            string actionName,
            string controllerName,
            object routeValues,
            AjaxOptions ajaxOptions,
            object htmlAttributes)
        {
            var tagActionLink = ajaxHelper.ActionLink("[replace]", actionName, controllerName, routeValues, ajaxOptions, htmlAttributes).ToHtmlString();
            return MvcHtmlString.Create(tagActionLink.Replace("[replace]", linkText));
        }
    }
}
