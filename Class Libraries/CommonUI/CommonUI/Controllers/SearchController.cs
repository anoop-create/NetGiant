using BusinessLogic.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using BusinessLogic;

namespace CommonUI.Controllers
{
    [SiteOfflineCheck]
    public class SearchController : ApplicationController
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string keyword, string wizardSearch, string token, string filter = "", string cat = null)
        {
            if (!Convert.ToBoolean(Session["U_IsFromPPC"]))
            {
                string sDate = Utilities.SimpleDecryptString(token);
                bool isValidDate = DateTime.TryParse(sDate, out DateTime tDate);
                if (!isValidDate)
                {
                    // Not a valid token
                    return RedirectToAction("Index", "Home");
                }

                tDate = tDate.ToUniversalTime();
                DateTime cDate = DateTime.Now.ToUniversalTime();
                DateTime vDate = cDate.AddHours(-1);
                if (tDate < vDate || tDate > cDate)
                {
                    // OK. The token date/time is lower than the current valid date/time or it's greater than the current date/time 
                    // so it's not a valid token 
                    return RedirectToAction("Index", "Home");
                }
            } 

            var model = new SearchViewModel
            {
                SearchTerm = keyword ?? wizardSearch
            };
            model.SearchTerm = Regex.Replace(model.SearchTerm, @"[\W]", " ");
            ViewBag.Term = model.SearchTerm;
            model.CategoryRestriction = cat;
            model.GetResults();

            if(!string.IsNullOrEmpty(model.JumpUrl))
            {              
                return Redirect(model.JumpUrl);
            }

            if (filter != "")
            {
                model.SetProductFilters(filter);
            }

            model.BreadcrumbTrail.Add("Search", "");
            ViewBag.ShowSavings = Utilities.GetItemFromDict(model.ProductData, "SwitchToBanner").Length > 0;
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        public ActionResult Gsearch(string keyword)
        {
            SearchViewModel model = new SearchViewModel();
            // Check request is from Google
            if (!Convert.ToBoolean(Request.UrlReferrer?.AbsoluteUri.Contains("www.google.com")))
            {
                return RedirectToAction("Index", "Home");
            }

            // Pass control to index
            return RedirectToAction("Index", new { keyword = keyword, wizardSearch = false, token = model.EncryptedDate });
        }

        public ActionResult Autocomplete(string keyword)
       {
            var model = new SearchViewModel
            {
                SearchTerm = keyword
            };

            ViewBag.Term = model.SearchTerm;
            model.GetResults(10, true);

            return PartialView(model);
        }
    }
}

