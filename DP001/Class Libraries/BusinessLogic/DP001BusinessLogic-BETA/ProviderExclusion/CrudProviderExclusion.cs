using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DP001BusinessLogic.ViewModels;

namespace DP001BusinessLogic
{
    public class CrudProviderExclusion
    {
        public ProviderExclusion Create(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ProviderExclusionID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public List<ProviderExclusionViewModel.Telerik> Read(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                //var exclusions = db.ProviderExclusions
                //    .Include(x => x.Lookup)
                //    .Include(x => x.Lookup1)
                //    .Where(x => x.ChannelFK == channelId).ToList();

                CrudLookup crudLookup = new CrudLookup();
                int activeStatus = crudLookup.Read(x => x.LookupType.LookupTypeName == "Status" && x.LookupName == "Active").FirstOrDefault().LookupID;
                int dormantStatus = crudLookup.Read(x => x.LookupType.LookupTypeName == "Status" && x.LookupName == "Dormant").FirstOrDefault().LookupID;

                var exclusions = (from a in db.ProviderExclusions
                    join b in db.CompetitorInventories on new { A = a.ProviderFK, B = a.ClientProductID, C = activeStatus } equals new { A = b.CompetitorFK, B = b.ClientProductID, C = b.StatusFK ?? dormantStatus } into joinedB
                    from b in joinedB.DefaultIfEmpty()
                    join c in db.Competitors on a.ProviderFK equals c.CompetitorID into joinedC
                    from c in joinedC.DefaultIfEmpty()
                    join d in db.Lookups on a.ExclusionTypeFk equals d.LookupID
                    join e in db.Lookups on a.FileTypeFK equals e.LookupID
                    where a.ChannelFK == channelId
                    select new ProviderExclusionViewModel.Telerik
                    {
                        ProviderExclusionId = a.ProviderExclusionID,
                        ExclusionType = d.LookupName,
                        ProviderName = c.CompetitorName,
                        BrandName = a.BrandName,
                        ProdId = a.ClientProductID,
                        Mfpn = a.ManufacturerPartNo,
                        ExclusionLevel = e.LookupName,
                        Comment = a.Comment
                    }).ToList();

                return exclusions;
            }
        }

        public List<ProviderExclusion> Read(Expression<Func<ProviderExclusion, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProviderExclusions
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public void Update(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}

