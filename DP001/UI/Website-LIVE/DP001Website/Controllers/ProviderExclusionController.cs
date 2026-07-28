using DP001BusinessLogic;
using DP001BusinessLogic.Shared;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    public class ProviderExclusionController : ApplicationController
    {
        private ProviderExclusionViewModel model;
        public ActionResult Index()
        {
            int channelId = GetChannelId();
            model = new ProviderExclusionViewModel(channelId);
            model.GetExclusions();

            //ViewBag.tenantId = GetTenant().TenantID;
            //ViewBag.channelId = channelId;

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public JsonResult Create(int competitorId, string brandName, string manuPartNo, string clientProductId, int inventoryId)
        {
            ProviderExclusionViewModel model = new ProviderExclusionViewModel(GetChannelId());
            ProviderExclusion pe = new ProviderExclusion();
            model.Channel = GetChannel();
            pe.ChannelFK = model.Channel.ChannelID;
            CrudLookup crudLookup = new CrudLookup();
            pe.FileTypeFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory").FirstOrDefault().LookupID;
            pe.ProviderFK = competitorId;
            pe.BrandName = brandName;
            pe.ManufacturerPartNo = manuPartNo;
            pe.ClientProductID = clientProductId;
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
    }    
}