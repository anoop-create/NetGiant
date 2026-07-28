using netGiant.Intranet.DataLayer;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;

namespace netGiant.Intranet.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        public ApplicationController() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NoLockInterceptor.ApplyNoLock = false;
                //db.Dispose();
            }
            base.Dispose(disposing);
        }

        public string RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }
    }
}