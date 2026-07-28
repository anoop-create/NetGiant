using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudReportConfiguration
    {
        public static List<ReportConfiguration> Read(Expression<Func<ReportConfiguration, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ReportConfigurations
                    .Include("Lookup")
                    .Include("Lookup1")
                    .OrderBy(x => x.Name)
                    .Where(where);

                return query.ToList();
            }
        }

        public static ReportConfiguration Create(ReportConfiguration obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ReportConfigurationId == 0)
                {
                    obj.DateLastUpdated = CommonDataFunctions.GetCurrentDateTime();
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public static ReportConfiguration Update(ReportConfiguration obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ReportConfigurationId > 0)
                {
                    obj.DateLastUpdated = CommonDataFunctions.GetCurrentDateTime();
                    db.Entry(obj).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public static ReportConfiguration Delete(ReportConfiguration obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ReportConfigurationId > 0)
                {
                    db.Entry(obj).State = EntityState.Deleted;
                    db.SaveChanges();
                }

                return obj;
            }
        }
    }
}
