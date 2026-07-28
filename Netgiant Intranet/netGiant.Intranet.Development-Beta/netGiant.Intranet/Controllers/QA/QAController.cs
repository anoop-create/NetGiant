using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using netGiant.Intranet.BusinessLayer.ViewModels.QA;
using System.Web.Security;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.QA
{
    [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
    public class QAController : ApplicationController
    {
        [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
        public ActionResult Index()
        {
            var model = new QAViewModel();
            return View(model);
        }

        [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
        public ActionResult QAIndexData(List<string> optionsArray)
        {
            var model = new QAViewModel();
            return PartialView("_QAData", model.Get(Convert.ToInt32(optionsArray[4]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2], Convert.ToInt32(optionsArray[3])).ListOfQAs);
        }

        public ActionResult QAEntry_Read([DataSourceRequest]DataSourceRequest request)
        {
            QAViewModel model = new QAViewModel();
            model.GetQAList();

            var result = model.QAList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, QAAdmin")]
        public ActionResult Create(int id)
        {
            return View(QAViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, QAAdmin")]
        public ActionResult Save(QAViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.UserName = User.Identity.Name;
                model.Save(model);
                TempData["InformationBoxFlag"] = "Question Saved";
            }

            return RedirectToAction("Create", new { id = model.QA.QuestionAnswerID });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, QAAdmin")]
        public ActionResult Delete(int id)
        {
            QAViewModel model = new QAViewModel();

            SaveReturn sr = model.Delete(id);

            return Json(new { saveReturn = sr });
        }

        [Authorize(Roles = "IntranetAdmin, QAAdmin")]
        public ActionResult SendCustomerAnsweredEmail(int id)
        {
            var model = new QAViewModel();
            model.SendCustomerAnsweredEmail(id);

            return RedirectToAction("Index");
        }
    }
}
