using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Xml.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Xml;
using NGBP.DataAccessLayer.DataUtilities;
using System.Data.SqlClient;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SiteMaps
    {
        private static XNamespace sm = "http://www.sitemaps.org/schemas/sitemap/0.9";
        public static string siteURL { get; set; }
        public static bool useSSL { get; set; }
        public static string filePath { get; set; }

        public static void CreateSiteMaps(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;

            string websiteName = parms["subtype"].ToLower();
            Website ws = EntityFunctions.GetWebsiteList(x => x.WebsiteName == websiteName).FirstOrDefault();
            string useHTTPS = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "Website Application Variables" && x.settingName == "UseHTTPS" && x.websiteFK == ws.WebsiteID)
                                .FirstOrDefault().settingValue;
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

            DataSet ds = new DataSet("sitemaps");
            DataTable dt = new DataTable();

            try
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully extracted data from SQL" });
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@websiteID", SqlDbType.Int);
                sqlParm.Value = ws.WebsiteID;
                sqlParms.Add(sqlParm);

                ds = SQLUtilities.ExecuteReadStoredProcedure("netgiantBatchProcesses", "ngmd.GetSiteMapsData", sqlParms, "SiteMapData", 3000);

                dt = ds.Tables[0];
                WriteEquipmentMap(dt, parms["filepath"] + parms["equipfile"]);
                dt = ds.Tables[1];
                WriteProductMap(dt, parms["filepath"] + parms["prodfile"]);
                dt = ds.Tables[2];
                WriteCatalogueMap(dt, parms["filepath"] + parms["catfile"]);
                WriteSiteMap(parms["websitepath"], parms["filepath"], "sitemap.xml", parms["prodfile"], parms["catfile"], parms["equipfile"], parms["blogfile"]);
                //WriteBlogMap(parms["filepath"] + parms["blogfile"]);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR with Sitemap generation. " + "Error Message: " + ex.Message, ErrorCode = "ERROR" });
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
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
                        if (!family.Contains("-Toner-Range") && !family.Contains("-Ink-Range"))
                        {
                            XElement urlF = new XElement(sm + "url");
                            urlF.Add(new XElement(sm + "loc", siteURL + dr["CartridgeType"] + "/" + dr["Manufacturer"] + "/" + dr["Family"] + "/"));
                            urlF.Add(new XElement(sm + "changefreq", "weekly"));
                            urlF.Add(new XElement(sm + "priority", "1.0"));

                            urlset.Add(urlF);
                        }
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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR With sitemap XML processing - " + fileName, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR With sitemap XML processing - " + fileName, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        public static void WriteCatalogueMap(DataTable dt, string fileName)
        {
            XDocument xmlDoc = new XDocument(new XDeclaration("1.0", "UTF-8", ""));

            string directory = StandardFunctions.GetMachineConfigAppSetting("LocalDirectory");

            try
            {
                XElement urlset = new XElement(sm + "urlset", "");
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["Sitemap"].ToString() == "True")
                    {
                        XElement url = new XElement(sm + "url");
                        var page = "products";
                        if (dr["HasChildren"].ToString() == "True")
                            page = "catalogue";
                        url.Add(new XElement(sm + "loc", siteURL + page + "/" + dr["Category"] + "-" + dr["GroupNo"].ToString().Trim() + "/"));
                        url.Add(new XElement(sm + "changefreq", "weekly"));
                        url.Add(new XElement(sm + "priority", "1.0"));

                        if (dr["HasChildren"].ToString() == "True"
                            || (dr["HasChildren"].ToString() == "False" && Int32.Parse(dr["Kount"].ToString()) >= 0))
                        {
                             urlset.Add(url);
                        }
                    }
                }
                xmlDoc.Add(urlset);
                xmlDoc.Save(fileName);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR With sitemap XML processing - " + fileName, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
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
