using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class SalesAreaGroupViewModel
    {
        public salesAreaGroup _salesAreaGroup { get; set; }
        public PagedList.IPagedList<salesAreaGroup> salesAreaGroups { get; set; }

        public SalesAreaGroupViewModel Get()
        {
            return Get(null, "", "", "");
        }
        
        public SalesAreaGroupViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<salesAreaGroup> list = db.salesAreaGroup;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.salesAreaGroupName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch(orderBy)
                    {
                        case "salesGroupNameAsc":
                            list = list.OrderBy(x => x.salesAreaGroupName);
                            break;
                        case "salesGroupNameDesc":
                            list = list.OrderByDescending(x => x.salesAreaGroupName);
                            break;
                        case "salesGroupNoAsc":
                            list = list.OrderBy(x => x.salesAreaGroupNo);
                            break;
                        case "salesGroupNoDesc":
                            list = list.OrderByDescending(x => x.salesAreaGroupNo);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.salesAreaGroupID);
                            break;
                    }

                    salesAreaGroups = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static SalesAreaGroupViewModel Create(int id)
        {
            SalesAreaGroupViewModel model = new SalesAreaGroupViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model._salesAreaGroup = db.salesAreaGroup.Find(id);
                    }
                    else
                    {
                        model._salesAreaGroup = new salesAreaGroup();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    _salesAreaGroup.dateLastUpdate = DateTime.Now;

                    if (_salesAreaGroup.salesAreaGroupID > 0)
                    {
                        db.Entry(_salesAreaGroup).State = EntityState.Modified;
                    }
                    else
                    {
                        db.salesAreaGroup.Add(_salesAreaGroup);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    salesAreaGroup sag = db.salesAreaGroup.Find(id);
                    db.salesAreaGroup.Remove(sag);
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
