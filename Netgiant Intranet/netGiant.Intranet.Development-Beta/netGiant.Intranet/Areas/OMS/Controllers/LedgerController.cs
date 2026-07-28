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

//namespace netGiant.Intranet.Areas.OMS.Controllers
//{
//    [Authorize]
//    public class LedgerController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Index()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View(new LedgerViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Ledger_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            string SearchString = "";
//            if (Request.Cookies["LedgerSearchString"] != null)
//            {
//                SearchString = Request.Cookies["LedgerSearchString"].Value;
//            }

//            var model = new LedgerViewModel();
//            model.GetLedgers(SearchString);

//            var result = model.LedgerList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult LedgerLine_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            int id = 0;
//            if (Request.QueryString.Count > 0)
//            {
//                id = int.Parse(Request.QueryString["or"].ToString());
//            }
//            ViewBag.Id = id;
//            var model = new LedgerViewModel();
//            model.GetLedgerLines(id);

//            var result = model.LedgerLineList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewLedger(int id)
//        {
//            var model = new LedgerViewModel();
//            model.LedgerId = id;
//            model.GetLedger(id);
//            model.GetLedgerLines(id);
//            model.GetVariousLookups();

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewLedgerLine(int id)
//        {
//            var model = new LedgerViewModel();
//            model.GetLedgerLine(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditLedger(int id)
//        {
//            var model = new LedgerViewModel();
//            model.LedgerId = id;
//            model.GetLedger(id);
//            model.GetLedgerLines(id);
//            model.GetVariousLookups();

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SaveLedger(LedgerViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveLedger(model);

//                return RedirectToAction("ViewLedger", "Ledger", new { id = model.Ledger.LedgerId });
//            }
//            return RedirectToAction("EditLedger", "Ledger", new { id = model.Ledger.LedgerId });
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditLedgerLine(int id)
//        {
//            var model = new LedgerViewModel();
//            model.GetLedgerLine(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult SaveLedgerLine(LedgerViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveLedgerLine(model);

//                return RedirectToAction("ViewLedgerLine", "Ledger", new { id = model.LedgerLine.LedgerLineId });
//            }
//            return RedirectToAction("EditLedgerLine", "Ledger", new { id = model.LedgerLine.LedgerLineId });
//        }
//    }
//}