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
    public class ProductStatusViewModel
    {
        public productStatus _productStatus { get; set; }
        public PagedList.IPagedList<productStatus> productStatuses { get; set; }

        public ProductStatusViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<productStatus> list = db.productStatus;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.productStatusName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "productStatusNameAsc":
                            list = list.OrderBy(x => x.productStatusName);
                            break;
                        case "productStatusNameDesc":
                            list = list.OrderByDescending(x => x.productStatusName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.productStatusName);
                            break;
                    }

                    productStatuses = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProductStatusViewModel Create(int id)
        {
            ProductStatusViewModel model = new ProductStatusViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model._productStatus = db.productStatus.Find(id);
                    }
                    else
                    {
                        model._productStatus = new productStatus();
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
                    _productStatus.dateLastUpdate = DateTime.Now;

                    if (_productStatus.productStatusID > 0)
                    {
                        db.Entry(_productStatus).State = EntityState.Modified;
                    }
                    else
                    {
                        db.productStatus.Add(_productStatus);
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
                    productStatus ps = db.productStatus.Find(id);
                    db.productStatus.Remove(ps);
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
