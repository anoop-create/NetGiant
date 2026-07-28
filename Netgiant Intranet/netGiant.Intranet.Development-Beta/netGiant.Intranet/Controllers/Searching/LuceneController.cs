using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Searching;

namespace netGiant.Intranet.Controllers.Searching
{
    public class LuceneController : Controller
    {
        // GET: Lucene
        public ActionResult CreateIndex()
        {
            LuceneSearchViewModel model = new LuceneSearchViewModel();
            return View("~/Views/Searching/Lucene/CreateIndex.cshtml", model.CreateIndex());
        }

        public ActionResult Search()
        {
            LuceneSearchViewModel model = new LuceneSearchViewModel();
            return View("~/Views/Searching/Lucene/Search.cshtml", model);
        }

        public ActionResult SearchIndex(LuceneSearchViewModel model)
        {
            return View("~/Views/Searching/Lucene/Search.cshtml", model.SearchIndex());
        }
    }
}