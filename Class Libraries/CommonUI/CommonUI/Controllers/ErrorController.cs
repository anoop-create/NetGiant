using BusinessLogic.ViewModels;
using System.Web.Mvc;
using BusinessLogic;

namespace CommonUI.Controllers
{
    public class ErrorController : ApplicationController
    {
        private ErrorViewModel model;
        // GET: Error
        public ActionResult Index(int id = 0, string status = "", string statusDetail = "")
        {
            if (Session["C_IsInCheckout"] != null)
            {
                Session.Remove("C_IsInCheckout");
            }
            model = new ErrorViewModel(id);
            if (model.ErrorNumber == 404)
            {
                model.ErrorDetail = Request.Url.ToString();
                model.ErrorDetail = model.ErrorDetail.Replace("http://", "https://");
                model.ErrorDetail = model.ErrorDetail.Replace("error/index/404?asperrorpath=/", "");
                model.ErrorDetail = model.ErrorDetail.Replace("Error/Index/404?aspxerrorpath=/", "");
            }
            else
            {
                model.ErrorDetail = statusDetail;
            }
            if (Session["C_CheckoutDetails"] != null)
            {
                Session.Remove("C_CheckoutDetails");
            }

            if (model.ResponseStatusCode > 0)
                Response.StatusCode = id;

            return PartialView(model);
        }

        public ActionResult Alert(int alertLevel = 0)
        {
            if (alertLevel == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            if (alertLevel == 4)
            {
                ViewBag.AlertTitle = "The checkout is temporarily unavailable";
                ViewBag.AlertMessage = "We expect to be up and running again shortly, please feel free to browse until then.";
            }

            if (alertLevel == 5)
            {
                ViewBag.AlertTitle = "We're Temporarily Down for Maintenance";
                ViewBag.AlertMessage = "We should be back online shortly, please visit us again soon";
            }

            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ScriptError(string url, string description)
        {
            Utilities.ScriptException(url, description);

            return null;
        }
    }
}
