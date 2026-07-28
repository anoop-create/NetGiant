using System;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Reports;
using System.IO;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Controllers.Reports
{
    [Authorize(Roles = "IntranetAdmin, Reports")]
    public class ReportsController : Controller
    {
        private const int kpiRetryConst = 5;

        public ActionResult Kpi()
        {
            var model = new ReportsViewModel();

            try
            {
                model = model.GetKpiData(1);
            }
            catch (FileNotFoundException e)
            {

                // Retry if a file not found exception occurs.
                // A maximum of 5 retries with a 1 second delay between each retry.
                // AXIS may be in the process of updating the XML data files.
                if (Session["kpiRetryCount"] != null)
                {
                    int kpiRetryCount = (int)Session["kpiRetryCount"];

                    if (kpiRetryCount <= kpiRetryConst)
                    {
                        Session["kpiRetryCount"] = kpiRetryCount + 1;
                        System.Threading.Thread.Sleep(1000);
                        return RedirectToAction("Kpi");
                    }
                    else
                    {
                        ModelState.AddModelError("", e.Message);
                    }
                }
                else
                {
                    Session["kpiRetryCount"] = 1;
                    return RedirectToAction("Kpi");
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            Session.Remove("kpiRetryCount");
            Response.AddHeader("Refresh", "60");

            model.PwaSettings.IsPwa = true;
            model.PwaSettings.Description = "NetGiant Dashboard";

            return View("KpiIndex", model);
        }

        public ActionResult WallBoard()
        {
            var model = new ReportsViewModel();

            try
            {
                model = model.GetKpiData(1);

                string thisStretch = "stretchTarget" + DateTime.Now.DayOfWeek.ToString();
                string thisBase = "baseTarget" + DateTime.Now.DayOfWeek.ToString();

                Session.Add("StretchTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisStretch));
                Session.Add("BaseTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisBase));
                Session.Add("TargetsAreRevenue", SharedFunctions.GetConfigurationSetting("WallBoard", "targetsAreRevenue"));
            }
            catch (FileNotFoundException e)
            {

                // Retry if a file not found exception occurs.
                // A maximum of 5 retries with a 1 second delay between each retry.
                // AXIS may be in the process of updating the XML data files.
                if (Session["kpiRetryCount"] != null)
                {
                    int kpiRetryCount = (int)Session["kpiRetryCount"];

                    if (kpiRetryCount <= kpiRetryConst)
                    {
                        Session["kpiRetryCount"] = kpiRetryCount + 1;
                        System.Threading.Thread.Sleep(1000);
                        return RedirectToAction("WallBoard");
                    }
                    else
                    {
                        ModelState.AddModelError("", e.Message);
                    }
                }
                else
                {
                    Session["kpiRetryCount"] = 1;
                    return RedirectToAction("WallBoard");
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            ViewBag.website = 1;
            ViewBag.targetsAreRevenue = Session["TargetsAreRevenue"];

            model.PwaSettings.IsPwa = true;
            model.PwaSettings.Description = "NetGiant Dashboard";

            return View(model);
        }

        public ActionResult WallBoardData(int website, bool targetsAreRevenue)
        {
            var model = new ReportsViewModel();

            try
            {
                model = model.GetKpiData(website);

                string thisStretch = "stretchTarget" + DateTime.Now.DayOfWeek.ToString();
                string thisBase = "baseTarget" + DateTime.Now.DayOfWeek.ToString();


                if (Session["StretchTarget"] == null)
                    Session.Add("StretchTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisStretch));

                if (Session["BaseTarget"] == null)
                    Session.Add("BaseTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisBase));

                if (Session["TargetsAreRevenue"] == null)
                    Session.Add("TargetsAreRevenue", SharedFunctions.GetConfigurationSetting("WallBoard", "targetsAreRevenue"));

            }
            catch (FileNotFoundException e)
            {

                // Retry if a file not found exception occurs.
                // A maximum of 5 retries with a 1 second delay between each retry.
                // AXIS may be in the process of updating the XML data files.
                if (Session["kpiRetryCount"] != null)
                {
                    int kpiRetryCount = (int)Session["kpiRetryCount"];

                    if (kpiRetryCount <= kpiRetryConst)
                    {
                        Session["kpiRetryCount"] = kpiRetryCount + 1;
                        System.Threading.Thread.Sleep(1000);
                        return RedirectToAction("WallBoardData", new { website = website });
                    }
                    else
                    {
                        ModelState.AddModelError("", e.Message);
                    }
                }
                else
                {
                    Session["kpiRetryCount"] = 1;
                    return RedirectToAction("WallBoardData", new { website = website });
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            ViewBag.website = website;
            ViewBag.targetsAreRevenue = targetsAreRevenue;

            return PartialView("_WallBoardData", model);
        }

        public ActionResult WallBoardTargets()
        {
            var model = new ReportsViewModel();
            return PartialView("_WallBoardEditTargets", model.GetWallBoardTargets());
        }

        public ActionResult UpdateWallBoardTargets(string[] optionsArray)
        {
            string result = ReportsViewModel.SetWallBoardTargets(optionsArray);
            ViewBag.Result = result;

            string thisStretch = "stretchTarget" + DateTime.Now.DayOfWeek.ToString();
            string thisBase = "baseTarget" + DateTime.Now.DayOfWeek.ToString();

            Session.Add("StretchTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisStretch));
            Session.Add("BaseTarget", SharedFunctions.GetConfigurationSetting("WallBoard", thisBase));
            Session.Add("TargetsAreRevenue", SharedFunctions.GetConfigurationSetting("WallBoard", "targetsAreRevenue"));

            return PartialView("_WallBoardEditTargets", null);
        }

        public ActionResult BatchLogs()
        {
            var model = new ReportsViewModel();

            return View("BatchLogs", model);
        }

        public ActionResult ReadLog(string logName)
        {
            var model = new ReportsViewModel();

            return PartialView("_LogContent", model.ReadLog(logName).LogContent);
        }

        public ActionResult Log_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel();
            model.GetLogs();

            var result = model.LogList.ToDataSourceResult(request);
            var jsonResult = Json(result, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
    }
}
