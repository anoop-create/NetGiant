using System.Web.Mvc;
using System;
using System.Collections.Generic;
using netGiant.Intranet.BusinessLayer.ViewModels.QA;
using System.Web.Security;

namespace netGiant.Intranet.Controllers.QA
{
    [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
    public class QAController : ApplicationController
    {
        [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
        public ActionResult Index()
        {
            QAViewModel model = new QAViewModel();
            return View("~/Views/QA/Main/Index.cshtml", model.Get());
        }

        [Authorize(Roles = "IntranetAdmin, QAAdmin, QAReader")]
        public ActionResult QAIndexData(List<string> optionsArray)
        {
            QAViewModel model = new QAViewModel();
            return PartialView("~/Views/QA/Main/QAData.cshtml",
                model.Get(Convert.ToInt32(optionsArray[4]), optionsArray[0].ToString(),
                    optionsArray[1].ToString(), optionsArray[2], Convert.ToInt32(optionsArray[3])).ListOfQAs);
        }

        [Authorize(Roles = "IntranetAdmin, QAAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/QA/Main/Create.cshtml", QAViewModel.Create(id));
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
        public ActionResult Delete(List<string> optionsArray)
        {
            QAViewModel model = new QAViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Question Deleted";

            return PartialView("~/Views/QA/Main/QAData.cshtml", model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[1].ToString(),
                optionsArray[2], "", null).ListOfQAs);
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
