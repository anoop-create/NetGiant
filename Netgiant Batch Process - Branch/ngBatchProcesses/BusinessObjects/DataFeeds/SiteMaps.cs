using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using NGBP.DataAccessLayer.SCOM.Services;
using System.Data;
using System.Xml.Linq;
using netGiant.Intranet.DataLayer;
using System.Xml;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SiteMaps
    {
        private static XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        public static string siteURL { get; set; }
        public static string webURL { get; set; }
        public static int websiteID { get; set; }
        public static bool useSSL { get; set; }
        public static string filePath { get; set; }
        public static StandardFunctions stnFunc { get; set; } = new StandardFunctions();

        public static void CreateSiteMaps(Dictionary<string, string> parms)
        {
            stnFunc.AddToActivityLog(parms["type"] + " Process Started"); 
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            using (ngmdEntities db = new ngmdEntities())
            {
                string websiteName = parms["subtype"].ToLower();
                Website ws = db.Websites.Where(x => x.WebsiteName == websiteName).FirstOrDefault();
                websiteID = ws.WebsiteID;
                webURL = ws.WebURL;
                string useHTTPS = db.configurationSetting.Where(x => x.sectionName == "Website Application Variables" && x.settingName == "UseHTTPS" && x.websiteFK == websiteID).FirstOrDefault().settingValue;
                if (useHTTPS == "True")
                {
                    siteURL = "https://" + ws.WebURL + "/";
                    useSSL = true;
                }
                else 
                {
                    siteURL = "http://" + ws.WebURL + "/";
                    useSSL = false;
                }
            }

            DataSet ds = new DataSet("sitemaps");
            DataTable dt = new DataTable();

            try
            {
                SiteMapsServices sitemap = new SiteMapsServices();
                stnFunc.AddToActivityLog("Successfully extracted data from SQL" + Environment.NewLine);
                ds = sitemap.GetSiteMapsData(websiteID);

                dt = ds.Tables[0];
                WriteEquipmentMap(dt, parms["filepath"] + parms["equipfile"]);
                dt = ds.Tables[1];
                WriteProductMap(dt, parms["filepath"] + parms["prodfile"]);
                dt = ds.Tables[2];
                WriteCatalogueMap(dt, parms["filepath"] + parms["catfile"], stnFunc);
                WriteSiteMap(parms["websitepath"], parms["filepath"], "sitemap.xml", parms["prodfile"], parms["catfile"], parms["equipfile"], parms["blogfile"]);
                //WriteBlogMap(parms["filepath"] + parms["blogfile"]);
            }
            catch (Exception ex)
            {
                errorHasOccurred = true;
                stnFunc.AddToActivityLog("**ERROR** with Sitemap generation. " +
                    "Error Message: " + ex.Message);
            }

            //Log in activity log

            stnFunc.AddToActivityLog(parms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }
            stnFunc = null;
        }

        public static void WriteEquipmentMap(DataTable dt, string fileName)
        {
            XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", ""));
            string manufacturer = string.Empty;
            string family = string.Empty;

            try
            {
                XElement urlset = new XElement(sm + "urlset", "");
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["Manufacturer"].ToString() != manufacturer)
                    {
                        manufacturer = dr["Manufacturer"].ToString();
                        XElement urlM = new XElement(sm + "url");
                        urlM.Add(new XElement(sm + "loc", siteURL + dr["CartridgeType"] + "/" + dr["Manufacturer"] + "/"));
                        urlM.Add(new XElement(sm + "changefreq", "weekly"));
                        urlM.Add(new XElement(sm + "priority", "1.0"));

                        urlset.Add(urlM);
                    }
                    if (dr["Family"].ToString() != family)
                    {
                        family = dr["Family"].ToString();
                        XElement urlF = new XElement(sm + "url");
                        urlF.Add(new XElement(sm + "loc", siteURL + dr["CartridgeType"] + "/" + dr["Manufacturer"] + "/" + dr["Family"] + "/"));
                        urlF.Add(new XElement(sm + "changefreq", "weekly"));
                        urlF.Add(new XElement(sm + "priority", "1.0"));

                        urlset.Add(urlF);
                    }
                    XElement url = new XElement(sm + "url");
                    //url.Add(new XElement(sm + "loc", siteURL + dr["CartridgeType"].ToString() + "/" + dr["Manufacturer"].ToString() + "/" + dr["Equipment"].ToString() + "/"));
                    url.Add(new XElement(sm + "loc", siteURL + "model/" + dr["Equipment"] + "-" + dr["CartridgeType"] + "/"));
                    url.Add(new XElement(sm + "changefreq", "weekly"));
                    url.Add(new XElement(sm + "priority", "1.0"));

                    urlset.Add(url);
                }
                xmlDoc.Add(urlset);
                xmlDoc.Save(fileName);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** With sitemap XML processing - " + fileName);
                stnFunc.ProcessException(ex);
            }
        }

        public static void WriteProductMap(DataTable dt, string fileName)
        {
            XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", ""));

            try
            {
                XElement urlset = new XElement(sm + "urlset", "");
                foreach (DataRow dr in dt.Rows)
                {
                    XElement url = new XElement(sm + "url");
                    url.Add(new XElement(sm + "loc", siteURL + dr["ProductURL"]));
                    url.Add(new XElement(sm + "changefreq", "weekly"));
                    url.Add(new XElement(sm + "priority", "1.0"));

                    urlset.Add(url);
                }
                xmlDoc.Add(urlset);
                xmlDoc.Save(fileName);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** With sitemap XML processing - " + fileName);
                stnFunc.ProcessException(ex);
            }
        }

        public static void WriteCatalogueMap(DataTable dt, string fileName, StandardFunctions stnFunc)
        {
            XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", ""));

            string filePath = "";
            string directory = StandardFunctions.GetMachineConfigAppSetting("LocalDirectory");

            try
            {
                XElement urlset = new XElement(sm + "urlset", "");
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["Sitemap"].ToString() == "True")
                    {
                        XElement url = new XElement(sm + "url");
                        filePath = String.Format(@"{0}IIS-Content\{1}\data\ProductGroups\Current\{2}.xml", directory, webURL, dr["GroupNo"].ToString().Trim());
                        var page = "products";
                        if (dr["HasChildren"].ToString() == "True")
                            page = "catalogue";
                        url.Add(new XElement(sm + "loc", siteURL + page + "/" + dr["Category"] + "-" + dr["GroupNo"].ToString().Trim() + "/"));
                        url.Add(new XElement(sm + "changefreq", "weekly"));
                        url.Add(new XElement(sm + "priority", "1.0"));

                        if (StandardFunctions.checkFileExists(filePath))
                        {
                            if (dr["HasChildren"].ToString() == "False" && Int32.Parse(dr["Kount"].ToString()) >= 0)
                                urlset.Add(url);
                        }

                        if (dr["HasChildren"].ToString() == "True")
                            urlset.Add(url);
                    }
                }
                xmlDoc.Add(urlset);
                xmlDoc.Save(fileName);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** With sitemap XML processing - " + fileName);
                stnFunc.ProcessException(ex);
            }
        }

        public static void WriteSiteMap(string websitePath, string pathName, string mapFile, string prodFile, string catFile, string equipFile, string blogFile)
        {
            XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", ""));

            try
            {
                XElement sitemapindex = new XElement(sm + "sitemapindex", "");

                XElement sitemap1 = new XElement(sm + "sitemap");
                sitemap1.Add(new XElement(sm + "loc", siteURL + websitePath + prodFile));
                sitemapindex.Add(sitemap1);
                XElement sitemap2 = new XElement(sm + "sitemap");
                sitemap2.Add(new XElement(sm + "loc", siteURL + websitePath + catFile));
                sitemapindex.Add(sitemap2);
                XElement sitemap3 = new XElement(sm + "sitemap");
                sitemap3.Add(new XElement(sm + "loc", siteURL + websitePath + equipFile));
                sitemapindex.Add(sitemap3);
                XElement sitemap4 = new XElement(sm + "sitemap");
                sitemap4.Add(new XElement(sm + "loc", siteURL + websitePath + "other.xml"));
                sitemapindex.Add(sitemap4);
                XElement sitemap5 = new XElement(sm + "sitemap");
                sitemap5.Add(new XElement(sm + "loc", siteURL + websitePath + blogFile));
                sitemapindex.Add(sitemap5);

                xmlDoc.Add(sitemapindex);
                xmlDoc.Save(pathName + mapFile);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error with sitemap XML processing. Error Message: " + ex.Message);
            }
        }

        public static void WriteBlogMap(string fileName)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                //Make sure we load the live version of the sitemap xml file to avoid security issues on the beta
                string loadURL = siteURL.Replace("//beta.", "//www.").Replace("https://", "http://");
                string linkURL = siteURL.Replace("//beta.", "//www.");
                xmlDoc.Load(loadURL + "blog/sitemap.xml");
                string xmlText = xmlDoc.InnerXml;

                //Make sure all links in the sitemap use the correct protocol and point at the www domain
                string blogURL = loadURL.Replace("//www.", "//blog.");
                xmlText = xmlText.Replace(blogURL, linkURL + "blog/");

                XmlDocument xmlDoc2 = new XmlDocument();
                xmlDoc2.LoadXml(xmlText);
                xmlDoc2.Save(fileName);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error with blog sitemap. Error Message: " + ex.Message);
            }

        }
    }
}
