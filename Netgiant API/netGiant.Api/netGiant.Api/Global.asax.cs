using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using netGiant.Api.BusinessLayer.Shared;

namespace netGiant.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            Application["EquipmentManuList"] = StandardFunctions.GetEquipmentManufacturers();
        }
    }
}
