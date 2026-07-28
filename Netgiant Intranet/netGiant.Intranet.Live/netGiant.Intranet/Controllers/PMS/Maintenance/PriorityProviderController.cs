using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class PriorityProviderController : ApplicationController
    {
        public ActionResult PriorityProviderIndex()
        {
            PriorityProviderViewModel model = new PriorityProviderViewModel();
            return View("~/Views/PMS/Maintenance/PriorityProvider/PriorityProviderIndex.cshtml", model.GetPriorityProvider()); 
        }

        public ActionResult PriorityProviderList(List<priorityProvider> model)
        {
            return PartialView("~/Views/PMS/Maintenance/PriorityProvider/PriorityProviderData.cshtml", model);
        }

        public ActionResult PriorityProviderData(string[] optionsArray)
        {
            PriorityProviderViewModel model = new PriorityProviderViewModel();
            model.GetPriorityProvider(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]));
            return PriorityProviderGetJson(model);
        }

        private ActionResult PriorityProviderGetJson(PriorityProviderViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.PriorityProviderList.Count < 50;
            jsonModel.Count = model.PriorityProviderListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/PriorityProvider/PriorityProviderData.cshtml",
                model.PriorityProviderList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }
    }
}