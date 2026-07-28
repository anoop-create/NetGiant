using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.DataSuppliers;
using netGiant.Intranet.Controllers;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.DataSuppliers
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class DataSupplierLookupController : ApplicationController
    {
        public ActionResult DataSupplierLookupIndex()
        {
            return View(new DataSupplierLookupViewModel());
        }

        public ActionResult DataSupplierLookup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new DataSupplierLookupViewModel().Get();

            var result = model.DataSupplierLookupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult DataSupplierLookupDetails(string id)
        {
            var model = new DataSupplierLookupViewModel();
            return View(model.Details(id));
        }
    }
}