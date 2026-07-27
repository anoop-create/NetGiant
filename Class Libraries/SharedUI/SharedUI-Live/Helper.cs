using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;

namespace SharedUI
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString CommonCss(this HtmlHelper html)
        {
            var css = Styles.Render("~/SharedUI/Embedded/Css").ToString();
            return MvcHtmlString.Create(css);
        }
    }
}