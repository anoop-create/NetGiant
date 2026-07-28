using netGiant.Intranet.BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using netGiant.Intranet.Controllers;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;

namespace netGiant.Intranet.Areas.PMS.Product
{
    [Authorize(Roles="IntranetAdmin, PMSAdmin, PMSReader")]
    public class ProductController : ApplicationController
    {
        public ActionResult ProductIndex()
        {
            return View(new ProductViewModel());
        }

        public ActionResult ProductGroupIndex()
        {
            return View(new ProductGroupViewModel());
        }

        public ActionResult BackToCreateProduct()
        {
            int productId = Convert.ToInt32(TempData["ImageParentProductId"]);
            return CreateProduct(productId);
        }

        public ActionResult Product_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProductViewModel();
            model.GetProducts();

            var result = model.ProductList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult Product_Inventory_Read([DataSourceRequest]DataSourceRequest request, int id)
        {
            var model = new ProductDetailViewModel();
            model.GetProductDetail(id);

            var result = model.ProviderInventory.ToDataSourceResult(request);
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult ProductGroup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProductGroupViewModel().Get();

            var result = model.ProductGroupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProduct(int id)
        {
            ViewBag.ProductId = id;
            TempData["ParentAction"] = "CreateProduct";
            TempData.Keep("ParentAction");
            return View(ProductViewModel.Create(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProductGroup(int id)
        {
            return View(new ProductGroupViewModel().Create(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateProductImage(int prodId, int webInvId, int id)
        {
            var model = new ProductViewModel();
            model.CreateProductImage(webInvId, id);

            TempData["ImageParentProductId"] = prodId;
            TempData.Keep("ImageParentProductId");

            return View(model);
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

            return RedirectToAction("CreateProduct", new { id = model.prod.productID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveProductGroup(ProductGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Product Group Saved";
                }
            }
            return RedirectToAction("ProductGroupIndex");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveProductImage(ProductViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.SaveProductImage(model.ProductImage);
                TempData["InformationBoxFlag"] = "Product Saved";
            }

            int prodId = (int)TempData["ImageParentProductId"];

            return RedirectToAction("CreateProduct", new { id = prodId });
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProduct(List<string> optionsArray)
        {
            var model = new ProductViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Product Deleted";

            if(optionsArray[1] == "telerik")
            {
                return Json(new JsonModel());
            }

            model.SelectedManufacturerID = Convert.ToInt32(optionsArray[4]);
            model.SelectedProductGroupID = Convert.ToInt32(optionsArray[5]);
            model.SelectedProductStatusID = Convert.ToInt32(optionsArray[6]);
            model.SelectedSalesAreaGroupID = Convert.ToInt32(optionsArray[7]);

            model = model.Get(Convert.ToInt32(optionsArray[8]), optionsArray[1].ToString(), optionsArray[2].ToString(), optionsArray[3].ToString());

            var jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.Products.Count < 50;
            jsonModel.Count = model.ProductsCount;
            jsonModel.HTMLString = RenderPartialViewToString("_ProductIndexData", model.Products);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProductGroup(int id)
        {
            return Json(new { saveReturn = new ProductGroupViewModel().Delete(id) });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteProductImage(List<int> optionsArray)
        {
            var model = new ProductViewModel();
            bool success = model.DeleteProductImage(optionsArray[0]);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Image Deleted";
            }

            model = ProductViewModel.Create(optionsArray[1]);

            return PartialView("_ProductImages", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMembership(List<string> optionsArray)
        {
            var model = new ProductDetailViewModel();
            bool success = model.DeleteMembership(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Membership Deleted";
            }
            ViewBag.ProductId = Convert.ToInt32(optionsArray[1]);
            model.GetProductDetail(Convert.ToInt32(optionsArray[1]));

            return PartialView("_EquipMemberships", model.ProductMembership);
        }

        public ActionResult Detail(int id)
        {
            ViewBag.ProductId = id;
            TempData["ParentAction"] = "Detail";
            TempData.Keep("ParentAction");
            var model = new ProductDetailViewModel();
            return View("ProductDetail", model.GetProductDetail(id));
        }

        public ActionResult EquipMemberships(List<string> optionsArray)
        {
            ViewBag.ProductId = Convert.ToInt32(optionsArray[0]);
            var model = new ProductDetailViewModel();
            model.GetProductDetail(Convert.ToInt32(optionsArray[0]));
            model.GetEquipmentMemberships(Convert.ToInt32(optionsArray[2]), Convert.ToInt32(optionsArray[0]), optionsArray[1]);

            return PartialView(model.ProductMembership);
        }

        public ActionResult GetNewEbusinessRow(string[] parms)
        {
            return PartialView("_EbusinessGroups", ProductViewModel.CreateNewEbusinessMapping(parms[0], Convert.ToInt32(parms[1])));
        }

        public ActionResult GetNewCatCodeRow(string[] parms)
        {
            ViewBag.guid = parms[2];
            return PartialView("_CategoryCodes", ProductViewModel.CreateNewSecondaryCategoryLookup(Convert.ToInt32(parms[0]), Convert.ToInt32(parms[1])));
        }

        public JsonResult GetAvailableComponents(string searchTerm)
        {
            int productItemType = 1;
            return Json(SelectListViewModel.GetProductsArray(searchTerm, productItemType).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNewComponentRow(string[] parms)
        {
            return PartialView("_AssemblyComponent", ProductViewModel.CreateNewAssemblyComponent(Convert.ToInt32(parms[0]), Convert.ToInt32(parms[1])));
        }

        public ActionResult GetAssemblyComponents(int productFK)
        {
            var model = new ProductDetailViewModel();
            return PartialView("_AssemblyComponentsData", model.GetProductDetail(productFK));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipMembership(int id)
        {
            var model = new ProductDetailViewModel();
            return View(model.GetEquipmentOptions(id));
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

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SetUntrustedProvider(List<string> options)
        {
            var model = new ProductViewModel();

            if (options[0] != "null")
                model.SetProviderUntrusted(Convert.ToInt32(options[0]), Convert.ToBoolean(options[1]));

            return PartialView("ProductIndex", model.Get());
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult EditUntrustedProviders(int id)
        {
            ViewBag.ProductId = id;
            TempData["ParentAction"] = "EditUntrustedProviders";
            TempData.Keep("ParentAction");
            var model = new ProductDetailViewModel();
            return View(model.GetProductDetail(id));
        }

        public ActionResult AXISQueueIndex()
        {
            var model = new AXISQueueViewModel();
            return View(model);
        }

        public ActionResult AxisQueue_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new AXISQueueViewModel().Get();

            var result = model.AxisQueueItemList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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
    }
}