//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using netGiant.Intranet.BusinessLayer.ViewModels.OMS;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using netGiant.Intranet.Models;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Web.Mvc;

//namespace netGiant.Intranet.Areas.OMS.Controllers
//{
//    [Authorize]
//    public class MonitorController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SO()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View("SalesOrder", new MonitorViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SOP()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View("SalesOrderPurchasing", new MonitorViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult OOP()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View("OnlineOrderProcessing", new MonitorViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SO_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            var model = new MonitorViewModel();
//            model.GetSO();

//            var result = model.SOList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SOP_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            var model = new MonitorViewModel();
//            model.GetSOP();

//            var result = model.SOPList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult OOP_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            var model = new MonitorViewModel();
//            model.GetOOP();

//            var result = model.OOPList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult CreateDirects()
//        {
//            return PartialView(new MonitorViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult GetSupplierOptions(int id)
//        {
//            var model = new MonitorViewModel();
//            model.GetSupplierOptions(id);
//            ViewBag.OrderId = id;

//            return PartialView("SupplierOptions", model);
//        }
//    }

//}