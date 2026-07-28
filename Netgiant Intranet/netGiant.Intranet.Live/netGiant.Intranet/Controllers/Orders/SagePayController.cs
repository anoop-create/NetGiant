using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using netGiant.Intranet.DataLayer;
using System.IO;
using netGiant.Intranet.Models;

namespace netGiant.Intranet.Controllers.Orders
{
    [Authorize]
    public class SagePayController : ApplicationController
    {
        // Transaction ActionResult's
        public ActionResult TransactionIndex()
        {
            SagePayViewModel model = new SagePayViewModel();
            return View("~/Views/Orders/SagePayTransactionsIndex.cshtml", model.GetSagePayTransactionsData()); 
        }

        public ActionResult SagePayTransactionList(List<SagePayTransactions> model)
        {
            return PartialView("~/Views/Orders/SagePayTransactionData.cshtml", model);
        }

        public ActionResult TransactionIndexData(string[] optionsArray)
        {
            DateTime dtTo = Convert.ToDateTime(optionsArray[6]);
            dtTo = dtTo.Date + new TimeSpan(23, 59, 59);

            SagePayViewModel model = new SagePayViewModel();
            model.GetSagePayTransactionsData(optionsArray[0].ToString(), optionsArray[1].ToString(), Convert.ToInt32(optionsArray[2]),optionsArray[3].ToString(),
                optionsArray[4].ToString(), Convert.ToDateTime(optionsArray[5]), dtTo, Convert.ToInt32(optionsArray[7]));
            return TransactionsGetJson(model);
        }

        public ActionResult ProtxStringResponseData(int protxID)
        {
            SagePayViewModel model = new SagePayViewModel();
            return PartialView("~/Views/Orders/ProtxStringResponseData.cshtml", 
                model.GetProtxData(protxID));
        }

        private ActionResult TransactionsGetJson(SagePayViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.TransactionsList.Count < 50;
            jsonModel.Count = model.TransactionsListCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/Orders/SagePayTransactionData.cshtml",
                model.TransactionsList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        // Tokens ActionResult's
        public ActionResult TokensIndex()
        {
            SagePayViewModel model = new SagePayViewModel();
            return View("~/Views/Orders/SagePayTokensIndex.cshtml", model.GetSagePayTokensData()); 
        }

        public ActionResult SagePayTokensList(List<SagePayTokens> model)
        {
            return PartialView("~/Views/Orders/SagePayTokensData.cshtml", model);
        }

        public ActionResult TokensIndexData(string[] optionsArray)
        {
            DateTime dtTo = Convert.ToDateTime(optionsArray[6]);
            dtTo = dtTo.Date + new TimeSpan(23, 59, 59);

            SagePayViewModel model = new SagePayViewModel();
            model.GetSagePayTokensData(optionsArray[0].ToString(), Convert.ToInt32(optionsArray[1]),optionsArray[2].ToString(), optionsArray[3].ToString(),
                Convert.ToBoolean(optionsArray[4]), Convert.ToDateTime(optionsArray[5]), dtTo, Convert.ToInt32(optionsArray[7]));
            return TokensGetJson(model);
        }

        public ActionResult CardDetailsData(int id)
        {
            SagePayViewModel model = new SagePayViewModel();
            return PartialView("~/Views/Orders/CardDetailsData.cshtml"
                , model.GetCardDetails(id));
        }

        private ActionResult TokensGetJson(SagePayViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.TokensList.Count < 50;
            jsonModel.Count = model.TokensListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/Orders/SagePayTokensData.cshtml",
                model.TokensList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }
    }
}