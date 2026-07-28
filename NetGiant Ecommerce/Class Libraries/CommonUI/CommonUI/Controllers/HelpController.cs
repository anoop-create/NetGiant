using BusinessLogic;
using BusinessLogic.ViewModels;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;

namespace CommonUI.Controllers
{
    [SiteOfflineCheck]
    public class HelpController : Controller
    {
        private CommonViewModel model;
        public ActionResult Index(string id)
        {
            model = new CommonViewModel
            {
                SignUp = new SignUp(),
                SignIn = new SignIn()
            };

            return View(model);
        }
    }
}