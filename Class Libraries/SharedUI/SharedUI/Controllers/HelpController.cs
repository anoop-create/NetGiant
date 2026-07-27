using BusinessLogic.ViewModels;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class HelpController : Controller
    {
        private CommonViewModel model;
        public ActionResult Index(string id)
        {
            model = new CommonViewModel();

            return View(model);
        }
    }
}