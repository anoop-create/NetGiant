using System;
using System.Collections.Generic;
using DataAccess.Utilities;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.Configuration;
using System.Web;
using System.Net;

namespace BusinessLogic.ViewModels
{
    public class HomeViewModel : WizardViewModel
    {
        public HomeViewModel()
        {
            HomeData = DataCache.GetSectionData("HomeData");
        }

        public Dictionary<string, string> HomeData { get; set; }
        public DataTable FeeFo { get; set; }

        public XmlNodeList BlogFeed { get; set; }

        /// <summary>
        /// Retrieve  the Blog feed summary component 
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public void GetBlogFeed(string url)
        {            
            string cacheKey = "BlogFeed";
            BlogFeed = DataCache.GetCache<XmlNodeList>(cacheKey);
            if (BlogFeed == null)
            {
                try
                {
                    Utilities.SetTlsVersion();
                    XmlDocument xmldoc = new XmlDocument();
                    xmldoc.Load(url);
                    BlogFeed = xmldoc.DocumentElement.SelectNodes("descendant::item");
                    DataCache.PutCache(cacheKey, BlogFeed);
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
            }
        }

        public HomeViewModel GetReviews()
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);

            FeeFo = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductFeedback", sqlParms, "feedback").Tables[0];

            return this;
        }

        public new void GetMeta()
        {
            var action = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString().ToLower();
            switch (action)
            {
                case "customerreviews":
                    GetMeta("Customer Reviews" + " | " + Utilities.GetItemFromDict(CommonData, "ShortSiteName"), "Customer Reviews");
                    break;
                default:
                    break;
            }
        }
    }
}
