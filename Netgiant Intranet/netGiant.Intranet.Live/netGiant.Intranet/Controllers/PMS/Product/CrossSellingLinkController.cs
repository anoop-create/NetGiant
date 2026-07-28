using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize]
    public class CrossSellingLinkController : Controller
    {
        public ActionResult Links()
        {
            CrossSellingLinkViewModel model = new CrossSellingLinkViewModel();
            return View("~/Views/PMS/Product/CrossSellingLink/CrossSellingLink.cshtml", model.Get());
        }

        public ActionResult LinksData(string[] optionsArray)
        {
            CrossSellingLinkViewModel model = new CrossSellingLinkViewModel();
            return PartialView("~/Views/PMS/Product/CrossSellingLink/CrossSellingLinkData.cshtml",
                model.Get(Convert.ToInt32(optionsArray[4]), optionsArray[2],
                optionsArray[1], optionsArray[3], Convert.ToInt32(optionsArray[0])).CrossSellingLinkList);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Product/CrossSellingLink/CreateCrossSellingLink.cshtml", 
                 CrossSellingLinkViewModel.Create(id));
        }

        public JsonResult GetProducts(string searchTerm)
        {
            return Json(SelectListViewModel.GetProductsArray(searchTerm).ToList(), JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CrossSellingLinkViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
            }

            return RedirectToAction("Links");
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(string[] optionsArray)
        {
            CrossSellingLinkViewModel model = new CrossSellingLinkViewModel();
            model.Delete(Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "Cross Selling Link Deleted";

            return PartialView("~/Views/PMS/Product/CrossSellingLink/CrossSellingLinkData.cshtml",
                model.Get(1, optionsArray[2],
                optionsArray[1], optionsArray[3], Convert.ToInt32(optionsArray[0])).CrossSellingLinkList);
        }
    }
}