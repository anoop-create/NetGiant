using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Product
{
    [Authorize]
    public class WebsiteInventoryController : Controller
    {
        public ActionResult WebsiteInventory()
        {
            return View(new WebsiteInventoryViewModel());
        }

        public ActionResult WebsiteInventory_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new WebsiteInventoryViewModel().Get();

            var result = model.WebsiteInventoryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("CreateWebsiteInventory", WebsiteInventoryViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Save(WebsiteInventoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Web Inventory Saved";
                return RedirectToAction("WebsiteInventory");
            }

            return View("CreateWebsiteInventory", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new WebsiteInventoryViewModel().Delete(id) });
        }
    }
}