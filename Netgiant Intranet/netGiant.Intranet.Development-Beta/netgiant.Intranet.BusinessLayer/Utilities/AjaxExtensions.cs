using System.Web.Mvc;
using System.Web.Mvc.Ajax;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public static class AjaxExtensions
    {
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
