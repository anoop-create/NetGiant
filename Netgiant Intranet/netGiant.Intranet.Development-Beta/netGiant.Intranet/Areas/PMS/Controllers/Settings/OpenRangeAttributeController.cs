using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    public class OpenRangeAttributeController : Controller
    {
        //public ActionResult Replacements()
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    return View("OpenRangeReplacementIndex", model);
        //}

        //public ActionResult OpenRangeReplacement_Read([DataSourceRequest]DataSourceRequest request)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    model.GetReplacementAttributes();

        //    var result = model.OpenRangeReplacementList.ToDataSourceResult(request);
        //    var jsonResult = Json(result);
        //    jsonResult.MaxJsonLength = int.MaxValue;
        //    return jsonResult;
        //}

        //public JsonResult CreateReplacement(int type, string original, string replacement, int nameId = 0, int productId = 0)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = new SaveReturn();
        //    var searchableId = 0;
        //    sr.IsSuccess = true;

        //    switch (type)
        //    {
        //        case 0:
        //            break;
        //        case 1:
        //            searchableId = model.GetSearchableId(type, nameId, productId);
        //            break;
        //        case 2:
        //            searchableId = model.GetSearchableId(type, nameId);
        //            break;
        //        default:
        //            sr.IsSuccess = false;
        //            sr.Message = "Error with Replacement Type";
        //            break;
        //    }

        //    if (sr.IsSuccess != false)
        //    {
        //        sr = model.CreateReplacement(type, searchableId, original, replacement);
        //    }

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //public JsonResult SaveReplacement(int replacementId, int type, string original, string replacement, int nameId = 0, int productId = 0)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = new SaveReturn();
        //    var searchableId = 0;
        //    sr.IsSuccess = true;

        //    switch (type)
        //    {
        //        case 0:
        //            break;
        //        case 1:
        //            searchableId = model.GetSearchableId(type, nameId, productId);
        //            break;
        //        case 2:
        //            searchableId = model.GetSearchableId(type, nameId);
        //            break;
        //        default:
        //            sr.IsSuccess = false;
        //            sr.Message = "Error with Replacement Type";
        //            break;
        //    }

        //    if (sr.IsSuccess != false)
        //    {
        //        sr = model.SaveReplacement(replacementId, type, searchableId, original, replacement);
        //    }

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //public JsonResult GetAttributeNames()
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = model.GetAttributeNames();

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //public JsonResult GetAttributeValues(int nameId)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = model.GetAttributeValues(nameId);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //public JsonResult GetAttributeProducts(int nameId)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = model.GetAttributeProducts(nameId);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //public JsonResult DeleteReplacement(int id)
        //{
        //    var model = new OpenRangeAttributeViewModel();
        //    var sr = model.DeleteReplacement(id);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}
    }
}