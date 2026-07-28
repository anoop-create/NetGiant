using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Reflection;
using netGiant.Intranet.DataLayer.Models;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class ProductFieldViewModel : HelperViewModel
    {        
        public ProductFieldViewModel()
        {
            productSuppliers = new product();
            productPrices = new List<ProductPriceModel>();
            productCompetitors = new List<providerInventory>();
        }

        public int SelectedProductID { get; set; }
        public int SelectedWebsiteID { get; set; }
        public int SelectedFieldSectionID { get; set; }
        public int SelectedFieldSectionIndex { get; set; }
        public string SelectedProductTitle { get; set; }
        public string SelectedPartNo { get; set; }

        public IQueryable<SelectListItem> allWebistes { get; set; }
        public IQueryable<SelectListItem> allFieldSubSections { get; set; }
        public IList<fieldSection> fieldSections { get; set; }
        public IList<fieldValue> fieldValues { get; set; }
        public IList<fieldValue> EditfieldValues { get; set; }
        public product productSuppliers { get; set; }
        public List<ProductPriceModel> productPrices { get; set; }
        public List<providerInventory> productCompetitors { get; set; }

        public ProductFieldViewModel Get(int productID, int websiteID, int fieldSectionID)
        {
            SelectedWebsiteID = websiteID == 0 ? 1 : websiteID;
            SelectedProductID = productID;
            SelectedFieldSectionID = fieldSectionID == 0 ? 1 : fieldSectionID;
            
            using (ngmdEntities db = new ngmdEntities())
            {
                allWebistes = SelectListViewModel.AllWebsites();
                fieldSections = db.fieldSection.Include("fieldSubSection.fieldName.fieldType").ToList();
                EditfieldValues = new List<fieldValue>();

                product prd = db.product.Find(productID);
                SelectedPartNo = prd.partNo;
                SelectedProductTitle = prd.productName;
            }

            PopulateFieldValues();
            
            return this;
        }

        public IList<fieldValue> PopulateFieldValues()
        {
            fieldValues = new List<fieldValue>();

            //Create temp field values if they don't exist
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (fieldSection fSec in fieldSections)
                {
                    foreach (fieldSubSection fSubSec in fSec.fieldSubSection)
                    {
                        foreach (fieldName fName in fSubSec.fieldName)
                        {
                            if (fName.websiteSpecific)
                            {
                                if (!db.fieldValue.Any(x => x.productFK == SelectedProductID && x.fieldNameFK == fName.fieldNameID && x.websiteFK == SelectedWebsiteID))
                                {
                                    CreateTempFieldValue(fName.fieldNameID, SelectedWebsiteID);
                                }
                            }
                            else
                            {
                                if (!db.fieldValue.Any(x => x.productFK == SelectedProductID && x.fieldNameFK == fName.fieldNameID))
                                {
                                    CreateTempFieldValue(fName.fieldNameID, null);
                                }
                            }
                        }
                    }
                }
                
                //Get field values
                fieldValues = db.fieldValue.Include("fieldName.fieldType").Where(x => (x.productFK == SelectedProductID) &&
                    (x.websiteFK == SelectedWebsiteID || x.websiteFK == null)).ToList();

                //Get productSuppliers
                productSuppliers = GetProductSuppliers();

                //Get productPrices - Retail Price, Trade Price, Standard Cost
                productPrices = GetProductPrices();

                GetProductCompetitors();

                return fieldValues;
            }
        }

        public ProductFieldViewModel Edit(int productID, int websiteID, int fieldSectionID)
        {
            Get(productID, websiteID, fieldSectionID);

            foreach (fieldSection section in fieldSections.Where(x => x.fieldSectionID == SelectedFieldSectionID).OrderBy(x => x.sequenceNo))
            {
                foreach (fieldSubSection subSection in section.fieldSubSection.OrderBy(x => x.sequenceNo))
                {
                    foreach (fieldName fName in subSection.fieldName.OrderBy(x => x.sequenceNo))
                    {
                        EditfieldValues.Add(fieldValues.FirstOrDefault(x => x.fieldNameFK == fName.fieldNameID));
                    }
                }
            }

            allFieldSubSections = SelectListViewModel.AllFieldSubSections();

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (fieldValue val in EditfieldValues)
                    {
                        //set websiteFK
                        if (val.fieldNameFK > 0)
                        {
                            val.websiteFK = db.fieldName.FirstOrDefault(x => x.fieldNameID == val.fieldNameFK).websiteSpecific ? SelectedWebsiteID : (int?)null;
                        }

                        CreateFieldValueAXISQueue(false, val);
                        db.Entry(val).State = EntityState.Modified;
                        db.SaveChanges();

                        //change product's dateLastUpdated when fieldValue gets changed [Glen asked for it]
                        product pd = db.product.Find(val.productFK);
                        pd.dateLastUpdate = DateTime.Now;
                        db.Entry(pd).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.TargetSite + e.Message + e.InnerException);
            }
        }

        public static IList<fieldName> CreateFieldName(int fieldSectionID)
        {
            IList<fieldName> fieldNames = new List<fieldName>();
            
            using(ngmdEntities db = new ngmdEntities())
	        {
                fieldNames = db.fieldName.Where(x => x.fieldSubSection.fieldSectionFK == fieldSectionID).ToList();
	        }

            return fieldNames;
        }

        public void CreateTempFieldValue(Int16 fieldNameID, int? webisteID)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                fieldValue fv = new fieldValue();
                fv.fieldValueText = null;
                fv.fieldValueBool = null;
                fv.fieldValueDouble = null;
                fv.fieldNameFK = fieldNameID;
                fv.productFK = SelectedProductID;
                fv.websiteFK = webisteID;
                db.fieldValue.Add(fv);
                db.SaveChanges();
            }
        }

        public static void CreateFieldValueAXISQueue(bool isNew, fieldValue fv)
        {
            string[] ignoreList = { "fieldValueID" };

            using (ngmdEntities db = new ngmdEntities())
            {
                //Create AXIS Queue for the product if doesn't exist
                AXISQueue axisQueue = null;
                if (!db.AXISQueue.Any(x => x.productFK == fv.productFK))
                {
                    axisQueue = new AXISQueue() { productFK = fv.productFK.Value, dateLastUpdated = DateTime.Now };
                    db.AXISQueue.Add(axisQueue);
                    db.SaveChanges();
                }
                else
                {
                    axisQueue = db.AXISQueue.FirstOrDefault(x => x.productFK == fv.productFK);
                }

                //when product is added/updated
                if (fv != null)
                {
                    fieldValue existingFieldValue = db.fieldValue.Find(fv.fieldValueID);
                    Type entityFieldValueType = fv.GetType();

                    foreach (PropertyInfo propertyInfo in entityFieldValueType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && !ignoreList.Contains(p.Name)))
                    {
                        if (!isNew)
                        {
                            object valueA = propertyInfo.GetValue(fv, null);
                            object valueB = propertyInfo.GetValue(existingFieldValue, null);

                            // if it is a primative type, value type or implements IComparable, just directly try and compare the value
                            if (CanDirectlyCompare(propertyInfo.PropertyType))
                            {
                                if (!AreValuesEqual(valueA, valueB))
                                {
                                    //Create Queue Details
                                    AXISQueueDetails queueDetails = new AXISQueueDetails()
                                    {
                                        entityName = entityFieldValueType.Name,
                                        //fieldName = propertyInfo.Name,
                                        fieldName = db.fieldName.FirstOrDefault(x => x.fieldNameID == fv.fieldNameFK).fieldName1,
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
                                    entityName = entityFieldValueType.Name,
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

        //protected List<ProductSupplierModel> GetProductSuppliers()
        //{
        //    List<ProductSupplierModel> productSuppliers = new List<ProductSupplierModel>();
            
        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        product prd = db.product.Find(SelectedProductID);
                
        //        foreach (var ps in db.GetProductSuppliers(prd.partNo, prd.manufacturerFK).OrderBy(x => x.price))
        //        {
        //            productSuppliers.Add(new ProductSupplierModel()
        //            {
        //                providerID = ps.providerID.Value,
        //                providerName = ps.providerName,
        //                altRef = ps.partNo,
        //                providerPartNo = ps.providerPartNo,
        //                price = Convert.ToDouble(ps.price ?? 0),
        //                quantity = ps.quantity ?? 0,
        //                inventoryUpdatedOn = ps.inventoryUpdated.Value,
        //                priceUpdatedOn = ps.priceUpdatedOn.Value,
        //                axisSupplierRef = ps.axisSupplierRef ?? 0,
        //                untrustedProvider = ps.untrusted
        //            });
        //        }
        //    }

        //    return productSuppliers;
        //}

        protected product GetProductSuppliers()
        {
            product productSuppliers = new product();

            using (ngmdEntities db = new ngmdEntities())
            {
                productSuppliers = db.product.Where(x => x.productID == SelectedProductID)
                                        .Include("skuMapping.providerInventory.provider")
                                        .Include("skuMapping.providerInventory.providerPrice").FirstOrDefault();
            }

            return productSuppliers;
        }

        public ProductFieldViewModel GetProductCompetitors()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                productCompetitors = db.providerInventory.Include(m => m.providerPrice)
                                                        .Include(m => m.provider)
                                                        .Where(x => x.partNo == SelectedPartNo
                                                            && (x.provider.providerTypeFK == 1 ||
                                                            x.provider.providerTypeFK == 5)).ToList();
            }

            return this;
        }

        protected List<ProductPriceModel> GetProductPrices()
        {
            List<ProductPriceModel> productPrices = new List<ProductPriceModel>();

            using (ngmdEntities db = new ngmdEntities())
            {
                string altRef = db.product.Find(SelectedProductID).partNo;

                foreach (var pp in db.GetProductPrices(altRef))
                {
                    productPrices.Add(new ProductPriceModel()
                    {
                        Price = pp.price,
                        WebsiteName = pp.FriendlyName,
                        DateLastUpdate = pp.dateLastUpdated
                    });
                }
            }

            return productPrices;
        }
    }
}
