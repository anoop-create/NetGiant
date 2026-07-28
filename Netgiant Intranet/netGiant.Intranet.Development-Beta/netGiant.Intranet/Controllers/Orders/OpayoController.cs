using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Orders
{
    [Authorize]
    public class OpayoController : ApplicationController
    {
        public ActionResult TransactionIndex()
        {
            var model = new OpayoViewModel();
            return View("OpayoTransactionsIndex", model);
        }

        public ActionResult OpayoTransaction_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new OpayoViewModel();
            model.GetTransactions();

            var result = model.TransactionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetOpayoTransaction(int id, string merchantSessionKey)
        {
            var model = new OpayoViewModel();
            model.GetTransaction(id, merchantSessionKey);

            return View("OpayoTransactionDetail", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportTransactions(string kendoData)
        {
            var model = new OpayoViewModel();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateOpayoTransactionsCSVFile();

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        public ActionResult TokensIndex()
        {
            var model = new OpayoViewModel();
            return View("OpayoTokensIndex", model);
        }

        public ActionResult OpayoToken_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new OpayoViewModel();
            model.GetTokens();

            var result = model.TokenList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        //public ActionResult ProtxStringResponseData(int protxID)
        //{
        //    var model = new OpayoViewModel();
        //    return PartialView("_ProtxStringResponseData", model.GetProtxData(protxID));
        //}

        public ActionResult CardDetailsData(int id)
        {
            var model = new OpayoViewModel();
            return PartialView("_CardDetailsData", model.GetCardDetails(id));
        }
    }
}