using Newtonsoft.Json;
using System.Text;
using System.Web.Mvc;

namespace netGiant.Intranet.Models
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
        public static MvcHtmlString DisplayJson(
            this HtmlHelper htmlHelper,
            string jsonString)
        {
            //string ret = JsonConvert.SerializeObject(jsonString, Formatting.Indented);
            //string ret = "<div>" + jsonString.Replace(",", ",</div><div>").Replace("{", "{<div class=\"g-m-l-10\">").Replace("}", "}</div>") + "</div>";
            return MvcHtmlString.Create(jsonString);
        }
    }
}