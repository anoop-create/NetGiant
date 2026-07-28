using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class OverrideTypeViewModel
    {
        public IPagedList<overrideType> overrideTypesList { get; set; }
        public overrideType overrideTypeSingle { get; set; }

        public OverrideTypeViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<overrideType> list = db.overrideTypes;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "overridetype":
                            list = list.Where(x => x.overrideTypeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "overrideTypeNameAsc":
                        list = list.OrderBy(x => x.overrideTypeName);
                        break;
                    case "overrideTypeNameDesc":
                        list = list.OrderByDescending(x => x.overrideTypeName);
                        break;
                    case "dateLastUpdatedAsc":
                        list = list.OrderBy(x => x.dateLastUpdate);
                        break;
                    case "dateLastUpdatedDesc":
                        list = list.OrderByDescending(x => x.dateLastUpdate);
                        break;
                    default:
                        list = list.OrderBy(x => x.overrideTypeName); 
                        break;
                }

                overrideTypesList = list.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public OverrideTypeViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    overrideTypeSingle = db.overrideTypes.Find(id);
                }
                else
                {
                    overrideTypeSingle = new overrideType();
                }
            }

            return this;
        }

        public bool Save(OverrideTypeViewModel otVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    otVm.overrideTypeSingle.dateLastUpdate = DateTime.Now;

                    if (otVm.overrideTypeSingle.overrideTypeID > 0)
                    {
                        db.Entry(otVm.overrideTypeSingle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.overrideTypes.Add(otVm.overrideTypeSingle);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool Delete(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    overrideType ot = db.overrideTypes.Find(id);
                    db.overrideTypes.Remove(ot);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }
    }
}
