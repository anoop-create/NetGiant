using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;
using System.Configuration;
using System.Web;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Review;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Review
{
    [Authorize]
    public class ReviewController : Controller
    {
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Index()
        {
            ReviewViewModel model = new ReviewViewModel();
            model.GetReviews();
            return View(model);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult ReviewDataAjax([DataSourceRequest] DataSourceRequest request)
        {
            ReviewViewModel model = new ReviewViewModel();
            model.GetReviews();

            DataSourceResult result = model.ReviewList.ToDataSourceResult(request);
            JsonResult jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult EditReview(int id)
        {
            ReviewViewModel model = new ReviewViewModel();
            return View(model.GetReviewForEdit(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveReview(ReviewViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveisHidden();
                return RedirectToAction("Index");
            }
            return View("EditReview", model);
        }
    }
}