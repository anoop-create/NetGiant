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
//using System.Data.Entity;
//using System.Data.Entity.Validation;
//using netGiant.Intranet.BusinessLayer.Utilities;

//namespace netGiant.Intranet.Areas.OMS.Controllers
//{
//    [Authorize]
//    public class AccController : Controller
//    {
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Index()
//        {
//            ViewBag.Query = Request.QueryString.ToRouteValues();
//            return View(new AccViewModel());
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult Account_Read([DataSourceRequest] DataSourceRequest request)
//        {
//            string SearchString = "";
//            if (Request.Cookies["AccountSearchString"] != null)
//            {
//                SearchString = Request.Cookies["AccountSearchString"].Value;
//            }

//            var model = new AccViewModel();
//            model.GetAccounts(SearchString);

//            var result = model.AccountList.ToDataSourceResult(request);
//            var jsonResult = Json(result);
//            jsonResult.MaxJsonLength = int.MaxValue;
//            return jsonResult;
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewAccount(int id)
//        {
//            var model = new AccViewModel();
//            model.GetAccount(id);
//            model.GetContacts(x => x.AccountFk == id);
//            model.GetAddresses(x => x.AccountFk == id);
//            model.GetVariousLookups();

//            model.Account.CustomerNotes = HTMLUtilities.BRForDisplay(model.Account.CustomerNotes);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewContact(int id)
//        {
//            var model = new AccViewModel();
//            model.GetContact(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult EditAccount(int id)
//        {
//            AccViewModel model = new AccViewModel();
//            model.GetAccount(id);
//            model.GetContacts(x => x.AccountFk == id);
//            model.GetAddresses(x => x.AccountFk == id);
//           model.GetVariousLookups();
//            return View(model);
//        }


//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SaveAccount(AccViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveAccount(model);

//                return RedirectToAction("ViewAccount", "Acc", new { id = model.Account.AccountId });
//            }
//            return RedirectToAction("EditAccount", "Acc", new { id = model.Account.AccountId });
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult EditContact(int id)
//        {
//            AccViewModel model = new AccViewModel();

//            model.GetContact(id);

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SaveContact(AccViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveContact(model);

//                string url = "EditAccount/" + model.Contact.AccountFk + "#AccountContactsTab";
//                return Redirect(url);
//            }
//            return RedirectToAction("EditContact", "Acc", new { id = model.Contact.ContactId });
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult ViewAddress(int id)
//        {
//            var model = new AccViewModel();
//            model.GetAddress(id);

//            return View(model);
//        }

//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin, OMSCS")]
//        public ActionResult EditAddress(int id)
//        {
//            AccViewModel model = new AccViewModel();
//            model.GetAddress(id);

//            return View(model);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [Authorize(Roles = "IntranetAdmin, OMSAccount, OMSAdmin")]
//        public ActionResult SaveAddress(AccViewModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                model.SaveAddress(model);

//                string url = "EditAccount/" + model.Address.AccountFk + "#AccountAddressesTab";
//                return Redirect(url);
//            }
//            return RedirectToAction("EditAddress", "Acc", new { id = model.Address.AddressId });
//        }
//    }
//}