using DP001BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class LookupController : ApplicationController
    {
        private LookupViewModel model;

        // GET: Lookup
        public ActionResult Index()
        {
            model = new LookupViewModel();
            model.GetLookups();
            model.GetLookupTypes();
            //ViewBag.MvcGridAddRow1 = new HtmlString("<a href=\"/Lookup/New\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Lookup</button></a>");
            //ViewBag.MvcGridAddRow2 = new HtmlString("<a href=\"/Lookup/NewType\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add LookupType</button></a>");

            return View(model);
        }

        public ActionResult New()
        {
            var model = new LookupViewModel();
            model.New();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult Create(LookupViewModel model)
        {
            var saveReturn = model.Create(model.LookupEntry);

            if (saveReturn.IsSuccess)
            {
                return Json(new
                {
                    IsSuccess = true,
                    Id = model.LookupEntry.LookupID,
                    Action = "Save",
                    Msg = ""
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                //var newModel = new LookupViewModel();
                //newModel.New();
                //newModel.LookupEntry = model.LookupEntry;

                //ModelState.AddModelError("", saveReturn.EntityValidationError);
                //ModelState.AddModelError("", saveReturn.Message);

                //return View("New", newModel);
                return Json(new
                {
                    IsSuccess = false,
                    Id = model.LookupEntry.LookupID,
                    Action = "Save",
                    Msg = saveReturn.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}