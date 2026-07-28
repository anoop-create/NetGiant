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
//    public class OrderController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Index()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View(new OrderViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Order_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            string SearchString = "";
//            int OrderAccountFk = 0;
//            if (Request.Cookies["OrderSearchString"] != null)
//            {
//                SearchString = Request.Cookies["OrderSearchString"].Value;
//            }
//            if (Request.Cookies["OrderAccountFk"] != null)
//            {
//                if (Regex.IsMatch(Request.Cookies["OrderAccountFk"].Value, "^[0-9]+$") == true)
//                {
//                    SearchString = "";
//                    OrderAccountFk = Convert.ToInt32(Request.Cookies["OrderAccountFk"].Value);
//                }
//            }

//            var model = new OrderViewModel();
//            model.GetOrders(SearchString, OrderAccountFk);

//            var result = model.OrderList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewOrder(int id)
//        {
//            var model = new OrderViewModel();
//            model.GetOrder(id);
//            model.GetOrderLines(id);
//            model.GetVariousLookups();

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewOrderLine(int id)
//        {
//            var model = new OrderViewModel();
//            model.GetOrderLine(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditOrder(int id)
//        {
//            OrderViewModel model = new OrderViewModel();
//            //model.OrderId = id;
//            model.GetOrder(id);
//            model.GetOrderLines(id);
//            model.GetVariousLookups();

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SaveOrder(OrderViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveOrder(model);

//                return RedirectToAction("ViewOrder", "Order", new { id = model.Order.OrderId });
//            }
//            return RedirectToAction("EditOrder", "Order", new { id = model.Order.OrderId });
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditOrderLine(int id)
//        {
//            OrderViewModel model = new OrderViewModel();
//            model.GetOrderLine(id);

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SaveOrderLine(OrderViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveOrderLine(model);

//                return RedirectToAction("ViewOrderLine", "Order", new { id = model.OrderLine.OrderLineId });
//            }
//            return RedirectToAction("EditOrderLine", "Order", new { id = model.OrderLine.OrderLineId });
//        }
//    }
//}