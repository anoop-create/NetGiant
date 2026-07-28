using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Import
{
    [Authorize(Roles="IntranetAdmin, PMSAdmin")]
    public class ImportController : Controller
    {
        public ActionResult Index()
        {
            return View(new ImportViewModel());
        }

        public ActionResult Import(HttpPostedFileBase file, ImportViewModel model)
        {
            if (file != null)
            {
                if (Path.GetExtension(file.FileName) != ".csv")
                {
                    ModelState.AddModelError("", "Invalid File Type, CSV Files Only");
                }
                else
                {
                    model.ProcessImport(file, model.Type);
                }
            }
            else
            {
                ModelState.AddModelError("", "Please Select a File");
            }

            return RedirectToAction("Status");
        }

        public ActionResult Status()
        {
            return View(new JobStatusCommonViewModel());
        }

        public JsonResult GetStatus()
        {
            var model = new JobStatusCommonViewModel();

            return Json(model.GetStatus(), JsonRequestBehavior.AllowGet);
        }
    }
}