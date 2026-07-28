using netGiant.Intranet.DataLayer;
using System;
using System.Linq;
using System.Data.Entity;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class FieldNameViewModel
    {
        public FieldNameViewModel()
        {
            allFieldSubSections = SelectListViewModel.AllFieldSubSections();
            allFieldSections = SelectListViewModel.AllFieldSections();
            allFieldTypes = SelectListViewModel.AllFieldTypes();
        }
        
        public fieldName fn { get; set; }
        public PagedList.IPagedList<fieldName> fieldNames { get; set; }
        public IQueryable<SelectListItem> allFieldSubSections { get; set; }
        public IQueryable<SelectListItem> allFieldSections { get; set; }
        public IQueryable<SelectListItem> allFieldTypes { get; set; }
        public int selectedFieldSubSectionID { get; set; }
        public int selectedFieldTypeID { get; set; }
        public fieldSubSection relatedFieldSubSection { get; set; }

        public FieldNameViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public FieldNameViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldName> list = db.fieldName.Include(i => i.fieldSubSection).Include(i => i.fieldType);

                    if (selectedFieldSubSectionID > 0)
                        list = list.Where(x => x.fieldSubSectionFK == selectedFieldSubSectionID);

                    if (selectedFieldTypeID > 0)
                        list = list.Where(x => x.fieldTypeFK == selectedFieldTypeID);

                    if (!string.IsNullOrEmpty(searchTerm.Trim()))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.fieldName1.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "fieldNameAsc":
                            list = list.OrderBy(x => x.fieldName1);
                            break;
                        case "fieldNameDesc":
                            list = list.OrderByDescending(x => x.fieldName1);
                            break;
                        case "sequenceNoAsc":
                            list = list.OrderBy(x => x.sequenceNo);
                            break;
                        case "sequenceNoDesc":
                            list = list.OrderByDescending(x => x.sequenceNo);
                            break;
                        case "AXISTableNameAsc":
                            list = list.OrderBy(x => x.AXISTableName);
                            break;
                        case "AXISTableNameDesc":
                            list = list.OrderByDescending(x => x.AXISTableName);
                            break;
                        case "AXISFieldNameAsc":
                            list = list.OrderBy(x => x.AXISFieldName);
                            break;
                        case "AXISFieldNameDesc":
                            list = list.OrderByDescending(x => x.AXISFieldName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdated);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdated);
                            break;
                        case "fieldTypeAsc":
                            list = list.OrderBy(x => x.fieldType.fieldTypeName);
                            break;
                        case "fieldTypeDesc":
                            list = list.OrderByDescending(x => x.fieldType.fieldTypeName);
                            break;
                        case "fieldSubSectionAsc":
                            list = list.OrderBy(x => x.fieldSubSection.fieldSubSectionName);
                            break;
                        case "fieldSubSectionDesc":
                            list = list.OrderByDescending(x => x.fieldSubSection.fieldSubSectionName);
                            break;
                        default:
                            list = list.OrderBy(x => x.fieldNameID);
                            break;
                    }

                    fieldNames = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return this;
        }

        public static FieldNameViewModel Create(int id)
        {
            FieldNameViewModel model = new FieldNameViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.fn = db.fieldName.Find(id);
                    }
                    else
                    {
                        model.fn = new fieldName();
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
                    fn.dateLastUpdated = DateTime.Now;

                    if (fn.fieldNameID > 0)
                    {
                        fn.fieldTypeFK = Convert.ToByte(fn.fieldTypeFK);
                        db.Entry(fn).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldName.Add(fn);
                    }

                    //isNew = db.ChangeTracker.Entries<fieldName>().Any(x => x.State == EntityState.Added) ? true : false;

                    db.SaveChanges();
                }
            }

            catch (DbEntityValidationException e)
            {
                foreach (var validationErrors in e.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    fieldName name = db.fieldName.Find(id);
                    db.fieldName.Remove(name);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetSubSectionsBySection(int sectionID)
        {
            return SelectListViewModel.AllFieldSubSections(sectionID);
        }
    }
}
