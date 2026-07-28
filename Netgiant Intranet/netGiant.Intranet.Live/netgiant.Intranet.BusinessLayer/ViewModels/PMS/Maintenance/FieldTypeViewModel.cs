using netGiant.Intranet.DataLayer;
using System;
using System.Linq;
using PagedList;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FieldTypeViewModel
    {   
        public fieldType ft { get; set; }
        public PagedList.IPagedList<fieldType> fieldTypes { get; set; }

        public FieldTypeViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public FieldTypeViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldType> list = db.fieldType;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.fieldTypeName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "fieldTypeNameAsc":
                            list = list.OrderBy(x => x.fieldTypeName);
                            break;
                        case "fieldTypeNameDesc":
                            list = list.OrderByDescending(x => x.fieldTypeName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdated);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdated);
                            break;
                        default:
                            list = list.OrderBy(x => x.fieldTypeID);
                            break;
                    }

                    fieldTypes = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return this;
        }

        public static FieldTypeViewModel Create(int id)
        {
            FieldTypeViewModel model = new FieldTypeViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.ft = db.fieldType.Find(id);
                    }
                    else
                    {
                        model.ft = new fieldType();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ft.dateLastUpdated = DateTime.Now;

                    if (ft.fieldTypeID > 0)
                    {
                        db.Entry(ft).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldType.Add(ft);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    fieldType ftype = db.fieldType.Find(id);
                    db.fieldType.Remove(ftype);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
    }
}
