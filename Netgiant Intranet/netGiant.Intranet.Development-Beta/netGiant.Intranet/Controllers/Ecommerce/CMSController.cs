using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Ecommerce
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class CMSController : Controller
    {
        #region CMS Entries
        public ActionResult EntryIndex()
        {
            Session["ReturnAction"] = "EntryIndex";
            Session["ReturnController"] = "CMS";
            return View(new CMSViewModel());
        }

        public ActionResult CMSEntry_Read([DataSourceRequest] DataSourceRequest request)
        {
            CMSViewModel model = new CMSViewModel();
            model.GetEntryList();

            var result = model.EntryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEntry(int id)
        {
            return View(new CMSViewModel().CreateEntry(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveEntry(CMSViewModel model, string content)
        {
            try
            {
                model.CMSEntry.cmsContent = content;
                if (model.SaveEntry())
                {
                    TempData["InformationBoxFlag"] = "CMS Entry Saved";
                }
                return RedirectToAction(Session["ReturnAction"].ToString(), Session["ReturnController"].ToString());
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("CreateEntry", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEntry(int id)
        {
            CMSViewModel model = new CMSViewModel();

            SaveReturn sr = model.DeleteEntry(id);

            return Json(new { saveReturn = sr });
        }
        #endregion

        #region CMS Sections
        public ActionResult SectionIndex()
        {
            return View(new CMSViewModel());
        }

        public ActionResult CMSSection_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new CMSViewModel();
            model.GetSectionList();

            var result = model.SectionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateSection(int id)
        {
            return View(new CMSViewModel().CreateSection(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveSection(CMSViewModel model)
        {
            try
            {
                if (model.SaveSection())
                {
                    TempData["InformationBoxFlag"] = "CMS Section Saved";
                }
                return RedirectToAction("SectionIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("CreateSection", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteSection(int id)
        {
            var model = new CMSViewModel();

            SaveReturn sr = model.DeleteSection(id);

            return Json(new { saveReturn = sr });
        }
        #endregion

        #region CMS Events
        public ActionResult EventIndex()
        {
            Session["ReturnAction"] = "EventIndex";
            Session["ReturnController"] = "CMS";
            return View(new CMSViewModel());
        }

        public ActionResult Event_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new CMSViewModel().GetEvents();

            var result = model.EventList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEvent(int id, bool isPopup = true)
        {
            ViewBag.IsPopup = isPopup;
            CMSViewModel model = new CMSViewModel();
            model.Layout = SharedFunctions.ModifyForPopup(isPopup);
            model.CreateEvent(id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public JsonResult SaveEvent(CMSViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                sr = model.SaveEvent();
                if (sr.IsSuccess)
                {
                    sr.Message = "Event Saved.";
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = "Error when saving Event: " + e.Message;
            }
            return Json(new
            {
                savereturn = sr,
                type = "Event"
            });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEvent(int id)
        {
            CMSViewModel model = new CMSViewModel();

            SaveReturn sr = model.DeleteEvent(id);

            return Json(new { saveReturn = sr });
        }
        #endregion
        
        #region CMS Event Mappings
        public ActionResult EventData_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new CMSViewModel().GetEventData();

            var result = model.EventDataList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        } 

        public ActionResult EventMappingIndex()
        {
            Session["ReturnAction"] = "EventMappingIndex";
            Session["ReturnController"] = "CMS";
            return View(new CMSViewModel());
        }

        public ActionResult EventMapping_Read([DataSourceRequest] DataSourceRequest request, int id = 0)
        {
            var model = new CMSViewModel().GetEventMappings(id);

            var result = model.EventMappingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEventMapping(int id, int cmsEntryId, bool isPopup = true)
        {
            
            CMSViewModel model = new CMSViewModel();
            model.Layout = SharedFunctions.ModifyForPopup(isPopup);
            model.IsPopup = isPopup;
            model.CreateEventMapping(id, cmsEntryId);
            return View(model);
        }        

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public JsonResult SaveEventMapping(CMSViewModel model, string content)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                sr = model.SaveEventMapping();
                if (sr.IsSuccess)
                {
                    sr.Message = "Event Mapping Saved.";
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = "Error when saving Event Mapping: " + e.Message;
            }
            return Json(new
            {
                savereturn = sr,
                type = "Event Mapping"
            });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEventMapping(int id)
        {
            CMSViewModel model = new CMSViewModel();

            SaveReturn sr = model.DeleteEventMapping(id);

            return Json(new { saveReturn = sr });
        }
        #endregion

        #region Utilities
        public JsonResult GetSectionDropDown(int? websiteId = null)
        {
            string sectionHtml = "<option value=\"\">Select ...</option>";
            if (websiteId != null)
            {
                CMSViewModel model = new CMSViewModel();
                List<SelectListItem> sList = model.GetSectionNames(websiteId);
                foreach (SelectListItem li in sList)
                {
                    sectionHtml += "<option value=\"" + li.Value.ToString() + "\" >" + li.Text + "</option>";
                }
            }

            return Json(new
            {
                SectionHtml = sectionHtml
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetEntriesDropDown(int? sectionId = null)
        {
            string entriesHtml = "<option value=\"\">Select ...</option>";
            if (sectionId != null)
            {
                CMSViewModel model = new CMSViewModel();
                List<SelectListItem> eList = model.GetEntryNames(sectionId);
                foreach (SelectListItem li in eList)
                {
                    entriesHtml += "<option value=\"" + li.Value.ToString() + "\" >" + li.Text + "</option>";
                }
            }

            return Json(new
            {
                EntriesHtml = entriesHtml
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}