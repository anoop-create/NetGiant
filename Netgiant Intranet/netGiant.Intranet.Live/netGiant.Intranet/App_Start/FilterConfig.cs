using System;
using System.Web;
using System.Web.Management;
using System.Web.Mvc;
using netGiant.Intranet.Models;

namespace netGiant.Intranet
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new CustomErrorHandling());
            filters.Add(new HandleErrorAttribute());
        }
    }
}
