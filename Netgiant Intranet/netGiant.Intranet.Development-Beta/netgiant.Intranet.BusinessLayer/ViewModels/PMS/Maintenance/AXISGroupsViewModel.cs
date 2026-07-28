using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PagedList;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class AXISGroupsViewModel
    {
        public IPagedList<AXISGroups> axisGroups { get; set; }
        public AXISGroups axisGroup { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllCategories { get; set; }

        public AXISGroupsViewModel Get(int? page, string searchTerm, string searchBy,
                                        int? websiteID, int? categoryCodeID, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<AXISGroups> list = db.AXISGroups.Include("categoryCode").Include("website");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "name":
                            list = list.Where(x => x.AXISGroupName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "axisGroupNo":
                            list = list.Where(x => x.AXISGroupNo.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    list = list.Where(x => x.websiteFK == websiteID);
                }

                if (categoryCodeID != null && categoryCodeID > 0)
                {
                    list = list.Where(x => x.categoryCodeFK == categoryCodeID);
                }

                switch (orderBy)
                {
                    case "axisGroupNameAsc":
                        list = list.OrderBy(x => x.AXISGroupName);
                        break;
                    case "axisGroupNameDesc":
                        list = list.OrderByDescending(x => x.AXISGroupName);
                        break;
                    case "axisGroupNoAsc":
                        list = list.OrderBy(x => x.AXISGroupNo);
                        break;
                    case "axisGroupNoDesc":
                        list = list.OrderByDescending(x => x.AXISGroupNo);
                        break;
                    case "categoryCodeNameAsc":
                        list = list.OrderBy(x => x.categoryCode.categoryCodeName);
                        break;
                    case "categoryCodeNameDesc":
                        list = list.OrderByDescending(x => x.categoryCode.categoryCodeName);
                        break;
                    case "websiteAsc":
                        list = list.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        list = list.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "pEbusinessAsc":
                        list = list.OrderBy(x => x.p_eBusinessGroup);
                        break;
                    case "pEbusinessDesc":
                        list = list.OrderByDescending(x => x.p_eBusinessGroup);
                        break;
                    case "sEbusinessAsc":
                        list = list.OrderBy(x => x.s_eBusinessGroup);
                        break;
                    case "sEbusinessDesc":
                        list = list.OrderByDescending(x => x.s_eBusinessGroup);
                        break;
                    default:
                        list = list.OrderBy(x => x.AXISGroupName);
                        break;
                }

                axisGroups = list.ToPagedList(pageNumber, pageSize);
                AllWebsites = SelectListViewModel.AllWebsites();
            }

            return this;
        }

        public AXISGroupsViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (id > 0)
                {
                    axisGroup = db.AXISGroups.Find(id);
                    AllCategories = SelectListViewModel.AllCategoryCodes(axisGroup.websiteFK);
                }
                else
                {
                    axisGroup = new AXISGroups();
                    AllCategories = SelectListViewModel.AllCategoryCodes();
                }
            }

            AllWebsites = SelectListViewModel.AllWebsites();

            return this;
        }

        public bool Save(AXISGroupsViewModel axVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    axVm.axisGroup.dateLastUpdate = DateTime.Now;
                    axVm.axisGroup.p_eBusinessGroup = "";
                    axVm.axisGroup.s_eBusinessGroup = "";

                    if (axVm.axisGroup.AXISGroupsID > 0)
                    {
                        db.Entry(axVm.axisGroup).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.AXISGroups.Add(axVm.axisGroup);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace, ex.InnerException);
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
                    AXISGroups axGrp = db.AXISGroups.Find(id);
                    db.AXISGroups.Remove(axGrp);
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
