using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.CrMS;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Web.Mvc;
using static netGiant.Intranet.BusinessLayer.ViewModels.CrMS.LookupViewModel;

namespace netGiant.Intranet.Controllers.Admin
{
    [Authorize(Roles = "IntranetAdmin")]
    public class LookupController : ApplicationController
    {
        #region Lookup
        public ActionResult LookupIndex()
        {
            return View(new LookupViewModel());
        }

        public ActionResult Lookup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new LookupViewModel();
            model.GetLookups();

            var result = model.LookupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult UpdateLookup(int id, LookupScope scope, bool isPopup = false)
        {
            var model = new LookupViewModel();
            model.Layout = SharedFunctions.ModifyForPopup(isPopup);
            model.IsPopup = isPopup;
            return View("CreateLookup", model.CreateLookup(id, scope));
        }

        public ActionResult CreateLookup(bool isPopup = false)
        {            
            var model = new LookupViewModel();
            model.Layout = SharedFunctions.ModifyForPopup(isPopup);
            model.IsPopup = isPopup;
            return View(model.CreateLookup());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public JsonResult SaveCustomerLookup(LookupViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                if (model.SaveCustomerLookup())
                {
                    sr.Message = "Lookup Saved.";
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = "Error when saving Lookup: " + e.Message;
            }
            return Json(new
            {
                savereturn = sr,
                type = "Customer Lookup"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public JsonResult SaveNgmdLookup(LookupViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                if (model.SaveNgmdLookup())
                {
                    sr.Message = "Lookup Saved.";
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = "Error when saving Lookup: " + e.Message;
            }
            return Json(new
            {
                savereturn = sr,
                type = "Ngmd Lookup"
            });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteLookup(int id, LookupScope scope)
        {
            var model = new LookupViewModel();

            var sr = model.DeleteLookup(id, scope);

            return Json(new { saveReturn = sr });
        }
        #endregion


        #region Lookup Types
        public ActionResult LookupTypeIndex()
        {
            return View(new LookupViewModel());
        }

        public ActionResult LookupType_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new LookupViewModel();
            model.GetLookupTypes();

            var result = model.LookupTypeList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult UpdateLookupType(int id, LookupScope scope)
        {
            var model = new LookupViewModel();
            return View("CreateLookupType", model.CreateLookupType(id, scope));
        }

        public ActionResult CreateLookupType()
        {
            var model = new LookupViewModel();
            return View(model.CreateLookupType());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveCustomerLookupType(LookupViewModel model)
        {
            try
            {
                if (model.SaveCustomerLookupType())
                {
                    TempData["InformationBoxFlag"] = "Lookup Saved";
                }
                return RedirectToAction("LookupTypeIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateLookupType", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveNgmdLookupType(LookupViewModel model)
        {
            try
            {
                if (model.SaveNgmdLookupType())
                {
                    TempData["InformationBoxFlag"] = "Lookup Saved";
                }
                return RedirectToAction("LookupTypeIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateLookupType", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteLookupType(int id, LookupScope scope)
        {
            var model = new LookupViewModel();

            var sr = model.DeleteLookupType(id, scope);

            return Json(new { saveReturn = sr });
        }
        #endregion
    }
}