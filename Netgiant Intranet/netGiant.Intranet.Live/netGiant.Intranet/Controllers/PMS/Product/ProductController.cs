using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize(Roles="IntranetAdmin, PMSAdmin, PMSReader")]
    public class ProductController : ApplicationController
    {
        #region Product

        public ActionResult ProductIndex()
        {
            ProductViewModel model = new ProductViewModel();
            return View("~/Views/PMS/Product/Product/ProductIndex.cshtml", model.Get());
        }

        //public ActionResult ProductIndexData(List<string> optionsArray)
        //{
        //    ProductViewModel model = new ProductViewModel();
        //    model.selectedManufacturerID = Convert.ToInt32(optionsArray[3]);
        //    model.selectedProductGroupID = Convert.ToInt32(optionsArray[4]);
        //    model.selectedProductStatusID = Convert.ToInt32(optionsArray[5]);
        //    model.selectedSalesAreaGroupID = Convert.ToInt32(optionsArray[6]);

        //    return PartialView("~/Views/PMS/Product/Product/ProductIndexData.cshtml", model.Get(Convert.ToInt32(optionsArray[7]),
        //        optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString()).products);
        //}

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProduct(int id)
        {
            ViewBag.ProductId = id;
            TempData["ParentAction"] = "CreateProduct";
            TempData.Keep("ParentAction");
            return View("~/Views/PMS/Product/Product/CreateProduct.cshtml", ProductViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveProduct(ProductViewModel model)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);

            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Product Saved";
            }

            //if (model.ChangedWebsiteInv)
            //{
            return RedirectToAction("CreateProduct", new { id = model.prod.productID });
            //}
            //else
            //{
                //return RedirectToAction("Detail", new { id = model.prod.productID });
            //}
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProduct(List<string> optionsArray)
        {
            ProductViewModel model = new ProductViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Product Deleted";

            model.SelectedManufacturerID = Convert.ToInt32(optionsArray[4]);
            model.SelectedProductGroupID = Convert.ToInt32(optionsArray[5]);
            model.SelectedProductStatusID = Convert.ToInt32(optionsArray[6]);
            model.SelectedSalesAreaGroupID = Convert.ToInt32(optionsArray[7]);

            model = model.Get(Convert.ToInt32(optionsArray[8]),
                optionsArray[1].ToString(), optionsArray[2].ToString(), optionsArray[3].ToString());

            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.Products.Count < 50;
            jsonModel.Count = model.ProductsCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Product/Product/ProductIndexData.cshtml", model.Products);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMembership(List<string> optionsArray)
        {
            ProductDetailViewModel model = new ProductDetailViewModel();
            bool success = model.DeleteMembership(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Membership Deleted";
            }
            ViewBag.ProductId = Convert.ToInt32(optionsArray[1]);
            model.GetProductDetail(Convert.ToInt32(optionsArray[1]));

            return PartialView("~/Views/PMS/Product/ProductDetail/EquipMemberships.cshtml", model.ProductMembership);
        }

        public ActionResult Detail(int id)
        {
            ViewBag.ProductId = id;
            TempData["ParentAction"] = "Detail";
            TempData.Keep("ParentAction");
            ProductDetailViewModel model = new ProductDetailViewModel();
            return View("~/Views/PMS/Product/ProductDetail/ProductDetail.cshtml", model.GetProductDetail(id));
        }

        public ActionResult EquipMemberships(List<string> optionsArray)
        {
            ViewBag.ProductId = Convert.ToInt32(optionsArray[0]);
            ProductDetailViewModel model = new ProductDetailViewModel();
            model.GetProductDetail(Convert.ToInt32(optionsArray[0]));
            model.GetEquipmentMemberships(Convert.ToInt32(optionsArray[2]), Convert.ToInt32(optionsArray[0]), optionsArray[1]);

            return PartialView("~/Views/PMS/Product/ProductDetail/EquipMemberships.cshtml", model.ProductMembership);
        }

        public ActionResult GetNewEbusinessRow(string[] parms)
        {
            return PartialView("~/Views/PMS/Product/Product/EbusinessGroups.cshtml", 
                ProductViewModel.CreateNewEbusinessMapping(parms[0], Convert.ToInt32(parms[1])));
        }

        public ActionResult GetNewCatCodeRow(string[] parms)
        {
            ViewBag.guid = parms[2];
            return PartialView("~/Views/PMS/Product/Product/CategoryCodes.cshtml",
                ProductViewModel.CreateNewSecondaryCategoryLookup(Convert.ToInt32(parms[0]), Convert.ToInt32(parms[1])));
        }

        [ChildActionOnly]
        public ActionResult ProductList(List<product> Model)
        {
            return PartialView("~/Views/PMS/Product/Product/ProductIndexData.cshtml", Model);
        }

        [HttpPost]
        public ActionResult ProductData(string[] optionsArray)
        {
            ProductViewModel model = new ProductViewModel();
            model.SelectedManufacturerID = Convert.ToInt32(optionsArray[3]);
            model.SelectedProductGroupID = Convert.ToInt32(optionsArray[4]);
            model.SelectedProductStatusID = Convert.ToInt32(optionsArray[5]);
            model.SelectedSalesAreaGroupID = Convert.ToInt32(optionsArray[6]);
            model.SelectedProductItemTypeID = Convert.ToInt32(optionsArray[7]);

            model = model.Get(Convert.ToInt32(optionsArray[8]),
                optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.Products.Count < 50;
            jsonModel.Count = model.ProductsCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/PMS/Product/Product/ProductIndexData.cshtml", model.Products);
            return Json(jsonModel);
        }

        public JsonResult GetAvailableComponents(string searchTerm)
        {
            int productItemType = 1;
            return Json(SelectListViewModel.GetProductsArray(searchTerm, productItemType).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNewComponentRow(string[] parms)
        {
            return PartialView("~/Views/PMS/Product/Product/AssemblyComponent.cshtml",
                ProductViewModel.CreateNewAssemblyComponent(Convert.ToInt32(parms[0]), Convert.ToInt32(parms[1])));
        }

        public ActionResult GetAssemblyComponents(int productFK)
        {
            ProductDetailViewModel model = new ProductDetailViewModel();
            return PartialView("~/Views/PMS/Product/ProductDetail/AssemblyComponentsData.cshtml", model.GetProductDetail(productFK));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipMembership(int id)
        {
            ProductDetailViewModel model = new ProductDetailViewModel();

            return View("~/Views/PMS/Product/ProductDetail/CreateEquipMembership.cshtml", model.GetEquipmentOptions(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveMembership(ProductDetailViewModel model, string parent)
        {
            var success = model.SaveMembership(model);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Equipment Membership Saved";
            }

            return RedirectToAction(parent, new { id = model.ProductDetail.productID });
        }

        #endregion Product

        #region AXIS Queue

        public ActionResult AXISQueueIndex()
        {
            AXISQueueViewModel model = new AXISQueueViewModel();

            return View("~/Views/PMS/Product/AXISQueue/AXISQueueIndex.cshtml", model.Get());
        }

        public ActionResult AXISQueueIndexData(List<string> optionsArray)
        {
            AXISQueueViewModel model = new AXISQueueViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Product/AXISQueue/AXISQueueIndexData.cshtml", model.listAXISQueueDetails);
        }

        public ActionResult AddToAxisQueueAll(int productFK)
        {
            ProductViewModel.AddProductToAxisQueue(productFK, "C", QueueType.Full, "");
            return RedirectToAction("AXISQueueIndex");
        }

        public ActionResult AddToAxisQueuePrice(int productFK)
        {
            ProductViewModel.AddProductToAxisQueue(productFK, "U", QueueType.Partial, "price");
            return RedirectToAction("AXISQueueIndex");
        }

        #endregion AXIS Queue

        #region Product Prices

        public ActionResult ProductPrices(int productFK)
        {
            ProductViewModel pVm = new ProductViewModel();
            return PartialView("~/Views/PMS/Product/Product/ProductPricesData.cshtml", pVm.GetProductPrices(productFK).productPrices);
        }

        #endregion Product Prices

    }
}