using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.PromotionalGroup;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Product
{
    public class PromotionalGroupController : Controller
    {
        // GET: PromotionalGroup
        public ActionResult Index()
        {
            var model = new PromotionalGroupViewModel();
            return View("PromotionalGroup", model);
        }

        public ActionResult PromotionalGroup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PromotionalGroupViewModel();
            model.GetPromotionalGroups();

            var result = model.PromotionalGroupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult CreatePromotionalGroup(string name, string filter)
        {
            var model = new PromotionalGroupViewModel();
            var sr = model.CreatePromotionalGroup(name.Replace(" ", ""), filter);

            return Json(new
            {
                saveReturn = sr
            });
        }

        public JsonResult UpdatedPromotionalGroup(int id, string name, string filter)
        {
            var model = new PromotionalGroupViewModel();
            var sr = model.UpdatePromotionalGroup(id, name.Replace(" ", ""), filter);

            return Json(new
            {
                saveReturn = sr
            });
        }

        public JsonResult DeletePromotionalGroup(int id)
        {
            var model = new PromotionalGroupViewModel();
            var sr = model.DeletePromotionalGroup(id);

            return Json(new
            {
                saveReturn = sr
            });
        }

        public JsonResult SetPromotionalGroupActive(int id)
        {
            var model = new PromotionalGroupViewModel();
            var sr = model.SetPromotionalGroupActive(id);

            return Json(new
            {
                saveReturn = sr
            });
        }
    }
}