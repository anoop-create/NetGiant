using netGiant.Intranet.DataLayer;
using System;
using System.Linq;
using System.Data.Entity;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FieldSubSectionViewModel
    {
        public FieldSubSectionViewModel()
        {
            allFieldSections = SelectListViewModel.AllFieldSections();
        }
        
        public fieldSubSection fss { get; set; }
        public PagedList.IPagedList<fieldSubSection> fieldSubSections { get; set; }
        public IQueryable<SelectListItem> allFieldSections { get; set; }
        public int SelectedFieldSectionID { get; set; }

        public FieldSubSectionViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public FieldSubSectionViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldSubSection> list = db.fieldSubSection.Include(i => i.fieldSection);

                    if (SelectedFieldSectionID > 0)
                        list = list.Where(x => x.fieldSectionFK == SelectedFieldSectionID);

                    if (!string.IsNullOrEmpty(searchTerm.Trim()))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.fieldSubSectionName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "fieldSubSectionNameAsc":
                            list = list.OrderBy(x => x.fieldSubSectionName);
                            break;
                        case "fieldSubSectionNameDesc":
                            list = list.OrderByDescending(x => x.fieldSubSectionName);
                            break;
                        case "sequenceNoAsc":
                            list = list.OrderBy(x => x.sequenceNo);
                            break;
                        case "sequenceNoDesc":
                            list = list.OrderByDescending(x => x.sequenceNo);
                            break;
                        case "fieldSectionAsc":
                            list = list.OrderBy(x => x.fieldSection.fieldSectionName);
                            break;
                        case "fieldSectionDesc":
                            list = list.OrderByDescending(x => x.fieldSection.fieldSectionName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdated);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdated);
                            break;
                        default:
                            list = list.OrderBy(x => x.sequenceNo);
                            break;
                    }

                    fieldSubSections = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return this;
        }

        public static FieldSubSectionViewModel Create(int id)
        {
            FieldSubSectionViewModel model = new FieldSubSectionViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.fss = db.fieldSubSection.Find(id);
                    }
                    else
                    {
                        model.fss = new fieldSubSection();
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
                    fss.dateLastUpdated = DateTime.Now;

                    if (fss.fieldSubSectionID > 0)
                    {
                        db.Entry(fss).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldSubSection.Add(fss);
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
                    fieldSubSection fSubSec = db.fieldSubSection.Find(id);
                    db.fieldSubSection.Remove(fSubSec);
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
