using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.CrMS;
using System;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.CrMS
{
    [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
    public class CustomerController : ApplicationController
    {
        public ActionResult AccountIndex()
        {
            var model = new CreditAccountViewModel();
            return View(model);
        }

        public ActionResult TradeIndex()
        {
            var model = new CreditAccountViewModel();
            return View(model);
        }

        public ActionResult Account_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new CreditAccountViewModel();
            model.GetAccounts();

            var result = model.AccountList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult Trade_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new CreditAccountViewModel();
            model.GetTradeAccounts();

            var result = model.AccountList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateAccount(int id, string page)
        {
            var model = new CreditAccountViewModel();
            model.ReturnToPage = page;
            return View(model.CreateAccount(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult ViewAccount(int id)
        {
            var model = new CreditAccountViewModel();
            return View(model.GetAccountDetail(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveAccount(CreditAccountViewModel model)
        {
            try
            {
                if (model.SaveAccountEntry())
                {
                    TempData["InformationBoxFlag"] = "Account Entry Saved";
                }
                return RedirectToAction(model.ReturnToPage, "Customer");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateAccount", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteEntry(int id)
        {
            var model = new CreditAccountViewModel();

            var sr = model.DeleteAccount(id);

            return Json(new { saveReturn = sr });
        }
    }
}