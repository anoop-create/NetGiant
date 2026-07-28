using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace netGiant.Intranet.BusinessLayer
{
    public class DataCache
    {
        public static T GetCache<T>(string cacheKey) where T : class
        {
            T item = HttpContext.Current.Cache[cacheKey] as T;
            return item;
        }

        public static bool PutCache(string cacheKey, object item, int period = 0)
        {
            if (period == 0)
            {
                period = int.Parse(ConfigurationManager.AppSettings["CacheTime_Long"].ToString());
            }
            HttpContext.Current.Cache.Insert(cacheKey, item, null,
                DateTime.Now.AddHours(period),
                TimeSpan.Zero);
            return true;
        }

        public static bool DeleteCache(string cacheKey)
        {
            if (cacheKey == null)
            {
                foreach (DictionaryEntry c in HttpContext.Current.Cache)
                {
                    HttpContext.Current.Cache.Remove(c.Key.ToString());
                }
            }
            else
            {
                HttpContext.Current.Cache.Remove(cacheKey);
            }
            return true;
        }

        public static List<actionLink> GetMainMenuItems(bool bypassCache = false)
        {
            var cacheKey = "MainMenuItems";
            var cacheItem = (List<actionLink>)HttpContext.Current.Cache[cacheKey];

            if (bypassCache || cacheItem == null)
            {
                var list = EntityAccess.GetActionLinks(w => w.actionLinkLevel == 1 && w.active == true);              

                if (list != null)
                {
                    cacheItem = list;
                    PutCache(cacheKey, cacheItem);
                }
            }

            return cacheItem;
        }

        public static List<actionLink> GetSideMenuItems(bool bypassCache = false)
        {
            var cacheKey = "SideMenuItems";
            var cacheItem = (List<actionLink>)HttpContext.Current.Cache[cacheKey];

            if (bypassCache || cacheItem == null)
            {
                var list = EntityAccess.GetActionLinks(w => (w.actionLinkLevel == 2 || w.actionLinkLevel == 3) && w.active == true);


                if (list != null)
                {
                    cacheItem = list;
                    PutCache(cacheKey, cacheItem);
                }
            }

            return cacheItem;
        }

        public static List<Lookup> GetNgmdLookups(Predicate<Lookup> where = null, bool bypassCache = false)
        {
            string cacheKey = "LookupNgmd";
            object cacheItem = HttpContext.Current.Cache[cacheKey] as List<Lookup>;
            if ((bypassCache) || (cacheItem == null))
            {
                List<Lookup> li = new List<Lookup>();
                li = EntityAccess.ReadNgmdLookUp(x => true);
                if (li != null)
                {
                    cacheItem = li;
                    PutCache(cacheKey, cacheItem);
                }
            }

            if (where != null && cacheItem != null)
            {
                return ((List<Lookup>)cacheItem).FindAll(where).ToList();
            }
            return (List<Lookup>)cacheItem;
        }

        public static Dictionary<string, string> GetSectionData(string sectionName, bool bypassCache = false)
        {
            string cacheKey = sectionName;
            Dictionary<string, string> sectionData = GetCache<Dictionary<string, string>>(cacheKey);
            if ((bypassCache) || (sectionData == null))
            {
                sectionData = new Dictionary<string, string>();

                List<cmsEntry> settings = EntityAccess.ReadCms(x => x.cmsSection.sectionName == sectionName);
                foreach (cmsEntry setting in settings)
                {
                    sectionData.Add(setting.entryName, setting.cmsContent);
                }

                if (sectionData.Count > 0)
                {
                    PutCache(cacheKey, sectionData);
                }
            }

            return sectionData;
        }

        public static DataTable GetKpiData(bool bypassCache = false)
        {
            string cacheKey = "kpidata";
            DataTable dt  = GetCache<DataTable>(cacheKey);
            if ((bypassCache) || (dt == null))
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@TestMode", SqlDbType.Bit);
                sqlParm.Value = ConfigurationManager.AppSettings["Environment"] != "Live" ? 1 : 0;
                sqlParms.Add(sqlParm);

                dt = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetKPIValues", sqlParms, "KPIValues").Tables[0];
                PutCache(cacheKey, dt);
            }

            return dt;
        }

        public static DataTable GetItData(bool bypassCache = false)
        {
            string cacheKey = "itdata";
            DataTable dt = GetCache<DataTable>(cacheKey);
            if ((bypassCache) || (dt == null))
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                dt = SQLUtilities.ExecuteReadStoredProcedure("netgiantMasterData", "ngmd.GetITData", sqlParms).Tables[0];
                PutCache(cacheKey, dt);
            }

            return dt;
        }

    }
}
