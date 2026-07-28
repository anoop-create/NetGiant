using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class MyAccountController : ApplicationController
    {
        private MyAccountViewModel model;

        // GET: MyAccount
        public ActionResult Index(string id = "")
        {
            if (Authentication.IsNotAuthenticated())
            {
                return RedirectToAction("Index", "Home");
            }
            model = new MyAccountViewModel();
            ViewBag.OpenSection = id.ToLower().Trim();

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        public JsonResult YourDetails()
        {
            MyAccountDetails partialModel = new MyAccountDetails();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            partialModel = MyAccountViewModel.GetMyAccountDetails(Session["U_Record"].ToString());
            partialModel.TitleList = Utilities.BuildTitleList();

            string html = RenderPartialViewToString("~/Views/MyAccount/YourDetails.cshtml", partialModel);

            return Json(new
            {
                responseHtml = html
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateDetails(MyAccountDetails details)
        {
            var sr = new SaveReturn();

            if (Session["U_Password"].ToString().Equals(details.OldPassword))
            {
                if (Touchpoints.CheckEmailAvailable(details.Email) || details.Email == Convert.ToString(Session["U_Email"]))
                {
                    sr = MyAccountViewModel.UpdateAccountDetails(details, Session["U_Record"].ToString());
                }
                else
                {
                    sr.IsSuccess = false;
                    sr.Message = "Email";
                }
            }
            else
            {
                sr.IsSuccess = false;
                sr.Message = "Password";
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        public JsonResult BillingAddress()
        {
            MyAccountDetails partialModel = new MyAccountDetails();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            partialModel = MyAccountViewModel.GetMyAccountDetails(Session["U_Record"].ToString());

            string html = RenderPartialViewToString("~/Views/MyAccount/BillingAddress.cshtml", partialModel);

            return Json(new
            {
                responseHtml = html
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateAddress(MyAccountDetails details)
        {
            SaveReturn sr = MyAccountViewModel.UpdateAddress(details);

            return Json(new
            {
                savereturn = sr
            });
        }

        public JsonResult MyVouchers()
        {
            model = new MyAccountViewModel();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            string ac = Session["U_AccountNo"].ToString();
            model.VoucherList = EntityAccess.ReadVoucherPromo(
                x => !x.IsUsed
                     && x.WebsiteFk == w
                     && (x.VoucherType.Description == "Amount" || x.VoucherType.Description == "Percentage" || x.VoucherType.Description == "MultiBuy")
                     && (x.AccountNumber == null || x.AccountNumber == ac)
                     && (x.ValidFrom <= DateTime.Now && x.ValidTo >= DateTime.Now)
            )
            .OrderByDescending(x => x.AccountNumber)
                .ThenByDescending(x => x.Amount)
                .ThenByDescending(x => x.Percentage)
            .ToList();

            ViewBag.IsNone = false;
            if (model.VoucherList.Count == 0)
            {
                ViewBag.IsNone = true;
            }
            string html = RenderPartialViewToString("~/Views/MyAccount/MyVouchers.cshtml", model);

            return new JsonResult()
            {
                Data = new { responseHtml = html },
                MaxJsonLength = int.MaxValue
            };
        }

        public JsonResult OrderHistory()
        {
            model = new MyAccountViewModel();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            model.GetOrderData();

            ViewBag.IsNone = false;
            if (model.OrderHistoryList.Count == 0)
            {
                ViewBag.IsNone = true;
            }
            string html = RenderPartialViewToString("~/Views/MyAccount/OrderHistory.cshtml", model);

            return new JsonResult()
            {
                Data = new { responseHtml = html },
                MaxJsonLength = int.MaxValue
            };
        }

        public JsonResult OrderDetail(string orderno = "")
        {
            model = new MyAccountViewModel();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            model.GetOrder(orderno);

            string filepath = Server.MapPath("/media/archive/INV" + orderno + "A.pdf");
            model.Order.InvoiceExists = System.IO.File.Exists(filepath);

            ViewBag.IsNone = false;
            if (model.Order == null)
            {
                ViewBag.IsNone = true;
            }
            string html = RenderPartialViewToString("~/Views/MyAccount/OrderDetail.cshtml", model);

            return new JsonResult()
            {
                Data = new { responseHtml = html },
                MaxJsonLength = int.MaxValue
            };
        }

        public JsonResult QuickReorder()
        {
            model = new MyAccountViewModel();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            if (Session["U_RecentlyOrdered"] == null)
            {
                Authentication.LoadRecentlyOrdered(Session["U_Record"].ToString());
            }
            string html = RenderPartialViewToString("~/Views/MyAccount/QuickReorder.cshtml", model);

            return Json(new
            {
                responseHtml = html
            });
        }

        public JsonResult MyPrinters()
        {
            model = new MyAccountViewModel();
            Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            if (Session["U_FavoutirePrinters"] == null)
            {
                string id = Session["U_Record"].ToString().Contains("/")
                    ? Session["U_Record"].ToString()
                    : Session["U_Email"].ToString();
                Authentication.LoadFavouritePrinters(id);
            }
            string html = RenderPartialViewToString("~/Views/MyAccount/MyPrinters.cshtml", model);

            return Json(new
            {
                responseHtml = html
            });
        }

        [HttpPost]
        public JsonResult SavePrinter()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            favouritePrinter fp = new favouritePrinter();
            fp.customerId = Request.Form["customerid"] != null ? Request.Form["customerid"].ToString() : "";
            fp.siteId = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            fp.eqEquipmentFK = int.Parse(Request.Form["equipid"]);
            fp.modelName = Request.Form["modelName"] != null ? Request.Form["modelName"].ToString() : "";
            fp.description = String.IsNullOrEmpty(Request.Form["description"])
                ? "My Printer"
                : Request.Form["description"].ToString();
            fp.dateLastUpdated = DateTime.Now;

            if (fp.description != "")
            {
                EntityAccess.InsertFavouritePrinter(fp);
                // Refresh Favourite Printers Session
                Authentication.LoadFavouritePrinters(fp.customerId);
                eqEquipment eq = EntityAccess.ReadEquipment(x => x.eqEquipmentID == fp.eqEquipmentFK).FirstOrDefault();
                string eqUrl = ("/model/" + eq.description + "-" + eq.eqCartridgeType.eqCartridgeTypeName.ToLower() + "/").Replace(" ", "-");
                sr.Html = "<div class=\"g-flex-vcenter\">" +
                          "<div class=\"pull-left\">" +
                          "<a href=\"" + eqUrl + "\"><img src=\"" + ConfigurationManager.AppSettings["CDN"] + "/" + eq.thumbnailURL + "\" alt=\"" + eq.description + "\" onerror=\"this.src = '" + ConfigurationManager.AppSettings["CDN"] + "/Images/noimage.jpg';\" /></a>" +
                          "</div>" +
                          "<div class=\"pull-left g-m-l-10 g-w-200\"><a href=\"" + eqUrl + "\" class=\"primary\">" + eq.description + "</a></div>" +
                          "</div>";
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult DeletePrinter()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            int id = int.Parse(Request.Form["favid"].ToString());
            string record = Session["U_Record"].ToString().Contains("/")
                ? Session["U_Record"].ToString()
                : Session["U_Email"].ToString();

            favouritePrinter fp = EntityAccess
                .ReadFavouritePrinter(x => x.favouritePrinterID == id && x.customerId == record).FirstOrDefault();

            if (fp != null)
            {
                EntityAccess.DeleteFavouritePrinter(fp);
                // Refresh Favourite Printers Session
                Authentication.LoadFavouritePrinters(fp.customerId);
            }

            return Json(new
            {
                savereturn = sr,
                id = id
            });
        }

        [HttpPost]
        public JsonResult PasswordResetRequest(string emailTemplate)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            model = new MyAccountViewModel();
            Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");

            passwordReset pr = new passwordReset();
            pr.email = Request.Form["email"] != null ? Request.Form["email"].ToString() : "";
            pr.siteId = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            pr.dateCreated = DateTime.Now;
            pr.guid = Guid.NewGuid().ToString();

            var userData = Touchpoints.GetUserData("", pr.email);
            if (pr.email != "" && userData.Rows.Count > 0)
            {
                EntityAccess.InsertPasswordReset(pr);

                string resetlink = "https://" + Utilities.GetItemFromDict(commondata, "DomainName") + "MyAccount/ResetPassword/" + pr.guid;
                Dictionary<string, string> replacements = new Dictionary<string, string>();
                replacements.Add("[resetlink]", resetlink);
                Utilities.SendEmail(Utilities.GetItemFromDict(commondata, "SupportEmail").ToLower(), pr.email, "Set your new password",
                    emailTemplate, replacements, "password@netgiant.com");
            }
            else
            {
                sr.IsSuccess = false;
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult VerifyPassword(string password)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            if (!Session["U_Password"].ToString().Equals(password))
            {
                sr.IsSuccess = false;
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        public ActionResult ResetPassword(string id)
        {
            model = new MyAccountViewModel();
            model.RedirectUrl = "/";
            model.GetResetData(id);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(MyAccountViewModel model)
        {
            model.ResetPassword.Sr = model.SetNewPassword();
            return View(model);
        }

        public ActionResult SignIn()
        {
            model = new MyAccountViewModel();
            return View(model);
        }

        public ActionResult SignInMobile()
        {
            model = new MyAccountViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SignIn(CommonViewModel model)
        {
            bool success = Authentication.Authenticate(model.SignIn.UserName, model.SignIn.Password);
            Session["U_FullyAuthenticated"] = success;

            return Json(new {issuccess = success, message = "", redirecturl = model.RedirectUrl}, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SignOut()
        {
            bool success = Authentication.LogOut(true);
            return Json(new { issuccess = success, message = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SignUp(CommonViewModel cVm)
        {
            var saveReturn = MyAccountViewModel.CreateUser(cVm.SignUp);
            if (saveReturn.IsSuccess)
            {
                Session["U_FullyAuthenticated"] = Authentication.Authenticate(cVm.SignUp.UserName, cVm.SignUp.Password);
            }

            return Json(new {saveReturn = saveReturn});
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public bool UserExists(string email)
        {
            return !Authentication.GetAccountInfo(email).IsNewAccount;
        }
    }
}
