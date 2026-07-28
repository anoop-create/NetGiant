using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize]
    public class ProductFieldController : ApplicationController
    {
        public ActionResult ProductFields(int id)
        {
            ProductFieldViewModel model = new ProductFieldViewModel();
            return View("~/Views/PMS/Product/ProductFields/ProductFields.cshtml", model.Get(id, 1, 1));
        }

        [HttpPost]
        public ActionResult ProductFieldsData (List<string> optionsArray)
        {
            ProductFieldViewModel model = new ProductFieldViewModel();
            return PartialView("~/Views/PMS/Product/ProductFields/ProductFieldData.cshtml",
                model.Get(Convert.ToInt32(optionsArray[0]), Convert.ToInt16(optionsArray[1]), Convert.ToInt32(optionsArray[2])));
        }

        [HttpPost]
        public ActionResult EditProductFieldsData(List<string> optionsArray)
        {
            ProductFieldViewModel model = new ProductFieldViewModel();
            model.SelectedFieldSectionIndex = Convert.ToInt32(optionsArray[3]);
            return PartialView("~/Views/PMS/Product/ProductFields/EditProductFieldData.cshtml",
                model.Edit(Convert.ToInt32(optionsArray[0]), Convert.ToInt16(optionsArray[1]), Convert.ToInt32(optionsArray[2])));
        }

        [HttpPost]
        public ActionResult SaveProductFieldData(ProductFieldViewModel model)
        {
            model.Save();            
            return View("~/Views/PMS/Product/ProductFields/ProductFields.cshtml", model.Get(model.SelectedProductID, model.SelectedWebsiteID, model.SelectedFieldSectionID));
        }

        public ActionResult CreateProductFieldName(int id)
        {
            return PartialView("~/Views/PMS/Product/ProductFields/CreateProductFieldName.cshtml", ProductFieldViewModel.CreateFieldName(id));
        }

        public ActionResult Test()
        {
            bool test = AXISFeedViewModel.TestingTheTest();
            return null;
        }
    }
}