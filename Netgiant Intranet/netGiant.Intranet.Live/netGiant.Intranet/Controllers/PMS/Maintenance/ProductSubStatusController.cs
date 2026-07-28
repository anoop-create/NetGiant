using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    //[Authorize]
    //public class ProductSubStatusController : Controller
    //{
    //    private ngmdEntities db = new ngmdEntities();

    //    public ActionResult Index(int? page, string search, string searchBy)
    //    {
    //        int pageSize = 21;
    //        int pageNumber = (page ?? 1);
    //        PagedList.IPagedList<productSubStatus> pssList = null;

    //        if (!string.IsNullOrEmpty(search))
    //        {
    //            switch (searchBy)
    //            {
    //                default:
    //                    pssList = db.productSubStatus.Where(x => x.productSubStatusName.Contains(search))
    //                        .OrderBy(x => x.productSubStatusID).ToPagedList(pageNumber, pageSize);
    //                    break;
    //            }
    //        }
    //        else
    //        {
    //            pssList = (from ps in db.productSubStatus orderby ps.productSubStatusID select ps).ToPagedList(pageNumber, pageSize);
    //        }

    //        return PartialView("~/Views/PMS/Maintenance/ProductSubStatus/Index.cshtml", pssList);
    //    }

    //    [HttpPost]
    //    public ActionResult Create(int? id)
    //    {
    //        if (!id.HasValue)
    //        {
    //            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
    //        }
    //        productSubStatus psStatus = id.HasValue && id.Value > 0 ? db.productSubStatus.Find(id) : new productSubStatus();
    //        if (psStatus == null)
    //        {
    //            return HttpNotFound();
    //        }

    //        return PartialView("~/Views/PMS/Maintenance/ProductSubStatus/Create.cshtml", psStatus);
    //    }

    //    [HttpPost]
    //    public ActionResult Save(productSubStatus psStatus)
    //    {
    //        if (ModelState.IsValid)
    //        {
    //            if (psStatus.productSubStatusID > 0)
    //            {
    //                db.Entry(psStatus).State = EntityState.Modified;
    //            }
    //            else
    //            {
    //                db.productSubStatus.Add(psStatus);
    //            }

    //            db.SaveChanges();

    //            return RedirectToAction("Index");
    //        }

    //        return PartialView("~/Views/PMS/Maintenance/ProductSubStatus/Create.cshtml", psStatus);
    //    }

    //    public ActionResult Delete(int? id)
    //    {
    //        if (id == null)
    //        {
    //            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
    //        }
    //        productSubStatus psStatus = db.productSubStatus.Find(id);
    //        if (psStatus == null)
    //        {
    //            return HttpNotFound();
    //        }

    //        db.productSubStatus.Remove(psStatus);
    //        db.SaveChanges();

    //        return RedirectToAction("Index");
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            db.Dispose();
    //        }
    //        base.Dispose(disposing);
    //    }
    //}
}
