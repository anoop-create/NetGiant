using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Reflection;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class WebsiteInventoryViewModel : HelperViewModel
    {
        public websiteInventory webInventory { get; set; }
        public PagedList.IPagedList<websiteInventory> webInventories { get; set; }
        public IQueryable<SelectListItem> allCategoryCodes { get; set; }
        public IQueryable<SelectListItem> allWebsites { get; set; }
        public IQueryable<SelectListItem> allProducts { get; set; }

        public WebsiteInventoryViewModel Get()
        {
            return Get(null, "", "", null, null, "");
        }

        public WebsiteInventoryViewModel Get(int? page, string searchTerm, string searchBy, 
                                            int? websiteID, int? categoryCodeID, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<websiteInventory> list = db.websiteInventory.Include("categoryCode").Include("product").Include("Website");

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "product":
                                list = list.Where(x => x.product.partNo.ToLower().Contains(searchTerm.ToLower().Trim()));
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

                    //Sorting
                    switch (orderBy)
                    {
                        case "categoryCodeNameAsc":
                            list = list.OrderBy(x => x.categoryCode.categoryCodeName);
                            break;
                        case "categoryCodeNameDesc":
                            list = list.OrderByDescending(x => x.categoryCode.categoryCodeName);
                            break;
                        case "partNoAsc":
                            list = list.OrderBy(x => x.product.partNo);
                            break;
                        case "partNoDesc":
                            list = list.OrderByDescending(x => x.product.partNo);
                            break;
                        case "websiteAsc":
                            list = list.OrderBy(x => x.Website.WebsiteName);
                            break;
                        case "websiteDesc":
                            list = list.OrderByDescending(x => x.Website.WebsiteName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.product.partNo);
                            break;
                    }

                    webInventories = list.ToPagedList(pageNumber, pageSize);
                    allWebsites = SelectListViewModel.AllWebsites();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static WebsiteInventoryViewModel Create(int id)
        {
            WebsiteInventoryViewModel model = new WebsiteInventoryViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.webInventory = db.websiteInventory.Find(id);
                    }
                    else
                    {
                        model.webInventory = new websiteInventory();
                    }

                    model.allCategoryCodes = SelectListViewModel.AllCategoryCodes(model.webInventory.websiteFK);
                    model.allWebsites = SelectListViewModel.AllWebsites();
                    model.allProducts = SelectListViewModel.AllProducts();
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
                    
                    //apply website inventory changes
                    webInventory.dateLastUpdate = DateTime.Now;
                    if (webInventory.websiteInventoryID > 0)
                    {
                        //Note: save product changes after creating the axis queue details
                        db.Entry(webInventory).State = EntityState.Modified;
                        CreateAXISQueueDetails(false, webInventory);
                        db.SaveChanges();
                    }
                    else
                    {
                        //Note: save product chanages before creating the axis queue details
                        db.websiteInventory.Add(webInventory);
                        db.SaveChanges();
                        CreateAXISQueueDetails(true, webInventory);
                    }

                    //db.SaveChanges();
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
                    websiteInventory webInventory = db.websiteInventory.Find(id);
                    db.websiteInventory.Remove(webInventory);

                    #region AXIS Queue
                    AXISQueue axisQueue = null;
                    if (!db.AXISQueue.Any(x => x.productFK == webInventory.productFK))
                    {
                        axisQueue = new AXISQueue() { productFK = webInventory.productFK, dateLastUpdated = DateTime.Now };
                        db.AXISQueue.Add(axisQueue);
                        db.SaveChanges();
                    }
                    else
                    {
                        axisQueue = db.AXISQueue.FirstOrDefault(x => x.productFK == webInventory.productFK);
                    }

                    Type entityProductType = webInventory.GetType();

                    AXISQueueDetails queueDetails = new AXISQueueDetails()
                    {
                        entityName = entityProductType.Name,
                        fieldName = "websiteInventoryID",
                        createdDate = DateTime.Now,
                        completedDate = null,
                        AXISQueueFK = axisQueue.AXISQueueID,
                        CRUD = GetEnumDescription(CRUD.Delete)
                    };

                    db.AXISQueueDetails.Add(queueDetails);
                    #endregion

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void CreateAXISQueueDetails(bool isNew, websiteInventory wi)
        {
            try
            {
                string[] ignoreList = { "websiteInventoryID", "dateLastUpdate" };
                
                using (ngmdEntities db = new ngmdEntities())
                {
                    //Create AXIS Queue for the product if doesn't exist
                    AXISQueue axisQueue = null;
                    if (!db.AXISQueue.Any(x => x.productFK == wi.productFK))
                    {
                        axisQueue = new AXISQueue() { productFK = wi.productFK, dateLastUpdated = DateTime.Now };
                        db.AXISQueue.Add(axisQueue);
                        db.SaveChanges();
                    }
                    else
                    {
                        axisQueue = db.AXISQueue.FirstOrDefault(x => x.productFK == wi.productFK);
                    }

                    //when product is added/updated
                    if (wi != null)
                    {
                        websiteInventory existingWI = db.websiteInventory.Find(wi.websiteInventoryID);
                        Type entityWIType = wi.GetType();

                        foreach (PropertyInfo propertyInfo in entityWIType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead && !ignoreList.Contains(p.Name)))
                        {
                            if (!isNew)
                            {
                                object valueA = propertyInfo.GetValue(wi, null);
                                object valueB = propertyInfo.GetValue(existingWI, null);

                                // if it is a primative type, value type or implements IComparable, just directly try and compare the value
                                if (CanDirectlyCompare(propertyInfo.PropertyType))
                                {
                                    if (!AreValuesEqual(valueA, valueB))
                                    {
                                        //Create Queue Details
                                        AXISQueueDetails queueDetails = new AXISQueueDetails()
                                        {
                                            entityName = entityWIType.Name,
                                            fieldName = propertyInfo.Name,
                                            createdDate = DateTime.Now,
                                            completedDate = null,
                                            AXISQueueFK = axisQueue.AXISQueueID,
                                            CRUD = GetEnumDescription(CRUD.Update)
                                        };

                                        db.AXISQueueDetails.Add(queueDetails);
                                        db.SaveChanges();
                                    }
                                }
                            }
                            else
                            {
                                // if it is a primative type, value type or implements IComparable, just directly try and compare the value
                                if (CanDirectlyCompare(propertyInfo.PropertyType))
                                {
                                    //Add queue for all attributes
                                    //Create Queue Details
                                    AXISQueueDetails queueDetails = new AXISQueueDetails()
                                    {
                                        entityName = entityWIType.Name,
                                        fieldName = propertyInfo.Name,
                                        createdDate = DateTime.Now,
                                        completedDate = null,
                                        AXISQueueFK = axisQueue.AXISQueueID,
                                        CRUD = GetEnumDescription(CRUD.Create)
                                    };

                                    db.AXISQueueDetails.Add(queueDetails);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.InnerException + e.TargetSite);
            }
        }
    }
}
