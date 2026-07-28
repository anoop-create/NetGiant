using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Reflection;
using System.Data.Entity.Core.Objects;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class WebsiteInventoryViewModel : HelperViewModel
    {
        private ngmdEntities _ctx;

        public WebsiteInventoryViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikWebsiteInventory> WebsiteInventoryList { get; set; }
        public websiteInventory WebInventory { get; set; }
        public IQueryable<SelectListItem> allCategoryCodes { get; set; }
        public IQueryable<SelectListItem> allWebsites { get; set; }
        public IQueryable<SelectListItem> allProducts { get; set; }
        public IQueryable<SelectListItem> allPromotionalGroups { get; set; }

        public WebsiteInventoryViewModel Get()
        {
            WebsiteInventoryList = _ctx.websiteInventory
                                       .Select(x => new TelerikWebsiteInventory
                                       {
                                           Id = x.websiteInventoryID,
                                           Category = x.categoryCode.categoryCodeName,
                                           PartNo = x.product.partNo,
                                           Website = x.Website.FriendlyName,
                                           Promotion = x.promotionalGroup.promotionalGroupName,
                                           LastUpdated = x.dateLastUpdate
                                       })
                                       .AsQueryable();
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
                        model.WebInventory = db.websiteInventory.Find(id);
                    }
                    else
                    {
                        model.WebInventory = new websiteInventory();
                    }
                    model.allCategoryCodes = SelectListViewModel.GetAllCategoryCodes(model.WebInventory.websiteFK);
                    model.allWebsites = SelectListViewModel.GetAllWebsites();
                    model.allProducts = SelectListViewModel.GetAllProducts();
                    model.allPromotionalGroups = SelectListViewModel.GetAllPromotionalGroups();
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
                    WebInventory.dateLastUpdate = DateTime.Now;
                    if (WebInventory.websiteInventoryID > 0)
                    {
                        //Note: save product changes after creating the axis queue details
                        db.Entry(WebInventory).State = EntityState.Modified;
                        CreateAXISQueueDetails(false, WebInventory);
                        db.SaveChanges();
                    }
                    else
                    {
                        //Note: save product chanages before creating the axis queue details
                        db.websiteInventory.Add(WebInventory);
                        db.SaveChanges();
                        CreateAXISQueueDetails(true, WebInventory);
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();

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

                    Type entityProductType = ObjectContext.GetObjectType(webInventory.GetType());

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
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
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

    public class TelerikWebsiteInventory
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string PartNo { get; set; }
        public string Website { get; set; }
        public string Promotion { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
