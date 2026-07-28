using DP001BusinessLogic;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class MapBrandCategoryController : ApplicationController
    {
        // GET: MapBrandCategory
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetCategories(int brandFK, long productCategoryFK)
        {
            string html = "";

            CrudMapBrandCategory crud = new CrudMapBrandCategory();
            CrudBrand crudBrand = new CrudBrand();
            CrudProductCategory crudCategory = new CrudProductCategory();

            var channelId = GetChannelId();
            var hasPermissionBrand = crudBrand.Read(x => x.ChannelFK == channelId && x.BrandID == brandFK).Count > 0 || brandFK == 0;
            var hasPermissionCategory = crudCategory.Read(x => x.ChannelFK == channelId && x.ProductCategoryID == productCategoryFK).Count > 0 || productCategoryFK == 0;

            if (hasPermissionBrand && hasPermissionCategory)
            {
                List<MapBrandCategory> lmbc = crud.GetCategories(brandFK);

                html += "<option value=\"\">Select ...</option>";
                foreach (MapBrandCategory mbc in lmbc)
                {
                    html += "<option value=\"" + mbc.ProductCategoryFK.ToString() + "\"" + (mbc.ProductCategoryFK == productCategoryFK ? " selected" : "") + ">" + mbc.ProductCategory.CategoryName + "</option>";
                }
            }

            return Json(new { isSuccess = true, html = html, msg = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}