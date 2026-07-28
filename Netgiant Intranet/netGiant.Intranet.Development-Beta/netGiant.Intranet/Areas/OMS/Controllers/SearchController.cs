//using Kendo.Mvc.Extensions;
//using Kendo.Mvc.UI;
//using netGiant.Intranet.BusinessLayer.ViewModels.OMS;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using System;
//using System.Collections.Generic;
//using System.Web.Mvc;
//using netGiant.Intranet.Models;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using System.Linq.Expressions;
//using System.Web;

//namespace netGiant.Intranet.Areas.OMS.Controllers
//{
//    [Authorize]
//    public class SearchController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Index()
//        {
//            return View(new SearchViewModel());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Search(SearchViewModel model)
//        {
//            if (!string.IsNullOrEmpty(model.AccountSearchAll))
//            {
//                HttpCookie MyCookie = new HttpCookie("AccountSearchString");
//                MyCookie.Value = model.AccountSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(1);
//                Response.Cookies.Add(MyCookie);

//                return RedirectToAction("Index", "Acc");
//            }

//            if (!string.IsNullOrEmpty(model.OrderSearchAll))
//            {
//                HttpCookie MyCookie = new HttpCookie("OrderSearchString");
//                MyCookie.Value = model.OrderSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(1);
//                Response.Cookies.Add(MyCookie);

//                MyCookie = new HttpCookie("OrderAccountFk");
//                MyCookie.Value = model.OrderSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(-10);
//                Response.Cookies.Add(MyCookie);

//                return RedirectToAction("Index", "Order");
//            }

//            if (!string.IsNullOrEmpty(model.PurchaseOrderSearchAll))
//            {
//                HttpCookie MyCookie = new HttpCookie("PurchaseOrderSearchString");
//                MyCookie.Value = model.PurchaseOrderSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(1);
//                Response.Cookies.Add(MyCookie);

//                MyCookie = new HttpCookie("PurchaseOrderOrderFk");
//                MyCookie.Value = model.OrderSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(-10);
//                Response.Cookies.Add(MyCookie);

//                return RedirectToAction("Index", "PurchaseOrder");
//            }

//            if (!string.IsNullOrEmpty(model.LedgerSearchAll))
//            {
//                HttpCookie MyCookie = new HttpCookie("LedgerSearchString");
//                MyCookie.Value = model.LedgerSearchAll;
//                MyCookie.Expires = DateTime.Now.AddHours(1);
//                Response.Cookies.Add(MyCookie);

//                return RedirectToAction("Index", "Ledger");
//            }

//            return RedirectToAction("Index", "Search");
//        }
//    }
//}