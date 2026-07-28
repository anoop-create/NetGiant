using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.Controllers;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class PriorityProviderController : ApplicationController
    {
        public ActionResult PriorityProviderIndex()
        {
            PriorityProviderViewModel model = new PriorityProviderViewModel();
            model.GetPriorityProvider();
            return View(model); 
        }

        public ActionResult PriorityProviderAjax([DataSourceRequest] DataSourceRequest request)
        {
            PriorityProviderViewModel model = new PriorityProviderViewModel();
            model.GetPriorityProvider();

            DataSourceResult result = model.PriorityProviderList2.ToDataSourceResult(request);
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
    }
}