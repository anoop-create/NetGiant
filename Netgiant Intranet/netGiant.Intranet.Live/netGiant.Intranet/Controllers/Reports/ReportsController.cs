using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Reports;
using System.IO;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;

namespace netGiant.Intranet.Controllers.Reports
{
    [Authorize(Roles = "IntranetAdmin, Reports")]
    public class ReportsController : Controller
    {
        private const int kpiRetryConst = 5;

        public ActionResult Kpi()
        {
            ReportsViewModel repVm = new ReportsViewModel();

            try
            {
                repVm = repVm.GetKpiData();
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
            return View("~/Views/Reports/KpiIndex.cshtml", repVm);
        }

        public ActionResult WallBoard()
        {
            ReportsViewModel repVm = new ReportsViewModel();

            try
            {
                repVm = repVm.GetKpiData();
                Session.Add("StretchTarget", SharedFunctions.GetConfigurationSetting("WallBoard", "stretchTarget"));
                Session.Add("BaseTarget", SharedFunctions.GetConfigurationSetting("WallBoard", "baseTarget"));
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
            return View("~/Views/Reports/WallBoard.cshtml", repVm);
        }

        public ActionResult WallBoardData(int website)
        {
            ReportsViewModel repVm = new ReportsViewModel();

            try
            {
                repVm = repVm.GetKpiData();

                if (Session["StretchTarget"] == null)
                    Session.Add("StretchTarget", SharedFunctions.GetConfigurationSetting("WallBoard", "stretchTarget"));

                if (Session["BaseTarget"] == null)
                    Session.Add("BaseTarget", SharedFunctions.GetConfigurationSetting("WallBoard", "baseTarget"));

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

            return PartialView("~/Views/Reports/WallBoardData.cshtml", repVm);
        }

        public ActionResult WallBoardTargets()
        {
            ReportsViewModel model = new ReportsViewModel();
            return PartialView("~/Views/Reports/WallBoardEditTargets.cshtml",
                model.GetWallBoardTargets());
        }

        public ActionResult UpdateWallBoardTargets(string[] optionsArray)
        {
            string result = ReportsViewModel.SetWallBoardTargets(Convert.ToInt32(optionsArray[0]),
                                Convert.ToInt32(optionsArray[1]));

            Session["StretchTarget"] = optionsArray[0];
            Session["BaseTarget"] = optionsArray[1];
            ViewBag.Result = result;

            return PartialView("~/Views/Reports/WallBoardEditTargets.cshtml", null);
        }

        public ActionResult BatchLogs()
        {
            ReportsViewModel repVm = new ReportsViewModel();
            repVm.ListBatchLogs();

            return View("~/Views/Reports/BatchLogs.cshtml", repVm.LogFiles);
        }

        public ActionResult ReadLog(string logName)
        {
            ReportsViewModel repVm = new ReportsViewModel();

            return PartialView("~/Views/Reports/LogContent.cshtml", repVm.ReadLog(logName).LogContent);
        }
    }
}
