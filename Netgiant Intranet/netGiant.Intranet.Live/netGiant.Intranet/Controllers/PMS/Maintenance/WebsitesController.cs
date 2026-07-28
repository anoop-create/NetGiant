using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class WebsitesController : ApplicationController
    {
        public ActionResult WebsiteIndex()
        {
            WebsiteViewModel model = new WebsiteViewModel();
            return View("~/Views/PMS/Maintenance/Websites/WebsiteIndex.cshtml", model.Get());
        }

        public ActionResult WebsiteData(List<string> optionsArray)
        {
            WebsiteViewModel model = new WebsiteViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Maintenance/Websites/WebsiteData.cshtml", model.websites);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/Websites/CreateWebsite.cshtml", WebsiteViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(WebsiteViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Website Saved";
                return RedirectToAction("WebsiteIndex");
            }

            return View("~/Views/PMS/Maitenance/Websites/CreateWebsite.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            WebsiteViewModel model = new WebsiteViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Website Deleted";

            return PartialView("~/Views/PMS/Maintenance/Websites/WebsiteData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).websites);
        }
    }
}
