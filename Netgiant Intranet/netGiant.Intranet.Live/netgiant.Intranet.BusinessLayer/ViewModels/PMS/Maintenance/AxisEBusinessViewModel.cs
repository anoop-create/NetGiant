using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class AxisEBusinessViewModel
    {
        public List<AxisEbusiness> EbusinessGroupList { get; set; }
        public int EbusinessGroupListCount { get; set; }
        public AxisEbusiness AxisEBusinessGroup { get; set; }

        public AxisEBusinessViewModel GetEbusinessGroups()
        {
            return GetEbusinessGroups(null, null, null, 1);
        }

        public AxisEBusinessViewModel GetEbusinessGroups(string orderBy, string searchTerm, string searchBy, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<AxisEbusiness> query = db.AxisEbusiness;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "ref":
                            query = query.Where(x => x.eBusinessRef.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "code":
                            query = query.Where(x => x.eBusinessCode.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "description":
                            query = query.Where(x => x.description.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "refAsc":
                        query = query.OrderBy(x => x.eBusinessRef);
                        break;
                    case "refDesc":
                        query = query.OrderByDescending(x => x.eBusinessRef);
                        break;
                    case "codeAsc":
                        query = query.OrderBy(x => x.eBusinessCode);
                        break;
                    case "codeDesc":
                        query = query.OrderByDescending(x => x.eBusinessCode);
                        break;
                    case "descriptionAsc":
                        query = query.OrderBy(x => x.description);
                        break;
                    case "descriptionDesc":
                        query = query.OrderByDescending(x => x.description);
                        break;
                    default:
                        query = query.OrderBy(x => x.eBusinessRef);  
                        break;
                }

                EbusinessGroupListCount = query.Count();
                EbusinessGroupList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
            }
            return this;
        }

        public AxisEBusinessViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    AxisEBusinessGroup = db.AxisEbusiness.Where(x => x.AxisEbusinessID == id).FirstOrDefault();
                }
            }
            else
            {
                AxisEBusinessGroup = new AxisEbusiness();
            }

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (AxisEBusinessGroup.AxisEbusinessID > 0)
                    {
                        db.Entry(AxisEBusinessGroup).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(AxisEBusinessGroup).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public void Delete(int EBusinessGroupID)
        {
            try
            {
                if (EBusinessGroupID > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        AxisEbusiness eb = db.AxisEbusiness.Where(x => x.AxisEbusinessID == EBusinessGroupID).FirstOrDefault();
                        db.Entry(eb).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

    }
}
