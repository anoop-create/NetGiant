using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class FilterableAttributeController : ApplicationController
    {
        public ActionResult Index()
        {
            FilterableAttributeViewModel faVm = new FilterableAttributeViewModel();
            return View("~/Views/PMS/Maintenance/FilterableAttribute/FilterableAttributeIndex.cshtml",
                        faVm.Get(1, null, null, null).filterableAttributesList);
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            FilterableAttributeViewModel faVm = new FilterableAttributeViewModel();

            return PartialView("~/Views/PMS/Maintenance/FilterableAttribute/FilterableAttributeData.cshtml",
                                faVm.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0], optionsArray[1], 
                                optionsArray[2]).filterableAttributesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            FilterableAttributeViewModel faVm = new FilterableAttributeViewModel();
            return View("~/Views/PMS/Maintenance/FilterableAttribute/CreateFilterableAttribute.cshtml", faVm.Create(id));
        }

    }
}
