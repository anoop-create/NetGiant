using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ProductGroupViewModel
    {
        public IPagedList<productGroup> productGroupList { get; set; }
        public productGroup productGroupSingle { get; set; }
        public IQueryable<SelectListItem> AllProductTypes { get; set; }

        public ProductGroupViewModel Get(int? page, string searchTerm, string searchBy, int? productTypeId, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<productGroup> list = db.productGroup.Include("productType");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "name":
                            list = list.Where(x => x.productGroupName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "productType":
                            list = list.Where(x => x.productType.productTypeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (productTypeId != null && productTypeId > 0)
                {
                    list = list.Where(x => x.productTypeFK == productTypeId);
                }

                switch (orderBy)
                {
                    case "productGroupNameAsc":
                        list = list.OrderBy(x => x.productGroupName);
                        break;
                    case "productGroupNameDesc":
                        list = list.OrderByDescending(x => x.productGroupName);
                        break;
                    case "productGroupNoAsc":
                        list = list.OrderBy(x => x.productGroupNo);
                        break;
                    case "productGroupNoDesc":
                        list = list.OrderByDescending(x => x.productGroupNo);
                        break;
                    case "productTypeAsc":
                        list = list.OrderBy(x => x.productType.productTypeName);
                        break;
                    case "productTypeDesc":
                        list = list.OrderByDescending(x => x.productType.productTypeName);
                        break;
                    case "dateLastUpdatedAsc":
                        list = list.OrderBy(x => x.dateLastUpdate);
                        break;
                    case "dateLastUpdatedDesc":
                        list = list.OrderByDescending(x => x.dateLastUpdate);
                        break;
                    default:
                        list = list.OrderBy(x => x.productGroupName);
                        break;
                }

                productGroupList = list.ToPagedList(pageNumber, pageSize);

                AllProductTypes = SelectListViewModel.AllProductTypes();
            }

            return this;
        }

        public ProductGroupViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    productGroupSingle = db.productGroup.Find(id);
                }
                else
                {
                    productGroupSingle = new productGroup();
                }

                AllProductTypes = SelectListViewModel.AllProductTypes();
            }

            return this;
        }

        public bool Save(ProductGroupViewModel pgVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    pgVm.productGroupSingle.dateLastUpdate = DateTime.Now;

                    if (pgVm.productGroupSingle.productGroupID > 0)
                    {
                        db.Entry(pgVm.productGroupSingle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.productGroup.Add(pgVm.productGroupSingle);
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
                    productGroup pdGrp = db.productGroup.Find(id);
                    db.productGroup.Remove(pdGrp);
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
