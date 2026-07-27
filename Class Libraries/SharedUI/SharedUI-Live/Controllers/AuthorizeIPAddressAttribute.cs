using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    //public class AuthorizeIpAddressAttribute : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext context)
    //    {
    //        string ipAddress = HttpContext.Current.Request.UserHostAddress;

    //        if (!IsIpAddressAllowed(ipAddress.Trim()))
    //        {
    //            context.Result = new HttpStatusCodeResult(403);
    //        }

    //        base.OnActionExecuting(context);
    //    }

    //    private bool IsIpAddressAllowed(string ipAddress)
    //    {
    //        if (!string.IsNullOrWhiteSpace(ipAddress))
    //        {
    //            string[] addresses = Convert.ToString(ConfigurationManager.AppSettings["AllowedIPAddresses"]).Split(',');
    //            return addresses.Where(a => a.Trim().Equals(ipAddress, StringComparison.InvariantCultureIgnoreCase)).Any();
    //        }
    //        return false;
    //    }
    //}
}