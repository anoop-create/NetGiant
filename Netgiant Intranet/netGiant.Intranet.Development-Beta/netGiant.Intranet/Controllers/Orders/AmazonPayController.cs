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
    public class AmazonPayController : ApplicationController
    {
        public ActionResult TransactionIndex()
        {
            var model = new PayPalViewModel();
            return View("AmazonPayTransactionsIndex", model);
        }

        public ActionResult AmazonPayTransaction_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new AmazonPayViewModel();
            model.GetTransactions();

            var result = model.TransactionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetAmazonPayTransaction(string id)
        {
            var model = new AmazonPayViewModel();
            model.GetTransaction(id);

            return View("AmazonPayTransactionDetail", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportTransactions(string kendoData)
        {
            //var data = JsonConvert.DeserializeObject<IList<Telerik>>(HttpUtility.UrlDecode(kendoData));
            var model = new AmazonPayViewModel();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateAmazonPayTransactionsCSVFile();

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }
    }
}