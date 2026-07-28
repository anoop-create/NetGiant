using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Shared
{
    public class SelectListsController : ApplicationController
    {
        public JsonResult GetCategoryCodes(int id)
        {
            return Json(SelectListViewModel.AllCategoryCodes(id, true).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetProviderNames(int id)
        {
            return Json(SelectListViewModel.AllProviders(id).ToList(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEquipmentFamilies(int id)
        {
            return Json(SelectListViewModel.AllEquipFamilies(id).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetWebsites()
        {
            return Json(SelectListViewModel.AllWebsites().Select(x => x.Text).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}


