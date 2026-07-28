using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize]
    public class WebsiteInventoryController : ApplicationController
    {
        public ActionResult WebsiteInventory()
        {
            WebsiteInventoryViewModel model = new WebsiteInventoryViewModel();
            return View("~/Views/PMS/Product/WebsiteInventory/WebsiteInventory.cshtml", model.Get());
        }

        public ActionResult WebsiteInventoryData(List<string> optionsArray)
        {
            WebsiteInventoryViewModel model = new WebsiteInventoryViewModel();
            model.Get(Convert.ToInt32(optionsArray[5]), optionsArray[0].ToString(), optionsArray[1].ToString(),
                        Convert.ToInt32(optionsArray[2]), Convert.ToInt32(optionsArray[3]), optionsArray[4].ToString());

            return PartialView("~/Views/PMS/Product/WebsiteInventory/WebsiteInventoryData.cshtml", model.webInventories);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Product/WebsiteInventory/CreateWebsiteInventory.cshtml", WebsiteInventoryViewModel.Create(id));
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

            return View("~/Views/PMS/Product/WebsiteInventory/CreateWebsiteInventory.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            WebsiteInventoryViewModel model = new WebsiteInventoryViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Web Inventory Deleted";

            return PartialView("~/Views/PMS/Product/WebsiteInventory/WebsiteInventoryData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], null, null, optionsArray[3].ToString()).webInventories);
        }
    }
}
