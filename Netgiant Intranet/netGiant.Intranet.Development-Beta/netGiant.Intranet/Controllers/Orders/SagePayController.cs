using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.Configuration;
using System.Net.Mime;
using System;

namespace netGiant.Intranet.Controllers.Orders
{
    [Authorize]
    public class SagePayController : ApplicationController
    {
        public ActionResult TransactionIndex()
        {
            var model = new SagePayViewModel();
            return View("SagePayTransactionsIndex", model); 
        }

        public ActionResult TokensIndex()
        {
            var model = new SagePayViewModel();
            return View("SagePayTokensIndex", model);
        }

        public ActionResult SagePayTransaction_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new SagePayViewModel();
            model.GetTransactions();

            var result = model.TransactionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetSagePayTransaction(long id)
        {
            var model = new SagePayViewModel();
            model.GetTransaction(id);

            return View("SagePayTransactionDetail", model);
        }

        public ActionResult SagePayToken_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new SagePayViewModel();
            model.GetTokens();

            var result = model.TokenList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult ProtxStringResponseData(int protxID)
        {
            var model = new SagePayViewModel();
            return PartialView("_ProtxStringResponseData", model.GetProtxData(protxID));
        }

        public ActionResult CardDetailsData(int id)
        {
            var model = new SagePayViewModel();
            return PartialView("_CardDetailsData", model.GetCardDetails(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportTransactions(string kendoData)
        {
            //var data = JsonConvert.DeserializeObject<IList<Telerik>>(HttpUtility.UrlDecode(kendoData));
            var model = new SagePayViewModel();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateSagePayTransactionsCSVFile();

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }
    }
}