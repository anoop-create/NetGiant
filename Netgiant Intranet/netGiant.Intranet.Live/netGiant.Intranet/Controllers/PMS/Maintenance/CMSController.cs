using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class CMSController : ApplicationController
    {
        // GET: CMS
        //public ActionResult Index()
        //{
        //    return View();
        //}

        public ActionResult SectionIndex()
        {
            CMSViewModel model = new CMSViewModel();
            return View("~/Views/PMS/Maintenance/CMS/SectionIndex.cshtml", model.GetCmsSection());
        }

        public ActionResult SectionList(List<cmsSection> model)
        {
            return PartialView("~/Views/PMS/Maintenance/CMS/SectionData.cshtml", model);
        }

        public ActionResult EntryIndex()
        {
            CMSViewModel model = new CMSViewModel();
            return View("~/Views/PMS/Maintenance/CMS/EntryIndex.cshtml", model.GetCmsEntries());
        }

        public ActionResult EntriesList(List<cmsEntry> model)
        {
            return PartialView("~/Views/PMS/Maintenance/CMS/EntriesData.cshtml", model);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateSection(int id)
        {
            CMSViewModel model = new CMSViewModel();
            return View("~/Views/PMS/Maintenance/CMS/CreateSection.cshtml",
                model.CreateSection(id));
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
                return View("~/Views/PMS/Maintenance/CMS/CreateSection.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteSection(List<string> optionsArray)
        {
            CMSViewModel model = new CMSViewModel();

            model.DeleteSection(Convert.ToInt32(optionsArray[0]));
            model.GetCmsSection(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "CMS Section Deleted";

            return CmsSectionGetJson(model);
        }

        public ActionResult SectionData(string[] optionsArray)
        {
            CMSViewModel model = new CMSViewModel();

            model.GetCmsSection(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            return CmsSectionGetJson(model);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEntry(int id)
        {
            CMSViewModel model = new CMSViewModel();
            return View("~/Views/PMS/Maintenance/CMS/CreateEntry.cshtml",
                model.CreateEntry(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveEntry(CMSViewModel model, string content)
        {
            try
            {
                model.CmsEntry.cmsContent = content;
                if (model.SaveEntry())
                {
                    TempData["InformationBoxFlag"] = "CMS Entry Saved";
                }
                return RedirectToAction("EntryIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("~/Views/PMS/Maintenance/CMS/CreateEntry.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEntry(List<string> optionsArray)
        {
            CMSViewModel model = new CMSViewModel();

            model.DeleteEntry(Convert.ToInt32(optionsArray[0]));
            model.GetCmsEntries(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "CMS Entry Deleted";

            return CmsEntryGetJson(model);
        }

        public ActionResult EntryData(string[] optionsArray)
        {
            CMSViewModel model = new CMSViewModel();

            model.GetCmsEntries(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            return CmsEntryGetJson(model);
        }

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

        private ActionResult CmsSectionGetJson(CMSViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.CmsSectionList.Count < 50;
            jsonModel.Count = model.CmsSectionListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/CMS/SectionData.cshtml",
                model.CmsSectionList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        private ActionResult CmsEntryGetJson(CMSViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.CmsEntryList.Count < 50;
            jsonModel.Count = model.CmsEntryListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/CMS/EntriesData.cshtml",
                model.CmsEntryList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

    }
}