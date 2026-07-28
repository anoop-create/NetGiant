using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Threading.Tasks;
using System;

namespace netGiant.Intranet.Controllers
{
    [Authorize(Roles="IntranetAdmin")]
    public class AdminController : ApplicationController
    {
        public ActionResult Index()
        {
            AdminViewModel model = new AdminViewModel();
            
            return View(model.Get());
        }

        [Authorize]
        public ActionResult ListMenuItems()
        {
            MenuViewModel model = new MenuViewModel();
            model.Get();

            return View("~/Views/Admin/ListMenuItems.cshtml", model.Get());
        }

        public JsonResult GetParentMenuItems(int id)
        {
            MenuViewModel model = new MenuViewModel();

            model.GetParentMenuItems(id - 1);
            return Json(model.ParentMenuItems, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult CreateMenuItem()
        {
            MenuViewModel model = new MenuViewModel();
            model.GetMenuDetails(0);
            ViewBag.Title = "New Menu Item";
            ViewBag.SubTitle = "Create a new menu item";

            return View("~/Views/Admin/CreateMenuItem.cshtml", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateMenuItem(MenuViewModel model)
        {
            return updateMenu(model, true);
        }

        [Authorize]
        public ActionResult UpdateMenuItem(int id)
        {
            MenuViewModel model = new MenuViewModel();
            model.GetMenuDetails(id);
            ViewBag.Title = "Update Menu Item";
            ViewBag.SubTitle = "Make changes to a menu item";

            return View("~/Views/Admin/CreateMenuItem.cshtml", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdateMenuItem(MenuViewModel model)
        {
            return updateMenu(model, true);
        }

        private ActionResult updateMenu(MenuViewModel model, bool update)
        {
            if (ModelState.IsValid)
            {
                bool updated = false;
                updated = model.SaveMenuItem(model);

                if (updated == true)
                {
                    TempData["InformationBoxFlag"] = "Menu Saved";
                }

                return RedirectToAction("ListMenuItems");
            }

            if (update == true)
            {
                model.GetMenuDetails(model.ActionLink.actionLinkID);
                ViewBag.Title = "Update Menu Item";
                ViewBag.SubTitle = "Make changes to a menu item";
            }
            else
            {
                model.GetMenuDetails(0);
                ViewBag.Title = "New Menu Item";
                ViewBag.SubTitle = "Create a new menu item";
            }

            return View("~/Views/Admin/CreateMenuItem.cshtml", model);
        }

        [HttpPost]
        public ActionResult DeleteMenuItem(List<string> optionsArray)
        {
            MenuViewModel mVm = new MenuViewModel();
            bool success = mVm.DeleteMenuItem(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Menu Deleted";
            }

            mVm.Get();
            return PartialView("~/Views/Admin/ListMenuItemsData.cshtml", mVm.MenuItems);
        }

        public ActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}