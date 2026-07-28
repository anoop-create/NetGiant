using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Shared
{
    public class SelectListsController : ApplicationController
    {
        public JsonResult GetCategoryCodes(int id)
        {
            return Json(SelectListViewModel.GetAllCategoryCodes(id, true).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetProviderNames(int id)
        {
            return Json(SelectListViewModel.GetAllProviders(id).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEquipmentFamilies(int id)
        {
            return Json(SelectListViewModel.GetAllEquipFamilies(id).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetWebsites()
        {
            return Json(SelectListViewModel.GetAllWebsites().Select(x => x.Text).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}