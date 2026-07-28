using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class CartridgeTypeController : ApplicationController
    {
        public ActionResult CartridgeTypeIndex()
        {
            CartridgeTypeViewModel model = new CartridgeTypeViewModel();
            return View("~/Views/PMS/Maintenance/CartridgeType/CartridgeTypeIndex.cshtml", model.GetCartridgeType()); 
        }

        public ActionResult CartridgeTypeList(List<eqCartridgeType> model)
        {
            return PartialView("~/Views/PMS/Maintenance/CartridgeType/CartridgeTypeData.cshtml", model);
        }

        public ActionResult CartridgeTypeData(string[] optionsArray)
        {
            CartridgeTypeViewModel model = new CartridgeTypeViewModel();
            model.GetCartridgeType(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            return CartridgeTypeGetJson(model);
        }

        private ActionResult CartridgeTypeGetJson(CartridgeTypeViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.CartridgeTypeList.Count < 50;
            jsonModel.Count = model.CartridgeTypeListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/CartridgeType/CartridgeTypeData.cshtml",
                model.CartridgeTypeList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            CartridgeTypeViewModel model = new CartridgeTypeViewModel();
            return View("~/Views/PMS/Maintenance/CartridgeType/CreateCartridgeType.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CartridgeTypeViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Cartridge Type Saved";
                }
                return RedirectToAction("CartridgeTypeIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.CartridgeType.ID });
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            CartridgeTypeViewModel model = new CartridgeTypeViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetCartridgeType(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            TempData["InformationBoxFlag"] = "CartridgeType Deleted";

            return CartridgeTypeGetJson(model);
        }
    }
}