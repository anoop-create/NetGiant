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
    public class PayPalController : ApplicationController
    {
        public ActionResult TransactionIndex()
        {
            var model = new PayPalViewModel();
            return View("PayPalTransactionsIndex", model);
        }

        public ActionResult PayPalTransaction_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new PayPalViewModel();
            model.GetTransactions();

            var result = model.TransactionList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult GetPayPalTransaction(string id)
        {
            var model = new PayPalViewModel();
            model.GetTransaction(id);

            return View("PayPalTransactionDetail", model);
        }

        //public ActionResult ProtxStringResponseData(int protxID)
        //{
        //    var model = new SagePayViewModel();
        //    return PartialView("_ProtxStringResponseData", model.GetProtxData(protxID));
        //}

        //public ActionResult CardDetailsData(int id)
        //{
        //    var model = new SagePayViewModel();
        //    return PartialView("_CardDetailsData", model.GetCardDetails(id));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public FileResult ExportTransactions(string kendoData)
        {
            //var data = JsonConvert.DeserializeObject<IList<Telerik>>(HttpUtility.UrlDecode(kendoData));
            var model = new PayPalViewModel();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreatePayPalTransactionsCSVFile();

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }
    }
}