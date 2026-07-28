using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data;
using System.Xml;
using System.Web;
using System.Linq.Expressions;
using System.Net;
using System.ComponentModel;
using static Lucene.Net.Index.SegmentReader;
using Nest;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class StaticFiles
    {
        public static void Control(Dictionary<string, string> parms)
        {
            switch (parms["subtype"])
            {
                case "mastheadmenu":
                    CreateMastheadMenu(parms, false);
                    CreateMastheadMenu(parms, true);
                    break;
                //Remove - no longer used
                case "newmastheadmenu":
                    CreateMastheadMenu(parms, false);
                    CreateMastheadMenu(parms, true);
                    break;
                case "footerstd":
                    CreateFooterStd(parms);
                    break;
                case "scripts":
                    CreateScriptFiles(parms);
                    break;
            }
        }

        private static void CreateMastheadMenu(Dictionary<string, string> parms, bool forMobile)
        {
            // ------------------------------------------------------------------------------------------- //
            // Key                                                                                         //
            // Element   Type 1         Type 2       Type 3       Type 4        Type 5      Type 6         //
            //           Sngl Link      Cartridges   Sngl Cat     Multi Cat                 CMS Entry      //
            // 0         Menu Type      Menu Type    Menu Type    Menu Type                 Menu Type      //
            // 1         Menu is New    Menu is New  Menu is New  Menu is New               Menu is New    //
            // 2         Menu Text      Menu Text    Menu Text    Menu Text                 Menu Text      //
            // 3         ID             ID           ID           ID                                       //
            // 4         Url            Cart Type    Group No     Group No                                 //
            // 5         --             Negate       --           Addon Links                              //
            // 6         --             --           --           Url                                      //
            // ------------------------------------------------------------------------------------------- //
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;
            string errorMessage = "";
            int websiteId = Convert.ToInt32(parms["websiteid"]);

            //string html = "";
            StringBuilder html = new StringBuilder();
            StringBuilder html2 = new StringBuilder(); //For mobile sub menus

            //Initialise Config Settings
            EntityFunctions.SetSiteConfigSettings(websiteId);

            //string addressPrefix = "";
            //if (EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "UseHTTPS").FirstOrDefault().settingValue == "True")
            //{
            //    addressPrefix = "https://";
            //}
            //else
            //{
            //    addressPrefix = "http://";
            //}

            try
            {
                string siteRoot = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "siteRoot")
                    .FirstOrDefault().settingValue;
                string version = EntityFunctions.WebsiteConfigSettings
                    .Where(x => x.settingName == "VersionNumber").FirstOrDefault().settingValue;
                string cdn = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "CDN")
                    .FirstOrDefault().settingValue;
                cdn = cdn.Replace("[version]", version);

                //Get the Menu Control Settings for this website
                List<configurationSetting> menus;
                menus = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName.StartsWith("menu"))
                    .OrderBy(y => y.settingName).ToList();
                string siteCode = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "site")
                    .FirstOrDefault().settingValue;

                //Build the html file
                int i = 0;
                int count = menus.Count();

                html.AppendLine("<h3 style=\"color: white; \" class=\"visible-xs visible-sm\">Main Menu</h3>");
                html.AppendLine("<ul class=\"nav navbar-nav list-unstyled\" id=\"dynamicNav\">");
                if (!forMobile)
                {
                    html.AppendLine("<li class=\"hidden-xs hidden-sm\">");
                    html.AppendLine("<a href=\"/\"><i class=\"fa fa-home fa-2x\" aria-hidden=\"true\"></i></a>");
                    html.AppendLine("</li>");
                }
                foreach (configurationSetting cs in menus)
                {
                    string[] menuSetting = cs.settingValue.Split('|');
                    string newSash = "";
                    string liClass = "";

                    liClass = "nav-normalListItem";
                    if (++i == count) //this is the last item
                    {
                        liClass = "nav-lastListItem";
                    }
                    if (menuSetting[1] == "1")
                    {
                        newSash =
                            "<img src=\"/Content/Images/1pxTrans.png\" class=\"msp_navTabNewLabel g-ps-a nav-newTabLabel\" alt=\"New\" />" +
                            Environment.NewLine;
                        liClass += " nav-purpleListItem";
                    }
                    switch (menuSetting[0])
                    {
                        case "1":
                            {
                                html.AppendLine("<li id=\"menuItem" + menuSetting[3].Replace(" ", "") +
                                                "\" class=\"nav-label1 " + liClass + "\"><a href=\"" + siteRoot +
                                                menuSetting[4] +
                                                "\" class=\"nav-homeLink g-ps-r g-a-c g-d-b g-fc-1\">" +
                                                menuSetting[2] + newSash + "</a></li>");
                                break;
                            }

                        case "2":
                            {
                                string typeName = "";
                                int typeId = Convert.ToInt32(menuSetting[4]);
                                Expression<Func<eqEquipment, bool>> where;
                                where = (x => x.eqCartridgeTypeFK == typeId && x.statusFK == 1);
                                if (menuSetting.Count() > 5)
                                {
                                    if (int.Parse(menuSetting[5]) == 1)
                                    {
                                        where = (x => x.eqCartridgeTypeFK != typeId 
                                                    && x.eqCartridgeTypeFK != 8 // 8 = Toner Range
                                                    && x.eqCartridgeTypeFK != 9 // 9 = Ink Range
                                                    && x.statusFK == 1); 
                                    }
                                }

                                typeName = menuSetting[3].ToLower() + "-cartridges";
                                html.AppendLine("<li>");
                                //html.AppendLine("<a href=\"/" + typeName + "/\" class=\"topLevelLink\">" +
                                //                menuSetting[2] + newSash +
                                //                "<i class=\"fa fa-chevron-down pull-right hidden-xs hidden-sm g-p-r-10\" aria-hidden=\"true\"></i></a>");
                                html.AppendLine("<a href=\"/" + typeName + "/\" class=\"topLevelLink hidden-xs hidden-sm\">" +
                                    menuSetting[2] + newSash + "</a>");
                                html.AppendLine("<div class=\"navbar-link visible-xs visible-sm g-lh-40\" data-cat=\"" + menuSetting[2].Replace(" ", "") + 
                                    "\">" + menuSetting[2] + "<i class=\"fa fa-chevron-right pull-right g-p-r-20 g-lh-40\" aria-hidden=\"true\"></i></div>");

                                html2.AppendLine("<ul id=\"" + menuSetting[2].Replace(" ", "") + "\" class=\"g-d-n nav navbar-slide list-unstyled\">");
                                html2.AppendLine("<div class=\"clearfix g-m-t-20 g-m-b-10\">");
                                html2.AppendLine("<div class=\"g-f-l g-p-l-15 g-fs-lg g-fw-b g-fc-st\">" + menuSetting[2] + "</div>");
                                html2.AppendLine("<div class=\"navbar-slide-close g-f-r g-fc-st g-p-t-10 g-p-r-10\">");
                                html2.AppendLine("<i class=\"fa fa-chevron-left g-p-r-10\" aria-hidden=\"true\"></i>Back");
                                html2.AppendLine("</div>");
                                html2.AppendLine("</div>");
                                html2.AppendLine("<div class=\"clearfix\"></div>");

                                if (!forMobile)
                                {
                                    html.AppendLine("<div class=\"g-d-n\">");
                                    html.AppendLine("<div class=\"g-va-t text-left col-lg-3 visible-lg g-p-l-0\">" +
                                                    EntityFunctions.GetNgmdCMSEntry(websiteId, "MenuData",
                                                        cs.settingName + "text") + "</div>");
                                    html.AppendLine("<ul class=\"list-unstyled col-md-12 col-lg-9 g-p-0 pull-right\">");
                                }

                                //Get All the manufactures for this cartridge type
                                string ct = menuSetting[2];

                                var manulist = EntityFunctions.GetEquipmentManufacturers(where);

                                //Build the Sub Menu for Desktop
                                for (i = 0; i <= manulist.Count - 1; i++)
                                {
                                    var manu = manulist[i];
                                    //string linkName = manu.Item1.Length > 11 ? manu.Item1.Substring(0, 11) : manu.Item1;
                                    string linkName = manu.Item1;
                                    typeName = manu.Item2.ToLower().Replace(" ", "-");
                                    html2.AppendLine("<li><a title=\"" + manu.Item1 + " " + menuSetting[2] +
                                        "\" class=\"navbar-link\" href=\"/" + typeName + "/" + manu.Item1.Replace(" ", "-") 
                                        + "/\">" + linkName + "</a></li>");

                                    if (!forMobile)
                                    {
                                        if (i == manulist.Count - 1 || manulist[i + 1].Item1 != manu.Item1)
                                        {
                                            //typeName = manu.Item2.ToLower().Replace(" ", "-");
                                            html.AppendLine("<li class=\"g-ps-r g-hoverbox g-b-1-p gm-d-n\">");
                                            html.AppendLine("<a title=\"" + manu.Item1 + " " + menuSetting[2] +
                                                            "\" href=\"/" + typeName + "/" + manu.Item1.Replace(" ", "-") +
                                                            "/\">");
                                            html.AppendLine("<img src=\"/Content/Images/1pxTrans.png\" class=\"pbs_" +
                                                            manu.Item3.ToString() + " hidden-xs\" alt=\"" + manu.Item1 +
                                                            " " + menuSetting[2] + "\">");
                                            html.AppendLine("</a>");
                                            html.AppendLine("</li>");
                                        }
                                        else
                                        {
                                            //typeName = manu.Item2.ToLower().Replace(" ", "-");
                                            string desc = manu.Item2.Replace(" Cartridges", "");
                                            string typeName2 = manulist[i + 1].Item2.ToLower().Replace(" ", "-");
                                            string desc2 = manulist[i + 1].Item2.Replace(" Cartridges", "");

                                            html.AppendLine("<li class=\"hm-brandWizardEntry g-ps-r\">");
                                            html.AppendLine(
                                                "<img src=\"/Content/Images/1pxTrans.png\" class=\"g-b-1-p pbs_" +
                                                manu.Item3.ToString() + " hidden-xs\" alt=\"" + manu.Item1 + " " +
                                                menuSetting[2] + "\">");
                                            html.AppendLine(
                                                "<div class=\"hm-bw-links g-ps-a g-p-10 g-bc-lg\" style=\"display: none;\">");
                                            html.AppendLine(
                                                "<div class=\"g-butt-secondary hm-bw-linksButton g-b-1-w g-m-b-10\"><a href=\"/" +
                                                typeName + "/" + manu.Item1.Replace(" ", "-") + "/\">" + desc +
                                                "</a></div>");
                                            html.AppendLine(
                                                "<div class=\"g-butt-secondary hm-bw-linksButton g-b-1-w\"><a href=\"/" +
                                                typeName2 + "/" + manulist[i + 1].Item1.Replace(" ", "-") + "/\">" + desc2 +
                                                "</a></div>");
                                            html.AppendLine("</div>");
                                            html.AppendLine("<span class=\"visible-xs\">" + manu.Item1 + " " +
                                                            menuSetting[2] + "</span>");
                                            html.AppendLine("</li>");

                                            i += 1;
                                        }
                                    }
                                }
                                if (!forMobile)
                                {
                                    html.AppendLine("</ul>");
                                    html.AppendLine("</div>");                                                                       
                                }
                                html.AppendLine("</li>");                                 
                                html2.AppendLine("</ul>");
                                break;
                            }

                        case "3":
                            {
                                categoryCode parentGroup;
                                List<categoryCode> categoryCodes = null;
                                string menuGroupNo1 = menuSetting[4];
                                string url = "";

                                parentGroup = EntityFunctions.GetCategoryCodeList(x => x.AXISGroupNo.Trim() == menuGroupNo1).FirstOrDefault();
                                if (parentGroup != null)
                                {
                                    categoryCodes = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == parentGroup.categoryCodeID)
                                        .OrderBy(x => x.categoryCodeName)
                                        .ToList();
                                    url = "/catalogue/" +
                                            HttpUtility.UrlEncode(parentGroup.categoryCodeName.Replace(" ", "-")) + "-" +
                                            menuSetting[4] + "/";
                                    html.AppendLine("<li>");
                                    html.AppendLine("<a href=\"" + url + "\" class=\"topLevelLink hidden-xs hidden-sm\">" +
                                        menuSetting[2] + newSash + "</a>");
                                    html.AppendLine("<div class=\"navbar-link visible-xs visible-sm g-lh-40\" data-cat=\"" + menuSetting[2].Replace(" ", "") +
                                        "\">" + menuSetting[2] + "<i class=\"fa fa-chevron-right pull-right g-p-r-20 g-lh-40\" aria-hidden=\"true\"></i></div>");

                                    html2.AppendLine("<ul id=\"" + menuSetting[2].Replace(" ", "") + "\" class=\"g-d-n nav navbar-slide list-unstyled\">");
                                    html2.AppendLine("<div class=\"clearfix g-m-t-20 g-m-b-10\">");
                                    html2.AppendLine("<div class=\"g-f-l g-p-l-15 g-fs-lg g-fw-b g-fc-st\">" + menuSetting[2] + "</div>");
                                    html2.AppendLine("<div class=\"navbar-slide-close g-f-r g-fc-st g-p-t-10 g-p-r-10\">");
                                    html2.AppendLine("<i class=\"fa fa-chevron-left g-p-r-10\" aria-hidden=\"true\"></i>Back");
                                    html2.AppendLine("</div>");
                                    html2.AppendLine("</div>");
                                    html2.AppendLine("<div class=\"clearfix\"></div>");

                                    if (!forMobile)
                                    {
                                        html.AppendLine("<div class=\"g-d-n\">");
                                        html.AppendLine("<div class=\"g-va-t text-left col-lg-3 visible-lg g-p-l-0\">" +
                                                        EntityFunctions.GetNgmdCMSEntry(websiteId, "MenuData",
                                                            cs.settingName + "text") + "</div>");
                                        html.AppendLine("<ul class=\"list-unstyled col-md-12 col-lg-9 g-p-0 pull-right\">");
                                    }

                                    foreach (categoryCode cc in categoryCodes)
                                    {
                                        var cnt1 = cc.websiteInventory.Count(x => x.product.productStatusFK == 1 || x.product.productStatusFK == 8);
                                        var cnt2 = cc.secondaryCategoryLookup.Count(x => x.websiteInventory.product.productStatusFK == 1 || x.websiteInventory.product.productStatusFK == 8);

                                        if (cnt1 > 0 || cnt2 > 0)
                                        {
                                            url = "/products/" +
                                                HttpUtility.UrlEncode(cc.categoryCodeName.Replace(" ", "-")) + "-" +
                                                cc.AXISGroupNo.Trim() + "/";
                                            if (!forMobile)
                                            {
                                                html.AppendLine("<li>");
                                                html.AppendLine("<a class=\"primary\" title=\"" + cc.categoryCodeName +
                                                    "\" href=\"" + url + "\">");
                                                html.AppendLine("<div class=\"hidden-xs g-p-b-10\"><strong>" +
                                                    cc.categoryCodeName + "</strong></div>");
                                                html.AppendLine("<div class=\"hidden-xs\"><img src=\"/Content/Images/1pxTrans.png\"  data-original=\"" + cdn +
                                                    "/Images/category-icons/" +
                                                    cc.categoryCodeName.Replace("& ", "").Replace(" ", "-")
                                                        .ToLower() +
                                                    ".jpg\" class=\"lazy g-b-1-p g-h-100 g-hoverbox\" alt=\"" +
                                                    cc.categoryCodeName + "\"></div>");
                                                html.AppendLine("<span class=\"visible-xs\">" + cc.categoryCodeName +
                                                    "</span>");
                                                html.AppendLine("</a>");
                                                html.AppendLine("</li>");
                                            }
                                            //string linkName = cc.categoryCodeName.Length > 11 ? cc.categoryCodeName.Substring(0, 11) : cc.categoryCodeName;
                                            string linkName = cc.categoryCodeName;
                                            html2.AppendLine("<li><a title=\"" + cc.categoryCodeName + 
                                                "\" class=\"navbar-link\" href=\"" + url + "\">" + linkName + "</a></li>");
                                        }
                                    }
                                    if (!forMobile)
                                    {
                                        html.AppendLine("</ul>");
                                        html.AppendLine("</div>");                                                                              
                                    }
                                    html.AppendLine("</li>");
                                    html2.AppendLine("</ul>");                                      
                                }
                                break;
                            }

                        case "4":
                            {
                                string href = (menuSetting[6].Substring(0, 10) == "javascript")
                                    ? menuSetting[6]
                                    : siteRoot + menuSetting[6];
                                html.AppendLine("<li>");
                                html.AppendLine("<a href=\"" + href + "\" class=\"topLevelLink\">" + menuSetting[2] +
                                                newSash +
                                                "<i class=\"fa fa-chevron-down pull-right hidden-xs hidden-sm g-p-r-10\" aria-hidden=\"true\"></i></a>");
                                if (!forMobile)
                                {
                                    html.AppendLine("<ul class=\"list-unstyled hide\">");

                                    if (menuSetting[5] == "1")
                                    {
                                        //html.Append(StandardFunctions.GetCMSEntry(websiteId, 45, 2, "T"));
                                    }
                                    else
                                    {
                                        //Get a list of all the Top Groups associated with the parent group
                                        categoryCode parentCC;
                                        string menuGroupNo2 = menuSetting[4];
                                        parentCC = EntityFunctions.GetCategoryCodeList(x => (x.AXISGroupNo ?? "0").Trim() == menuGroupNo2).FirstOrDefault();

                                        if (parentCC != null)
                                        {
                                            // Get top categories where at least one sub category contains an active product
                                            List<categoryCode> topCC = null;
                                            topCC = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == parentCC.categoryCodeID)
                                                .OrderBy(x => x.categoryCodeName)
                                                .ToList();

                                            //For Each Top Group Write a submenu header
                                            foreach (categoryCode tc in topCC)
                                            {
                                                if (CategoryHasProducts(tc))
                                                {
                                                    string cURL =
                                                        siteRoot + "catalogue/" +
                                                        StandardFunctions.CleanupURL(tc.categoryCodeName
                                                            .Replace(" ", "-")) + "-" + (tc.AXISGroupNo ?? "0").Trim() +
                                                        "/";
                                                    html.AppendLine("<li>");
                                                    html.AppendLine("<a href=\"" + cURL + "\">" + tc.categoryCodeName +
                                                                    "<i class=\"fa fa-chevron-down pull-right g-p-r-10\" aria-hidden=\"true\"></i></a>");
                                                    html.AppendLine("<ul class=\"list-unstyled hide\">");

                                                    //Get A list of all Sub Groups associated with this Top Group which contain at least one active product
                                                    List<categoryCode> subCC = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == tc.categoryCodeID)
                                                        .OrderBy(x => x.categoryCodeName)
                                                        .ToList();

                                                    //For Each Sub Group write a menu item
                                                    foreach (categoryCode sc in subCC)
                                                    {
                                                        if (ValidCategory(sc))
                                                        {
                                                            string scURL =
                                                                siteRoot + "products/" +
                                                                HttpUtility.HtmlEncode(sc.categoryCodeName)
                                                                    .Replace(" ", "-").Replace("-&amp;-", "-") + "-" +
                                                                (sc.AXISGroupNo ?? "0").Trim() + "/";
                                                            html.AppendLine("<li>");
                                                            html.AppendLine(
                                                                "<a href=\"" + scURL + "\">" + sc.categoryCodeName +
                                                                "</a>");
                                                            html.AppendLine("</li>");
                                                        }
                                                    }
                                                    html.AppendLine("</ul></li>");
                                                    //html.AppendLine("<!--[if lte IE 9]></ul></li><![endif]-->");
                                                }
                                            }
                                        }
                                    }
                                    html.AppendLine("</ul>");
                                    html.AppendLine("</li>");
                                }

                                break;
                            }

                        case "5":
                            {
                                //string href = (menuSetting[6].Substring(0, 10) == "javascript")
                                //    ? menuSetting[6]
                                //    : siteRoot + menuSetting[6];     

                                string href = menuSetting[6];

                                html.AppendLine("<li id=\"menuItem" + menuSetting[3].Replace(" ", "") +
                                                "\" class=\"nav-label1 g-ps-r-i " + liClass + "\">");
                                html.AppendLine("<a href=\"" + href +
                                                "\" class=\"nav-mainLink g-a-c g-fc-1 g-ps-r topLevelLink hidden-md hidden-lg\">" +
                                                menuSetting[2] + newSash + "</a>");
                                if (!forMobile)
                                {
                                    html.AppendLine("<a href=\"javascript:void(0);\"class=\"nav-mainLink g-a-c g-fc-1 g-ps-r hidden-xs hidden-sm\">" +
                                                    menuSetting[2] + newSash + "</a>");

                                    html.AppendLine("<ul class=\"nav-level2 g-ps-a g-d-n g-bsz-bb g-bc-s hidden-xs hidden-sm\">");
                                    if (menuSetting[5] == "1")
                                    {
                                        //html.AppendLine(StandardFunctions.GetCMSEntry(websiteId, 45, 2, "T"));
                                        html.AppendLine(EntityFunctions.GetCMSEntry(3, 45, 2, "T"));
                                    }
                                    else
                                    {
                                        //Get a list of all the Top Groups associated with the parent group
                                        categoryCode parentCC;
                                        string menuGroupNo2 = menuSetting[4];
                                        parentCC = EntityFunctions.GetCategoryCodeList(x => (x.AXISGroupNo ?? "0").Trim() == menuGroupNo2).FirstOrDefault();

                                        if (parentCC != null)
                                        {
                                            // Get top categories where at least one sub category contains an active product
                                            List<categoryCode> topCC = null;
                                            topCC = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == parentCC.categoryCodeID)
                                                .OrderBy(x => x.categoryCodeName)
                                                .ToList();

                                            //For Each Top Group Write a submenu header
                                            foreach (categoryCode tc in topCC)
                                            {
                                                if (CategoryHasProducts(tc))
                                                {
                                                    string cURL =
                                                        siteRoot + "catalogue/" +
                                                        StandardFunctions.CleanupURL(tc.categoryCodeName
                                                            .Replace(" ", "-")) + "-" + (tc.AXISGroupNo ?? "0").Trim() +
                                                        "/";
                                                    html.AppendLine(
                                                        "<!--[if lte IE 9]><li class=\"nav_css2\"><ul><![endif]-->");
                                                    //html.AppendLine("<li class=\"nav_menuSubTitleSep\"></li>");
                                                    html.AppendLine(
                                                        "<li class=\"nav-label2 nav_menuSubTitle g-fc-5 g-fs-11 g-fw-b\">");
                                                    html.AppendLine("<span class=\"nav-label2Chevron1 g-fc-s\"><i class=\"fa fa-chevron-left g-fs-xs-i g-p-l-5\"></i></span>");
                                                    html.AppendLine("<span class=\"nav-label2Label\"><a href=\"//" + cURL + "\">" + tc.categoryCodeName + "</a></span>");
                                                    html.AppendLine("<span class=\"nav-label2Chevron2 g-fc-s\"><i class=\"fa fa-chevron-right g-fs-xs-i g-p-l-5\"></i></span>");
                                                    html.AppendLine("<ul class=\"nav-level3 g-d-n\">");

                                                    //Get A list of all Sub Groups associated with this Top Group which contain at least one active product
                                                    List<categoryCode> subCC = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == tc.categoryCodeID)
                                                        .OrderBy(x => x.categoryCodeName)
                                                        .ToList();

                                                    //For Each Sub Group write a menu item
                                                    foreach (categoryCode sc in subCC)
                                                    {
                                                        if (ValidCategory(sc))
                                                        {
                                                            string scURL =
                                                                siteRoot + "products/" +
                                                                HttpUtility.HtmlEncode(sc.categoryCodeName)
                                                                    .Replace(" ", "-").Replace("-&amp;-", "-") + "-" +
                                                                (sc.AXISGroupNo ?? "0").Trim() + "/";
                                                            html.AppendLine("<li class=\"nav-label3\"><a href=\"//" + scURL +
                                                                            "\">" + sc.categoryCodeName + "</a></li>");
                                                        }
                                                    }
                                                    html.AppendLine("</ul></li>");
                                                    html.AppendLine("<!--[if lte IE 9]></ul></li><![endif]-->");
                                                }
                                            }
                                        }
                                    }
                                    html.AppendLine("</ul>");
                                }
                                html.AppendLine("</li>");
                                break;
                            }

                        case "6":
                            {
                                html.AppendLine("<li>");
                                html.AppendLine("<a href=\"javascript:void(0);\" class=\"topLevelLink\">" +
                                                menuSetting[2] + newSash +
                                                "<i class=\"fa fa-chevron-down pull-right g-p-r-10\" aria-hidden=\"true\"></i></a>");

                                html.AppendLine("<div class=\"g-d-n\">");
                                html.AppendLine("<div class=\"g-va-t text-left col-md-12 g-p-l-0\">" +
                                                EntityFunctions.GetNgmdCMSEntry(websiteId, "MenuData",
                                                    cs.settingName + "text") + "</div>");

                                html.AppendLine("</div>");
                                html.AppendLine("</li>");
                                break;
                            }
                    }
                }

                html.AppendLine("</ul>");

                // Mobile Links
                html.Append(EntityFunctions.GetNgmdCMSEntry(websiteId, "MenuData", "MobileAddOn"));
            }
            catch (Exception ex)
            {
                errorHasOccurred = true;
                errorMessage = ex.Message;
            }

            //Write the html file
            if (errorHasOccurred == false)
            {
                try
                {
                    string filename = "main-menu.html";
                    string cacheItem = "Menu";
                    if (forMobile)
                    {
                        filename = "mobile-menu.html";
                        cacheItem = "Mobile";
                    }
                    writeHTMLFile(html.ToString() + html2.ToString(), parms["output"] + filename);

                    DataCache cache = new DataCache(websiteId);
                    cache.ClearCache(cacheItem);
                }
                catch (Exception ex)
                {
                    errorHasOccurred = true;
                    errorMessage = ex.Message;
                }
            }

            //Log in activity log
            if (errorHasOccurred)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Occurred: " + errorMessage, ErrorCode = "ERROR" });
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private static bool ValidCategory(categoryCode sc)
        {
            bool valid = false;

            if (sc.websiteInventory.Any(x => x.product.productStatusFK == 1 || x.product.productStatusFK == 8) ||
                sc.secondaryCategoryLookup.Any(x => x.websiteInventory.product.productStatusFK == 1 || x.websiteInventory.product.productStatusFK == 8))
            {
                foreach (var wi in sc.websiteInventory)
                {
                    if (wi.productPrice.FirstOrDefault() != null || HasTGPrice(wi.product.productID))
                    {
                        valid = true;
                        break;
                    }
                }

                if (!valid)
                {
                    foreach (var wi in sc.secondaryCategoryLookup)
                    {
                        if (wi.websiteInventory.productPrice.FirstOrDefault() != null ||
                            HasTGPrice(wi.websiteInventory.product.productID))
                        {
                            valid = true;
                            break;
                        }
                    }
                }
            }

            return valid;
        }

        private static bool HasTGPrice(int productID)
        {
            bool bTGPrice = false;

            websiteInventory wiTG = EntityFunctions.GetWebsiteInventoryList(x => x.websiteFK == 1 && x.productFK == productID).FirstOrDefault();

            if (wiTG != null)
            {
                if (wiTG.productPrice != null)
                    bTGPrice = true;
            }

            return bTGPrice;
        }

        public static bool CategoryHasProducts(categoryCode cc)
        {
            bool returnValue = false;
            List<categoryCode> ccBottom = EntityFunctions.GetCategoryCodeList(x => x.parentCategoryCodeID == cc.categoryCodeID);
            foreach (categoryCode bot in ccBottom)
            {
                var cnt1 = bot.websiteInventory.Count(x => x.product.productStatusFK == 1 || x.product.productStatusFK == 8);
                if (cnt1 > 0)
                {
                    returnValue = true;
                }
                var cnt2 = bot.secondaryCategoryLookup.Count(x => x.websiteInventory.product.productStatusFK == 1 || x.websiteInventory.product.productStatusFK == 8);
                if (cnt2 > 0)
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        private static void CreateFooterStd(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;
            string errorMessage = "";
            int websiteId = Convert.ToInt32(parms["websiteid"]);
            string siteRoot = "";
            string siteName = "";
            string html = "";
            string addressPrefix = "";
            string chatText = "";
            string blogText = "";
            //string bestSellersText = "";

            //Initialise Config Settings
            EntityFunctions.SetSiteConfigSettings(websiteId);

            if (EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "UseHTTPS").FirstOrDefault()
                    .settingValue == "True")
            {
                addressPrefix = "https://";
            }
            else
            {
                addressPrefix = "http://";
            }

            try
            {
                siteRoot = addressPrefix + EntityFunctions.WebsiteConfigSettings
                                .Where(x => x.settingName == "siteRoot").FirstOrDefault().settingValue;
                siteName = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "siteName")
                    .FirstOrDefault().settingValue;

                switch (websiteId)
                {
                    case 1:
                        chatText = "LIVE CHAT";
                        blogText = "NEWS BLOG";
                        //bestSellersText = "Top Sellers";
                        break;
                    case 2:
                        chatText = "LIVE HELP";
                        blogText = "BLOG";
                        //bestSellersText = "Best Sellers";
                        break;
                    case 3:
                        chatText = "LIVE HELP";
                        blogText = "NEWS BLOG";
                        //bestSellersText = "Top Selling Products";
                        break;
                }

                //Get the Menu Control Settings for this website
                List<configurationSetting> menus;
                menus = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName.StartsWith("menu"))
                    .OrderBy(y => y.settingName).ToList();
                string siteCode = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "site")
                    .FirstOrDefault().settingValue;
                string supportEmailAddress = EntityFunctions.WebsiteConfigSettings
                    .Where(x => x.settingName == "supportEmailAddress").FirstOrDefault().settingValue;

                //Build the html file
                html = "</div>";
                html += "<div id=\"ft_top\" class=\"g-c-b\"></div>" + Environment.NewLine;
                html += "<div id=\"footerSlideButton\" class=\"ft_barButtonContainer gm-d-n ft_fix\">" +
                        Environment.NewLine;
                html += "<div class=\"ft_barButtonCenter g-b-3-w g-ps-f\">" + Environment.NewLine;
                html += "<div class=\"g-f-l ft_liveHelpImage\">" + Environment.NewLine;
                html += "<img src=\"" + siteRoot +
                        "images/1pxTrans.png\" alt=\"Help\" class=\"msp_openHelpBar ft_barButton g-cur-p\">" +
                        Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "<div class=\"g-f-l ft_liveHelpActions g-bc-1\">" + Environment.NewLine;
                html += "<div class=\"ft_liveHelpButton g-m-a\">" + Environment.NewLine;
                html += "<div id=\"lhnContainer1\" class=\"g-f-l ft_chat\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 12, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "<div class=\"ft_liveHelpButton g-m-a\">" + Environment.NewLine;
                html += "<a class=\"g-ba\" href=\"" + siteRoot +
                        "help.asp\"><div class=\"g-button-A g-bs-5\">FAQ</div></a>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "<div class=\"ft_liveHelpButton g-m-a\">" + Environment.NewLine;
                html += "<a class=\"g-ba\" href=\"mailto:" + supportEmailAddress +
                        "\"><div class=\"g-button-A g-bs-5\">Send Email</div></a>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "<div class=\"ft_liveHelpText g-a-c\">" + Environment.NewLine;
                html += "<span class=\"g-fc-2 g-fs-12\">Call: </span><span class=\"g-fc-5 g-fs-11\">" +
                        EntityFunctions.GetCMSEntry(websiteId, 2, 1, "T") + "</span>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                //Display FeeFo Info
                html += feefoXML(websiteId);

                //Help Bar
                html += "<div id=\"ft_footerStandard1\" class=\"ft_footerStandard1 gm-d-n g-bc-5 g-fc-1 g-w-a\">" +
                        Environment.NewLine;
                html += "<div class=\"ft_center\">" + Environment.NewLine;
                html += "<div class=\"g-f-l ft_text1\">" + EntityFunctions.GetCMSEntry(websiteId, 41, 4, "T") +
                        "</div>" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 5, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                //Info Links
                html += "<div id=\"ft_footerStandard2\" class=\"ft_footerStandard2 gm-d-n g-bc-1 g-fc-2 g-w-a\">" +
                        Environment.NewLine;
                html += "<div class=\"ft_center\">" + Environment.NewLine;
                html += "<div class=\"g-f-l g-m-t-20\">" + Environment.NewLine;

                //Popular Brands
                //html += "<div class=\"g-f-l ft_linkSectionFirst g-w-a\">" + Environment.NewLine;
                //html += StandardFunctions.GetCMSEntry(websiteId, 41, 1, "T") + Environment.NewLine;
                //html += " </div>" + Environment.NewLine;
                //Top Sellers
                //html += "<div class=\"g-f-l ft_linkSectionBS g-of-h g-w-a\">" + Environment.NewLine;
                //html += "<div class=\"g-fc-5 g-fs-08 g-fw-b\">" + bestSellersText + "</div>" + Environment.NewLine;
                //html += "<ul class=\"g-fs-08\">" + Environment.NewLine;
                //html += bestSellers(websiteId, 10);
                //html += "</ul>" + Environment.NewLine;
                //html += "</div>" + Environment.NewLine;

                //Help & Advice
                html += "<div class=\"g-f-l ft_linkSection g-of-h g-w-a\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 2, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                //Quick Links
                html += "<div class=\"g-f-l ft_linkSection g-of-h g-w-a\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 3, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                //Help & Advice 3
                html += "<div class=\"g-f-l ft_linkSection g-of-h g-w-a\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 15, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                //Help & Advice 4
                html += "<div class=\"g-f-l ft_linkSection g-of-h g-w-a\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 16, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                //Social & Trust
                html += "<div class=\"g-f-r ft_socialAndTrust\">" + Environment.NewLine;
                //The following entry contains https:// references. The http:// version is in 41, 7
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 8, "T") + Environment.NewLine;
                html += "<div class=\"ft_buttonContainer g-bsz-bb\">" + Environment.NewLine;
                html += "<div id=\"ft_blog\" class=\"g-button-B g-footerButton\">" + Environment.NewLine;
                html += "<a class=\"g-ba\" href=\"" + siteRoot + "blog/\">" + blogText + "</a>" +
                        Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html +=
                    "<div class=\"g-button-B g-footerButton\" onclick=\"$('#liveagent_button_online_1').trigger('click');\">" +
                    chatText + "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "<div class=\"g-f-l ft_spacer3 g-m-t-20\"></div>" + Environment.NewLine;
                //Company Address
                html += "<div class=\"g-f-l ft_spacer6 g-fs-08\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 6, "T") + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                //Payment Types
                html += "<div class=\"g-f-r ft_paymentTypes\">" + Environment.NewLine;
                html += "<img class=\"msp_sagepay\" alt=\"SagePay\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-f-r msp_payPal\" alt=\"PayPal\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-f-r msp_visa\" alt=\"Visa\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-f-r msp_mastercard\" alt=\"Master Card\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-c-b g-f-r msp_maestro\" alt=\"Maestro\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-f-r msp_solo\" alt=\"Solo\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"g-f-r msp_americanExpress\" alt=\"American Express\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                html += "<div id=\"ft_goToTop\" class=\"ft_goToTop g-ps-f g-cur-p g-d-n\">" + Environment.NewLine;
                html += "<img class=\"msp_goToTop\" alt=\"Go to top of page\" title=\"Go to top of page\" src=\"" +
                        siteRoot + "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                //Mobile Footer
                html += "<div class=\"mob_footerContainer\">" + Environment.NewLine;
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 9, "T") + Environment.NewLine;
                html += "<div class=\"mob_unorderedListClear\"></div>" + Environment.NewLine;
                html += "<div class=\"mob_footerRuleLine\"></div>" + Environment.NewLine;
                html += "<div class=\"mob_footerDivContactUs\">Contact us now on</div>" + Environment.NewLine;
                //The following entry contains https:// references. The http:// version is in 41, 10
                html += EntityFunctions.GetCMSEntry(websiteId, 41, 11, "T") + Environment.NewLine;
                html += "<div class=\"mob_footerRuleLine\"></div>" + Environment.NewLine;
                html += "<div class=\"mob_footerIcons\">" + Environment.NewLine;
                html += "<img class=\"msp_sagepay g-m-b-20\" alt=\"SagePay\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<div class=\"g-m-tb-10 mob_footerTrust\">" + Environment.NewLine;
                html += "<img class=\"msp_payPal\" alt=\"PayPal\" src=\"" + siteRoot + "images/1pxTrans.png\" />" +
                        Environment.NewLine;
                html += "<img class=\"msp_visa\" alt=\"Visa\" src=\"" + siteRoot + "images/1pxTrans.png\" />" +
                        Environment.NewLine;
                html += "<img class=\"msp_mastercard\" alt=\"Master Card\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"msp_maestro\" alt=\"Maestro\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "<img class=\"msp_solo\" alt=\"Solo\" src=\"" + siteRoot + "images/1pxTrans.png\" />" +
                        Environment.NewLine;
                html += "<img class=\"msp_americanExpress\" alt=\"American Express\" src=\"" + siteRoot +
                        "images/1pxTrans.png\" />" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                html += "<div class=\"g-c-b mob_footerCopyright\">" + Environment.NewLine;
                html += "Copyright &copy; <%=year(date)%><br />" + siteName +
                        " is a trading brand of NetGiant Ltd,<br />61 Gibfield Park Avenue, Gibfield Park, Atherton, Manchester, M46 0SY, UK" +
                        Environment.NewLine;
                html += "</div>" + Environment.NewLine;
                html += "</div>" + Environment.NewLine;

                //Other Stuff
                html +=
                    "<div id=\"ml_launchForm\" class=\"g-d-n pop_standardPopup\" data-popupname=\"general&amp;series=26&amp;id=13\"></div>" +
                    Environment.NewLine;
            }
            catch (Exception ex)
            {
                errorHasOccurred = true;
                errorMessage = ex.Message;
            }

            //Write the html file
            if (errorHasOccurred == false)
            {
                try
                {
                    writeHTMLFile(html, parms["output"]);
                }
                catch (Exception ex)
                {
                    errorHasOccurred = true;
                    errorMessage = ex.Message;
                }
            }

            //Log in activity log
            if (errorHasOccurred)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Occurred: " + errorMessage, ErrorCode = "ERROR" });
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private static string feefoXML(int websiteId)
        {
            string html = "";
            string addressPrefix = "";

            if (EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "UseHTTPS").FirstOrDefault()
                    .settingValue == "True")
            {
                addressPrefix = "https://";
            }
            else
            {
                addressPrefix = "http://";
            }

            string feefoSiteName = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "feefoSiteName")
                .FirstOrDefault().settingValue;
            string siteName = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "siteName")
                .FirstOrDefault().settingValue;
            string testSite = EntityFunctions.WebsiteConfigSettings.Where(x => x.settingName == "testSite")
                .FirstOrDefault().settingValue;
            string siteRoot = addressPrefix + EntityFunctions.WebsiteConfigSettings
                                  .Where(x => x.settingName == "siteRoot").FirstOrDefault().settingValue;
            int totalAmountOfReviewsCount = 0;
            double averagePercentage = 0;
            double feefoReviewRating = 0;
            string feefoURL1 = "";
            string feefoURL2 = "";

            //if (testSite == "True")
            //{
            //    feefoURL1 = "http://uat.feefo.com/api/xmlfeedback?merchantidentifier=" + feefoSiteName + "&limit=0";
            //    feefoURL2 = "http://uat.feefo.com/en-gb/reviews/" + feefoSiteName + "#?timeFrame=ALL&sort=newest";
            //}
            //else
            //{
            feefoURL1 = "http://cdn2.feefo.com/api/xmlfeedback?merchantidentifier=" + feefoSiteName + "&limit=0";
            feefoURL2 = "http://ww2.feefo.com/en-gb/reviews/" + feefoSiteName + "#?timeFrame=ALL&sort=newest";
            //}

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(feefoURL1);

            int.TryParse(xmlDoc.DocumentElement.SelectSingleNode("SUMMARY/COUNT").InnerText,
                out totalAmountOfReviewsCount);
            double.TryParse(xmlDoc.DocumentElement.SelectSingleNode("SUMMARY/AVERAGE").InnerText,
                out averagePercentage);
            feefoReviewRating = Math.Round(((averagePercentage / 20) + 0.05), 1);

            html =
                "<div id=\"ft_feefoBar\" class=\"ft_feefoBar g-bc-2 g-fc-1 g-w-a g-m-t-60 gm-d-n\" itemtype=\"http://data-vocabulary.org/Review-aggregate\" itemscope=\"\">" +
                Environment.NewLine;
            html += "<div class=\"g-d-n\"><span itemprop=\"itemreviewed\">" + siteName + "</span></div>" +
                    Environment.NewLine;
            html += "<div class=\"ft_center\">" + Environment.NewLine;
            html += "<div class=\"ft_feefoLogo g-f-l g-a-l\">" + Environment.NewLine;
            html += "<a href=\"" + siteRoot + siteName.ToLower() + "-reviews/\" onclick=\"javascript:window.open('" +
                    feefoURL2 +
                    "','feefo','width=1100,height=600,scrollbars=yes,resizable=no,toolbar=no,menubar=no,location=no');return false;\"><img src=\"" +
                    siteRoot + "images/1pxTrans.png\" class=\"msp_feefoMainLogo\" alt=\"Feefo\"  /></a>" +
                    Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "<div class=\"ft_feefoText g-f-l g-m-l-25\">" + Environment.NewLine;
            html += EntityFunctions.GetCMSEntry(websiteId, 41, 14, "T") + Environment.NewLine;
            html += "<div class=\"ft_feefoCircle g-bc-13 g-f-r\">" + Environment.NewLine;
            html += "<a href=\"" + siteRoot + siteName.ToLower() + "-reviews/\" onclick=\"javascript:window.open('" +
                    feefoURL2 +
                    "','feefo','width=1100,height=600,scrollbars=yes,resizable=no,toolbar=no,menubar=no,location=no');return false;\"><div class=\"ft_reviewPercentageContainer g-a-c g-fc-2 g-bsz-bb\">" +
                    averagePercentage + "%</div></a>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "<div class=\"ft_feefoData g-f-l g-m-t-10 g-m-l-20\">" + Environment.NewLine;
            html += "<div class=\"ft_feefoStarsContainer g-b-r-5 g-b-1-w g-bsz-bb g-a-l g-p-l-10\">" +
                    Environment.NewLine;
            html += "<div class=\"ft_feefoStars g-f-l g-bsz-bb\">" + Environment.NewLine;
            html += "<a href=\"" + siteRoot + siteName.ToLower() + "-reviews/\" onclick=\"javascript:window.open('" +
                    feefoURL2 +
                    "','feefo','width=1100,height=600,scrollbars=yes,resizable=no,toolbar=no,menubar=no,location=no');return false;\"><img src=\"" +
                    siteRoot + "images/1pxTrans.png\" class=\"msp_feefoStars\" alt=\"Feefo Rating\"  /></a>" +
                    Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "<div itemtype=\"http://data-vocabulary.org/rating\" itemscope=\"\" itemprop=\"rating\">" +
                    Environment.NewLine;
            html += "<div class=\"ft_reviewRating g-f-l g-bsz-bb g-m-l-25\"><span itemprop=\"average\">" +
                    feefoReviewRating + "</span> / <span itemprop=\"best\">5</span></div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "<div class=\"ft_reviewTotalCount g-f-r g-bsz-bb g-p-r-5\">(<span itemprop=\"votes\">" +
                    totalAmountOfReviewsCount + "</span> Reviews)</div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "</div>" + Environment.NewLine;
            html += "<div id=\"ft_goToFeefo\" class=\"g-baseline gm-d-n\">" + Environment.NewLine;
            html += "<div id=\"ft_gcs\"></div>" + Environment.NewLine;
            html += "<div class=\"g-ps-r\"><a href=\"" + siteRoot + siteName.ToLower() +
                    "-reviews/\" onclick=\"javascript:window.open('" + feefoURL2 +
                    "','feefo','width=1100,height=600,scrollbars=yes,resizable=no,toolbar=no,menubar=no,location=no');return false;\"><img src=\"/images/1pxTrans.png\" class=\"msp_feefoTab\"  title=\"Click for Reviews\" alt=\"Click for Reviews\" /></a></div>" +
                    Environment.NewLine;
            html += "<div class=\"ft_averagePercentage g-fc-1 g-ps-a g-a-c\">" + averagePercentage + "%</div>" +
                    Environment.NewLine;
            html += "</div>" + Environment.NewLine;

            return html;
        }

        private static void CreateScriptFiles(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;
            string errorMessage = "";
            string websiteAbbrev = "";
            int websiteId = Convert.ToInt32(parms["websiteid"]);
            string awinId = "";
            string sliDomain = EntityFunctions.GetConfigurationSetting("Website Application Variables", "SLIDomain", websiteId);
            Dictionary<string, string> fileList = new Dictionary<string, string>();

            switch (websiteId)
            {
                case 1:
                {
                    awinId = "5500";
                    websiteAbbrev = "tg";
                    fileList.Add("cpy_freshrelevance.js", "https://d81mfvml8p5ml.cloudfront.net/qgorm22z.js");
                    break;
                }
                case 2:
                {
                    awinId = "808";
                    websiteAbbrev = "cm";
                    break;
                }
                case 3:
                {
                    awinId = "6704";
                    websiteAbbrev = "ng";
                    break;
                }
            }
            fileList.Add("cpy_affilliatewindow.js", "https://www.dwin1.com/" + awinId + ".js");
            fileList.Add("cpy_googlereviewsbadge.js", "https://apis.google.com/js/platform.js");
            fileList.Add("cpy_sfliveagent.js", "https://c.la1-c1-par.salesforceliveagent.com/content/g/js/45.0/deployment.js");
            fileList.Add("cpy_sliracsearch.js", "https://" + sliDomain + "/autocomplete/rac-resources-" + websiteAbbrev + "/sli-rac.config.js");
            fileList.Add("cpy_slispark.js", "https://" + sliDomain + "/js/sli-spark.js");

            foreach (KeyValuePair<string, string> kvp in fileList)
            {
                string oName = parms["output"] + "\\" + kvp.Key;
                string text = "";
                try
                {
                    ServicePointManager.Expect100Continue = true;
                    StandardFunctions.SetTlsVersion();

                    // Backup current
                    using (WebClient client = new WebClient())
                    {
                        text = client.DownloadString(kvp.Value);
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        writeHTMLFile(text, oName);
                    }
                }
                catch (Exception ex)
                {
                    errorHasOccurred = true;
                    errorMessage = ex.Message;
                    break;
                }
            }

            //Log in activity log
            if (errorHasOccurred)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Occurred: " + errorMessage, ErrorCode = "ERROR" });
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private static void writeHTMLFile(string text, string outputFile)
        {
            string[] fileParts = outputFile.Split('\\');
            string fileName = fileParts[fileParts.Length - 1];
            string fileNamePath = "";
            for (int i = 0; i < fileParts.Length - 1; i++)
            {
                fileNamePath += fileParts[i] + "\\";
            }
            string[] fileNameParts = fileName.Split('.');
            string fileNameTemp = fileNamePath + fileNameParts[0] + "_temp." + fileNameParts[1];
            string fileNameArchive = fileNamePath + fileNameParts[0] + "_old." + fileNameParts[1];

            StreamWriter sw = new StreamWriter(fileNameTemp);
            try
            {
                //Write menu file
                sw.Write(text);
                sw.Close();

                //Backup existing menu file
                File.Copy(outputFile, fileNameArchive, true);

                //Overwrite menu file
                File.Copy(fileNameTemp, outputFile, true);

                //Delete the Temp file
                File.Delete(fileNameTemp);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error writing file: " + outputFile + ". Error Message: " + ex.Message + ex.StackTrace);
            }
        }
    }
}