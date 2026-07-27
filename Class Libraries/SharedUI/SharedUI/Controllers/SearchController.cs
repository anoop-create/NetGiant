using BusinessLogic.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using BusinessLogic;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class SearchController : ApplicationController
    {
        public ActionResult Index(string keyword, string wizardSearch, string token, string filter = "", string cat = null)
        {
            if (!Convert.ToBoolean(Session["U_IsFromPPC"]))
            {
                //try
                //{
                //    System.Web.Helpers.AntiForgery.Validate();
                //}
                //catch (Exception)
                //{
                //    return RedirectToAction("Index", "Home");
                //}

                // Check token for a valid time in the last hour

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

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
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

