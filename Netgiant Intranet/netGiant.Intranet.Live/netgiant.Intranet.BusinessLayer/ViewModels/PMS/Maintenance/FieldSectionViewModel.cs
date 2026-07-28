using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PagedList;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FieldSectionViewModel
    {
        public fieldSection fs { get; set; }
        public PagedList.IPagedList<fieldSection> fieldSections { get; set; }

        public FieldSectionViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public FieldSectionViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldSection> list = db.fieldSection;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.fieldSectionName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "fieldSectionNameAsc":
                            list = list.OrderBy(x => x.fieldSectionName);
                            break;
                        case "fieldSectionNameDesc":
                            list = list.OrderByDescending(x => x.fieldSectionName);
                            break;
                        case "sequenceNoAsc":
                            list = list.OrderBy(x => x.sequenceNo);
                            break;
                        case "sequenceNoDesc":
                            list = list.OrderByDescending(x => x.sequenceNo);
                            break;
                        default:
                            list = list.OrderBy(x => x.sequenceNo);
                            break;
                    }

                    fieldSections = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return this;
        }

        public static FieldSectionViewModel Create(int id)
        {
            FieldSectionViewModel model = new FieldSectionViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.fs = db.fieldSection.Find(id);
                    }
                    else
                    {
                        model.fs = new fieldSection();
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
                    fs.dateLastUpdated = DateTime.Now;

                    if (fs.fieldSectionID > 0)
                    {
                        db.Entry(fs).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldSection.Add(fs);
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
                    fieldSection fSec = db.fieldSection.Find(id);
                    db.fieldSection.Remove(fSec);
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
