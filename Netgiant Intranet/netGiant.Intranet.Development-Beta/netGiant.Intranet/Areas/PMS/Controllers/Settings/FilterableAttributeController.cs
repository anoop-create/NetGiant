using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class FilterableAttributeController : Controller
    {
        public ActionResult Index()
        {
            var model = new FilterableAttributeViewModel();
            return View("FilterableAttributeIndex", model);
        }

        public ActionResult FilterableAttribute_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new FilterableAttributeViewModel().Get();

            var result = model.FilterableAttributeList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
    }
}
