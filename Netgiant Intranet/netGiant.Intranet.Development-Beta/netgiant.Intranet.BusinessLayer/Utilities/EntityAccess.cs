using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class EntityAccess
    {
        public static List<actionLink> GetActionLinks(Expression<Func<actionLink, bool>> where)
        {
            var list = new List<actionLink>();

            using (ngmdEntities db = new ngmdEntities())
            {
                list = db.actionLinks
                         .Where(where)
                         .OrderBy(o => o.actionLinkDesc)
                         .ToList();
            }

            return list;
        }

        public static List<Lookup> ReadNgmdLookUp(Expression<Func<Lookup, bool>> where)
        {
            var list = new List<Lookup>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    list = db.Lookup
                            .Include("LookupType")
                            .Where(where)
                            .ToList();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return list;
        }

        public static List<cmsEntry> ReadCms(Expression<Func<cmsEntry, bool>> where)
        {
            return ReadCms(where, 4);
        }

        public static List<cmsEntry> ReadCms(Expression<Func<cmsEntry, bool>> where, int websiteid)
        {
            List<cmsEntry> ret = new List<cmsEntry>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    //ret = db.cmsEntry
                    //    .Where(where)
                    //    .Where(x => x.cmsSection.websiteFK == websiteid)
                    //    .OrderBy(x => x.entryName)
                    //    .ToList();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
            return ret;
        }


        public static string ReturnEmptyString(string FromThis)
        {
            if (string.IsNullOrEmpty(FromThis) == true)
            {
                return "";
            }
            return FromThis;
        }
    }
}
