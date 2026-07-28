using DP001BusinessLogic;
using DP001BusinessLogic.Shared;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using DP001Website.Models;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    public class ProviderExclusionController : ApplicationController
    {
        private ProviderExclusionViewModel model;

        public ActionResult Index()
        {
            return View(model);
        }

        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            model = new ProviderExclusionViewModel(GetChannelId());
            model.GetExclusions();

            var result = model.ProviderExclusionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult Create(int competitorId, string brandName, string manuPartNo, string clientProductId, int inventoryId, string description)
        {
            ProviderExclusionViewModel model = new ProviderExclusionViewModel(GetChannelId());
            ProviderExclusion pe = new ProviderExclusion();
            model.Channel = GetChannel();
            pe.ChannelFK = model.Channel.ChannelID;
            CrudLookup crudLookup = new CrudLookup();
            pe.FileTypeFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory").FirstOrDefault().LookupID;
            pe.ExclusionTypeFk = crudLookup.Read(x => x.LookupType.LookupTypeName == "ProviderExclusionType" && x.LookupName == "Item").FirstOrDefault().LookupID;
            pe.ProviderFK = competitorId;
            pe.BrandName = brandName;
            pe.ManufacturerPartNo = manuPartNo;
            pe.ClientProductID = clientProductId;
            pe.Comment = description;
            model.ProviderExclusionEntry = pe;
            model.InventoryId = inventoryId;

            SaveReturn saveReturn = model.Create();
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = competitorId, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = competitorId, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult CreateNewExclusion(int competitorId, string[] brandNames, int exclusionTypeId, int[] inventoryIds)
        {
            var errorOccured = false;
            var errorMessage = "";
            model = new ProviderExclusionViewModel();

            if (brandNames != null)
            {
                foreach (var brandName in brandNames)
                {
                    model.ProviderExclusionEntry = new ProviderExclusion
                    {
                        ChannelFK = GetChannelId(),
                        ExclusionTypeFk = exclusionTypeId
                    };
                    var crudLookup = new CrudLookup();
                    var crudCompetitorInventory = new CrudCompetitorInventory();
                    model.ProviderExclusionEntry.FileTypeFK = crudLookup
                        .Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory")
                        .FirstOrDefault()
                        .LookupID;

                    model.ProviderExclusionEntry.ProviderFK = competitorId;
                    model.ProviderExclusionEntry.BrandName = brandName;

                    var saveReturn = model.Create();
                    if (!saveReturn.IsSuccess)
                    {
                        errorOccured = true;
                        errorMessage = saveReturn.Message;
                        break;
                    }
                }
            }

            if (inventoryIds != null)
            {
                foreach (var inventoryId in inventoryIds)
                {
                    model.ProviderExclusionEntry = new ProviderExclusion
                    {
                        ChannelFK = GetChannelId(),
                        ExclusionTypeFk = exclusionTypeId
                    };
                    var crudLookup = new CrudLookup();
                    var crudCompetitorInventory = new CrudCompetitorInventory();
                    model.ProviderExclusionEntry.FileTypeFK = crudLookup
                        .Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory")
                        .FirstOrDefault()
                        .LookupID;

                    var compInventoryRecord = crudCompetitorInventory
                        .Read(x => x.ChannelFK == model.ProviderExclusionEntry.ChannelFK &&
                                   x.CompetitorInventoryID == inventoryId)
                        .FirstOrDefault();

                    if (compInventoryRecord != null)
                    {
                        model.ProviderExclusionEntry.ProviderFK = competitorId;
                        model.ProviderExclusionEntry.BrandName = compInventoryRecord.Brand.BrandName;
                        model.ProviderExclusionEntry.ManufacturerPartNo = compInventoryRecord.ManufacturerPartNo;
                        model.ProviderExclusionEntry.ClientProductID = compInventoryRecord.ClientProductID;
                        model.ProviderExclusionEntry.Comment = compInventoryRecord.Description;
                    }

                    var saveReturn = model.Create();
                    if (!saveReturn.IsSuccess)
                    {
                        errorOccured = true;
                        errorMessage = saveReturn.Message;
                        break;
                    }
                }
            }

            return Json(!errorOccured ? new { isSuccess = true, action = "Save", msg = "" } : new { isSuccess = false, action = "Save", msg = errorMessage }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public ActionResult UpdateExclusion(ProviderExclusionViewModel model)
        {
            model.Update(model.ProviderExclusionEntry);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            int channelId = GetChannelId();
            var model = new ProviderExclusionViewModel(channelId);
            var sr = model.Delete(id);

            return Json(new
            {
                isSuccess = sr.IsSuccess,
                msg = sr.Message,
                id = id

            }
            , JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult DeleteMultiple(int[] ids)
        {
            var errorOccured = false;
            var errorMessage = "";

            foreach (var id in ids)
            {
                int channelId = GetChannelId();
                model = new ProviderExclusionViewModel(channelId);
                var sr = model.Delete(id);
                if (sr.IsSuccess) continue;
                errorOccured = true;
                errorMessage = sr.Message;
                break;
            }
            
            return Json(new
                {
                    isSuccess = !errorOccured,
                    msg = errorMessage

                }
                , JsonRequestBehavior.AllowGet);
        }

        [CheckUserPermission(FieldName = "ProviderBrandExclusion", Check = TenantPermissonCheck.IsFeatureOn)]
        public ActionResult New()
        {
            model = new ProviderExclusionViewModel(GetChannelId());
            model.New();

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            model = new ProviderExclusionViewModel(GetChannelId());
            model.Edit(id);

            return View(model);
        }

        public JsonResult SearchCompetitors(string term)
        {
            model = new ProviderExclusionViewModel(GetChannelId());
            var results = model.SearchCompetitors(term).SearchCompetitorsResults
                .Select(x => new
                {
                    Des = x.CompetitorName,
                    Id = x.CompetitorID
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchBrands(string term)
        {
            model = new ProviderExclusionViewModel(GetChannelId());
            var results = model.SearchBrands(term).SearchBrandsResults
                .Select(x => new
                {
                    Des = x.BrandName,
                    Id = x.BrandName
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }
    }
}
