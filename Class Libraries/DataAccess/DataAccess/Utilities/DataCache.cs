using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections;

namespace BusinessLogic
{
    public class DataCache
    {
        public static T GetCache<T>(string cacheKey) where T : class
        {
            T item = HttpContext.Current.Cache[cacheKey] as T;
            return item;
        }

        public static bool PutCache(string cacheKey, object item)
        {
            HttpContext.Current.Cache.Insert(cacheKey, item, null,
                DateTime.Now.AddHours(4),
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

        public static Dictionary<string, string> GetSectionData(string sectionName)
        {
            string cacheKey = sectionName;
            Dictionary<string, string> sectionData = DataCache.GetCache<Dictionary<string, string>>(cacheKey);
            if (sectionData == null)
            {
                sectionData = new Dictionary<string, string>();

                List<cmsEntry> settings = DataCache.ReadCms(x => x.cmsSection.sectionName == sectionName);
                foreach (cmsEntry setting in settings)
                {
                    sectionData.Add(setting.entryName, setting.cmsContent);
                }

                DataCache.PutCache(cacheKey, sectionData);
            }

            return sectionData;
        }

        public static string GetCMSEntry(string sectionName, string entryName, bool bypassCache = false)
        {
            string cacheKey = sectionName + "/" + entryName;
            object cacheItem = HttpContext.Current.Cache[cacheKey] as string;
            if ((bypassCache) || (cacheItem == null))
            {
                string s = ReadCms(x => x.cmsSection.sectionName == sectionName 
                        && x.entryName == entryName).FirstOrDefault().cmsContent;

                if (s != null)
                {
                    cacheItem = s;
                    HttpContext.Current.Cache.Insert(cacheKey, cacheItem, null,
                DateTime.Now.AddHours(4),
                TimeSpan.Zero);
                }
            }
            return (string)cacheItem ?? "";
        }

        public static List<cmsEntry> ReadCms(Expression<Func<cmsEntry, bool>> where)
        {
            using (NgmdEntities db = new NgmdEntities())
            {
                int i = Int32.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

                List<cmsEntry> ret = db.cmsEntries
                    .Where(where)
                    .OrderBy(x => x.entryName)
                    .ToList();
                ret = ret.Where(x => x.cmsSection.websiteFK == i).ToList();
                return ret;
            }
        }     
    }

    //public class InMemoryCache : ICacheService
    //{
    //    public T GetOrSet<T>(string cacheKey, Func<T> getItemCallback) where T : class
    //    {
    //        T item = MemoryCache.Default.Get(cacheKey) as T;
    //        if (item == null)
    //        {
    //            item = getItemCallback();
    //            MemoryCache.Default.Add(cacheKey, item, DateTime.Now.AddMinutes(10));
    //        }
    //        return item;
    //    }
    //}

    //interface ICacheService
    //{
    //    T GetOrSet<T>(string cacheKey, Func<T> getItemCallback) where T : class;
    //}

}
