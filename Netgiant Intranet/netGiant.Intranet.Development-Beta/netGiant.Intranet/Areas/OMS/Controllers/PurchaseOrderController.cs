//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using netGiant.Intranet.BusinessLayer.ViewModels.OMS;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using netGiant.Intranet.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Web.Mvc;
//using System.Text.RegularExpressions;

//namespace netGiant.Intranet.Areas.OMS.Controllers
//{
//    [Authorize]
//    public class PurchaseOrderController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Index()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View(new OrderViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult PurchaseOrder_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            string SearchString = "";
//            Int32 PurchaseOrderOrderFk = 0;
//            if (Request.Cookies["PurchaseOrderSearchString"] != null)
//            {
//                SearchString = Request.Cookies["PurchaseOrderSearchString"].Value;
//            }
//            if (Request.Cookies["PurchaseOrderOrderFk"] != null)
//            {
//                if (Regex.IsMatch(Request.Cookies["PurchaseOrderOrderFk"].Value, "^[0-9]+$") == true)
//                {
//                    SearchString = "";
//                    PurchaseOrderOrderFk = Convert.ToInt32(Request.Cookies["PurchaseOrderOrderFk"].Value);
//                }
//            }

//            var model = new PurchaseOrderViewModel();
//            model.GetPurchaseOrders(SearchString, PurchaseOrderOrderFk);

//            var result = model.PurchaseOrderList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewPurchaseOrder(int id)
//        {
//            var model = new PurchaseOrderViewModel();
//            model.GetPurchaseOrder(id);
//            model.GetPurchaseOrderLines(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewPurchaseOrderLine(int id)
//        {
//            var model = new PurchaseOrderViewModel();
//            model.GetPurchaseOrderLine(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditPurchaseOrder(int id)
//        {
//            var model = new PurchaseOrderViewModel();
//            model.GetPurchaseOrder(id);
//            model.GetPurchaseOrderLines(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditPurchaseOrderLine(int id)
//        {
//            var model = new PurchaseOrderViewModel();
//            model.GetPurchaseOrderLine(id);

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SavePurchaseOrder(PurchaseOrderViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SavePurchaseOrder(model);

//                return RedirectToAction("ViewPurchaseOrder", "PurchaseOrder", new { id = model.PurchaseOrder.PurchaseOrderId });
//            }
//            return RedirectToAction("EditPurchaseOrder", "PurchaseOrder", new { id = model.PurchaseOrder.PurchaseOrderId });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SavePurchaseOrderLine(PurchaseOrderViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SavePurchaseOrderLine(model);

//                return RedirectToAction("ViewPurchaseOrderLine", "PurchaseOrder", new { id = model.PurchaseOrderLine.PurchaseOrderLineId });
//            }
//            return RedirectToAction("EditPurchaseOrderLine", "PurchaseOrder", new { id = model.PurchaseOrderLine.PurchaseOrderLineId });
//        }
//    }
//}