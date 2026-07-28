using DP001BusinessLogic.Shared;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Utilities;
using DP001Website.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001DataAccess.Entities;

namespace DP001Website.Controllers
{
    [Authorize]
    public class FtpSettingsController : ApplicationController
    {
        public ActionResult Index()
        {
            int channelId = GetChannelId();
            var model = new FtpSettingViewModel(channelId);
            model.GetFtpSettings();

            return View(model);
        }

        public ActionResult New(string feedType)
        {
            int channelId = GetChannelId();
            TenantSetting tenant = GetTenant();
            var model = new FtpSettingViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields
                .Where(x => x.Lookup.LookupName == "Product Inventory Field")
                .ToList();
            model.New(feedType);
            ViewBag.Label_UseLite = "Use Skuuudle Lite";
            ViewBag.Label_LiteZipFile = "Skuuudle Lite Zip File Name";
            if (tenant.Description == "Demo")
            {
                ViewBag.Label_UseLite = "Use Lite";
                ViewBag.Label_LiteZipFile = "Lite Zip File Name";
            }

            return PartialView(model);
        }

        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            TenantSetting tenant = GetTenant();
            var model = new FtpSettingViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields
                .Where(x => x.Lookup.LookupName == "Product Inventory Field")
                .ToList();
            model.Edit(id);
            ViewBag.Label_UseLite = "Use Skuuudle Lite";
            ViewBag.Label_LiteZipFile = "Skuuudle Lite Zip File Name";
            if (tenant.Description == "Demo")
            {
                ViewBag.Label_UseLite = "Use Lite";
                ViewBag.Label_LiteZipFile = "Lite Zip File Name";
            }

            if (model.FTPSettingEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("Main", "TenantSettings");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult Create(FtpSettingViewModel model)
        {
            model.FTPSettingEntry.ChannelFK = GetChannelId();
            if (model.FTPSettingEntry.Suppliers.Count() > 0)
            {
                model.FTPSettingEntry.Suppliers.First().ChannelFK = model.FTPSettingEntry.ChannelFK;
            }
            model.Channel = GetChannel();

            var ftpHostDetails = new Ftp.FtpHostDetails()
            {
                FtpHost = model.FTPSettingEntry.FTPServer,
                FtpUser = model.FTPSettingEntry.FTPUser,
                FtpPassword = model.FTPSettingEntry.FTPPassword,
                FolderPath = model.FTPSettingEntry.FTPPath,
                FileName = model.FTPSettingEntry.FTPFileName,
                Protocol = CommonFunctions.LookupFtpProtocol(model.FTPSettingEntry.FTPProtocolFK)
            };

            if (Ftp.TestFTPConnection(ftpHostDetails))
            {
                var saveReturn = model.Create();
                if (saveReturn.IsSuccess)
                {
                    CommonModel cm = new CommonModel();
                    cm.RefreshTenantSession();

                    int channelId = GetChannelId();
                    var savedModel = new FtpSettingViewModel(channelId);
                    model = savedModel.Edit(model.FTPSettingEntry.FTPSettingsID);

                    ViewBag.FTPProtocol = CommonFunctions.LookupFtpProtocol(model.FTPSettingEntry.FTPProtocolFK);
                    string pv = RenderPartialViewToString("~/Views/FtpSettings/IndexRow.cshtml", model);
                    pv = "<tr class=\"list-form-row\" data-id=\"" + model.FTPSettingEntry.FTPSettingsID + "\">" + pv +
                         "</tr>";
                    return Json(
                        new
                        {
                            isSuccess = true,
                            id = model.FTPSettingEntry.FTPSettingsID,
                            action = "Save",
                            html = pv,
                            msg = ""
                        }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(
                        new
                        {
                            isSuccess = false,
                            id = model.FTPSettingEntry.FTPSettingsID,
                            action = "Save",
                            msg = saveReturn.Message
                        }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new {isSuccess = false, msg = "Could not connect using the FTP settings provided"},
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Update")]
        public JsonResult Update(FtpSettingViewModel model)
        {
            model.FTPSettingEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();

            var ftpHostDetails = new Ftp.FtpHostDetails()
            {
                FtpHost = model.FTPSettingEntry.FTPServer,
                FtpUser = model.FTPSettingEntry.FTPUser,
                FtpPassword = model.FTPSettingEntry.FTPPassword,
                FolderPath = model.FTPSettingEntry.FTPPath,
                FileName = model.FTPSettingEntry.FTPFileName,
                Protocol = CommonFunctions.LookupFtpProtocol(model.FTPSettingEntry.FTPProtocolFK)
            };

            if (Ftp.TestFTPConnection(ftpHostDetails))
            {
                SaveReturn saveReturn = model.Update(model.FTPSettingEntry);
                if (saveReturn.IsSuccess)
                {
                    CommonModel cm = new CommonModel();
                    cm.RefreshTenantSession();

                    ViewBag.FTPProtocol = CommonFunctions.LookupFtpProtocol(model.FTPSettingEntry.FTPProtocolFK);
                    string pv = RenderPartialViewToString("~/Views/FtpSettings/IndexRow.cshtml", model);
                    return Json(
                        new
                        {
                            isSuccess = true,
                            id = model.FTPSettingEntry.FTPSettingsID,
                            action = "Save",
                            html = pv,
                            msg = ""
                        }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(
                        new
                        {
                            isSuccess = false,
                            id = model.FTPSettingEntry.FTPSettingsID,
                            action = "Save",
                            msg = saveReturn.Message
                        }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(
                    new {isSuccess = false, action = "Save", msg = "Could not connect using the FTP settings provided"},
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            var model = new FtpSettingViewModel(channelId);
            SaveReturn sr = model.Delete(id);
            if (sr.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                return Json(new {isSuccess = true, id = id, action = "Save", html = "", msg = ""},
                    JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new {isSuccess = false, id = id, action = "Save", msg = sr.Message},
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [MultipleButton(Name = "action", Argument = "Test")]
        public JsonResult Test(FtpSettingViewModel model)
        {
            var ftpHostDetails = new Ftp.FtpHostDetails()
            {
                FtpHost = model.FTPSettingEntry.FTPServer,
                FtpUser = model.FTPSettingEntry.FTPUser,
                FtpPassword = model.FTPSettingEntry.FTPPassword,
                FolderPath = model.FTPSettingEntry.FTPPath,
                FileName = model.FTPSettingEntry.FTPFileName,
                Protocol = CommonFunctions.LookupFtpProtocol(model.FTPSettingEntry.FTPProtocolFK)
            };

            if (Ftp.TestFTPConnection(ftpHostDetails))
            {
                return Json(new {isSuccess = true, msg = "Test successful", action = "Test"},
                    JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(
                    new {isSuccess = false, msg = "Could not connect using the FTP settings provided", action = "Test"},
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult GetFileHeadings(FtpRequestOptions ftpRequestOptions)
        {
            var ftpHost = new Ftp.FtpHostDetails
            {
                FileName = ftpRequestOptions.FileName,
                FtpHost = ftpRequestOptions.Host,
                FolderPath = ftpRequestOptions.FilePath,
                FtpUser = ftpRequestOptions.Username,
                FtpPassword = ftpRequestOptions.Password,
                Protocol = Ftp.FTPProtocol.FTP,
                BlobContainer = "tenantfolders",
                SavePath = GetChannel().TenantFK + "\\" + ftpRequestOptions.FtpSettingsId + "_" +
                           ftpRequestOptions.FileName
            };

            return Json(CommonFunctions.GetFileHeadings(ftpHost));
        }

        public class FtpRequestOptions
        {
            public string Protocol { get; set; }
            public string Host { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public int FtpSettingsId { get; set; }
        }

    }
}

