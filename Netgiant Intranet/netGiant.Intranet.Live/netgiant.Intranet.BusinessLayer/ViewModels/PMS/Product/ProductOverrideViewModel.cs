using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class ProductOverrideViewModel
    {
        public productOverride ProdOverride { get; set; }
        public PagedList.IPagedList<productOverride> ProductOverrides { get; set; }
        public IQueryable<SelectListItem> allOverrideTypes { get; set; }
        public IQueryable<SelectListItem> allProducts { get; set; }

        public ProductOverrideViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public ProductOverrideViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<productOverride> list = db.productOverride.Include(p => p.overrideType).Include(p => p.product);
                    
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "overrideType":
                                list = list.Where(x => x.overrideType.overrideTypeName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;

                            case "orginal":
                                list = list.Where(x => x.originalValue.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;

                            case "override":
                                list = list.Where(x => x.overrideValue.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;

                            default:
                                break;
                        }
                    }

                    //Sorting
                    switch (orderBy)
                    {
                        case "originalValAsc":
                            list = list.OrderBy(x => x.originalValue);
                            break;
                        case "originalValDesc":
                            list = list.OrderByDescending(x => x.originalValue);
                            break;
                        case "overrideValAsc":
                            list = list.OrderBy(x => x.overrideValue);
                            break;
                        case "overrideValDesc":
                            list = list.OrderByDescending(x => x.overrideValue);
                            break;
                        case "overrideRuleAsc":
                            list = list.OrderBy(x => x.overrideRule);
                            break;
                        case "overrideRuleDesc":
                            list = list.OrderByDescending(x => x.overrideRule);
                            break;
                        case "overrideTypeAsc":
                            list = list.OrderBy(x => x.overrideType.overrideTypeName);
                            break;
                        case "overrideTypeDesc":
                            list = list.OrderByDescending(x => x.overrideType.overrideTypeName);
                            break;
                        case "productAsc":
                            list = list.OrderBy(x => x.product.productName);
                            break;
                        case "productDesc":
                            list = list.OrderByDescending(x => x.product.productName);
                            break;
                        default:
                            list = list.OrderBy(x => x.productOverrideID);
                            break;
                    }

                    ProductOverrides = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch(Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProductOverrideViewModel Create(int id)
        {
            ProductOverrideViewModel model = new ProductOverrideViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.ProdOverride = db.productOverride.Find(id);
                    }
                    else
                    {
                        model.ProdOverride = new productOverride();
                    }

                    model.allOverrideTypes = SelectListViewModel.AllOverrideTypes();
                    model.allProducts = SelectListViewModel.AllProducts();
                }
            }

            catch(Exception e)
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
                    ProdOverride.dateLastUpdate = DateTime.Now;
                    if (ProdOverride.productOverrideID > 0)
                    {
                        db.Entry(ProdOverride).State = EntityState.Modified;
                    }
                    else
                    {
                        db.productOverride.Add(ProdOverride);
                    }

                    db.SaveChanges();
                }
            }

            catch(Exception e)
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
                    productOverride prodOverride = db.productOverride.Find(id);
                    db.productOverride.Remove(prodOverride);
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
