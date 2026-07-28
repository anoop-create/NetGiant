using DP001Website.Models;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace DP001Website
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new CustomErrorHandling());
            filters.Add(new HandleErrorAttribute());

            if (ConfigurationManager.AppSettings["Environment"] == "Live")
                filters.Add(new RequireHttpsAttribute());
        }
    }
}
