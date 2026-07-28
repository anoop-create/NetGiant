using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ProductTypeViewModel
    {
        public productType _productType { get; set; }
        public PagedList.IPagedList<productType> productTypes { get; set; }
        
        public ProductTypeViewModel Get()
        {
            return Get(null, "", "", "");
        }
        
        public ProductTypeViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<productType> list = db.productType;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.productTypeName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "productTypeNameAsc":
                            list = list.OrderBy(x => x.productTypeName);
                            break;
                        case "productTypeNameDesc":
                            list = list.OrderByDescending(x => x.productTypeName);
                            break;
                        case "productTypeNoAsc":
                            list = list.OrderBy(x => x.productTypeNo);
                            break;
                        case "productTypeNoDesc":
                            list = list.OrderByDescending(x => x.productTypeNo);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.productTypeName);
                            break;
                    }

                    productTypes = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProductTypeViewModel Create(int id)
        {
            ProductTypeViewModel model = new ProductTypeViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model._productType = db.productType.Find(id);
                    }
                    else
                    {
                        model._productType = new productType();
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
                    _productType.dateLastUpdate = DateTime.Now;

                    if (_productType.productTypeID > 0)
                    {
                        db.Entry(_productType).State = EntityState.Modified;
                    }
                    else
                    {
                        db.productType.Add(_productType);
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
                    productType pt = db.productType.Find(id);
                    db.productType.Remove(pt);
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
