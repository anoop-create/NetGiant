using netGiant.Intranet.BusinessLayer.ViewModels.QA;
using netGiant.Intranet.ViewModels;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}