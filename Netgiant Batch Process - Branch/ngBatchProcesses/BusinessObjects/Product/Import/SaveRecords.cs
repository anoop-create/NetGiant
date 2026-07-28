using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.Product.Import
{
    public class SaveRecords
    {
        public SaveRecords(int _websiteFK)
        {
            websiteFK = _websiteFK;
        }

        private int websiteFK;

        internal product PopulateProduct(ImportFields prodFields, product dbProduct)
        {
            if (prodFields.IsNew)
            {
                //dbProduct = CreateProduct(prodFields);
                //AutoSkuMapProduct(dbProduct);
                //SetupAxisQueue('C', dbProduct, prodFields);
            }
            else
            {
                UpdateProduct(prodFields, dbProduct);
            }

            return dbProduct;
        }

        private void AutoSkuMapProduct(product dbProduct)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                List<providerInventory> autoList = db.providerInventory.Include(x => x.provider)
                                                .Where(x => x.partNo == dbProduct.partNo).ToList();

                int stockCount = 0;

                foreach (providerInventory pi in autoList)
                {
                    skuMapping sm = new skuMapping();
                    sm.productFK = dbProduct.productID;
                    sm.altRef = dbProduct.partNo;
                    sm.providerFK = pi.providerFK;
                    sm.providerInventoryFK = pi.providerInventoryID;
                    sm.providerPartNo = pi.providerPartNo;
                    sm.supplierNo = pi.provider.axisSupplierRef;
                    pi.potentialNewProduct = false;
                    stockCount += pi.quantity;
                    db.Entry(sm).State = EntityState.Added;
                    db.Entry(pi).State = EntityState.Modified;
                }

                dbProduct.supplierStock = stockCount;
                db.Entry(dbProduct).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        internal void PopulateWebsiteInventory(ImportFields prodFields, product dbProduct)
        {
            if (prodFields.IsNew)
            {
                CreateWebsiteInventory(prodFields, dbProduct);
            }
            else
            {
                UpdateWebsiteInventory(prodFields, dbProduct);
            }
        }

        private void UpdateWebsiteInventory(ImportFields prodFields, product dbProduct)
        {
            websiteInventory wi = dbProduct.websiteInventory.Where(x => x.websiteFK == websiteFK).FirstOrDefault();

            if (wi != null)
            {
                if ((prodFields.CategoryCodeFK != null) && (prodFields.CategoryCodeFK != wi.categoryCodeFK))
                {
                    wi.categoryCodeFK = prodFields.CategoryCodeFK != null ? prodFields.CategoryCodeFK : wi.categoryCodeFK;

                    using (ngmdEntities db = new ngmdEntities())
                    {
                        db.Entry(wi).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                CreateWebsiteInventory(prodFields, dbProduct);
            }
        }

        private void CreateWebsiteInventory(ImportFields prodFields, product dbProduct)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                websiteInventory wi = new websiteInventory();
                wi.productFK = dbProduct.productID;
                wi.categoryCodeFK = prodFields.CategoryCodeFK;
                wi.websiteFK = websiteFK;
                wi.dateLastUpdate = DateTime.Now;
                db.Entry(wi).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        internal void PopulateAxisFields(ImportFields prdSup, product dbProd)
        {
            if (dbProd == null || dbProd.AxisFields == null)
            {
                CreateAxisFields(prdSup, dbProd);
                CreateAxisFieldsAdditional(prdSup, dbProd);
            }
            else
            {
                UpdateAxisFields(prdSup, dbProd);
                UpdateAxisFieldsAdditional(prdSup, dbProd);
            }
        }

        internal skuMapping CreateSkuMapping(SkuMappingFields sku)
        {
            skuMapping newSkuMapping = new skuMapping();
            newSkuMapping.productFK = GetProductFK(sku.AltRef);
            newSkuMapping.providerInventoryFK = GetProviderInventoryFK(sku.ProviderPartNo, sku.ProviderFK);
            newSkuMapping.providerPartNo = sku.ProviderPartNo;
            newSkuMapping.providerFK = sku.ProviderFK;
            newSkuMapping.supplierNo = sku.AxisSupplierNo;
            newSkuMapping.altRef = sku.AltRef;

            if (newSkuMapping.productFK == null)
            {
                throw new ApplicationException("Could not find a product with alt ref " + sku.AltRef);
            }

            if (newSkuMapping.providerInventoryFK == null)
            {
                throw new ApplicationException("Could not find a provider inventory item with provider part no " + sku.ProviderPartNo +
                                                    " for providerFK: " + sku.ProviderFK);
            }

            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(newSkuMapping).State = EntityState.Added;
                db.SaveChanges();
            }

            return newSkuMapping;
        }

        internal void UpdateProviderInventory(skuMapping sku)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                providerInventory pi = db.providerInventory.Find(sku.providerInventoryFK);
                if (pi != null)
                    pi.potentialNewProduct = false;

                db.Entry(pi).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void UpdateProduct(ImportFields prd, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                dbProd.productName = prd.productName != null ? prd.productName : dbProd.productName;
                dbProd.UNSPSCCode = prd.unspscCode != null ? prd.unspscCode : dbProd.UNSPSCCode;
                dbProd.UNSPSCCode = string.IsNullOrEmpty(dbProd.UNSPSCCode) ? "99999999" : dbProd.UNSPSCCode;
                dbProd.manufacturerFK = prd.manufacturerFK != null ? prd.manufacturerFK : dbProd.manufacturerFK;
                dbProd.productGroupFK = prd.productGroupFK != null ? prd.productGroupFK : dbProd.productGroupFK;
                dbProd.salesAreaGroupFK = prd.salesAreaGroupFK != null ? prd.salesAreaGroupFK : dbProd.salesAreaGroupFK;

                if (prd.productStatusFK != null)
                {
                    int prdStatusFK = Convert.ToInt32(prd.productStatusFK);
                    dbProd.productStatusFK = prdStatusFK;

                    //If Inactive or Not Required
                    if (prdStatusFK == 3 || prdStatusFK == 5)
                        RemoveFromWebsites(dbProd, db);

                }
                if (prd.dataSupplierFK != null)
                {
                    int dsFK = Convert.ToInt32(prd.dataSupplierFK);
                    dbProd.dataSupplierFK = dsFK;
                }

                db.Entry(dbProd).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void RemoveFromWebsites(product dbProd, ngmdEntities db)
        {
            db.websiteInventory.Where(x => x.productFK == dbProd.productID).ToList()
                                .ForEach(item => db.websiteInventory.Remove(item));
            db.SaveChanges();

        }

        private product CreateProduct(ImportFields prodFields)
        {
            product prod = new product();

            using (ngmdEntities db = new ngmdEntities())
            {
                prod.productName = prodFields.productName;
                prod.partNo = prodFields.partNo;
                prod.UNSPSCCode = prodFields.unspscCode;
                prod.manufacturerFK = prodFields.manufacturerFK;
                prod.productStatusFK = Convert.ToInt32(prodFields.productStatusFK);
                prod.productGroupFK = prodFields.productGroupFK;
                prod.salesAreaGroupFK = prodFields.salesAreaGroupFK;
                prod.dateLastUpdate = DateTime.Now;
                prod.dataSupplierFK = Convert.ToInt32(prodFields.dataSupplierFK);
                db.Entry(prod).State = EntityState.Added;
                db.SaveChanges();
            }

            return prod;
        }

        private void CreateAxisFields(ImportFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFields afNew = new AxisFields();
                afNew.productFK = dbProd.productID;
                afNew.stockReference = prdSup.stockRef;
                afNew.spec1 = prdSup.spec1;
                afNew.spec2 = prdSup.spec2;
                afNew.spec3 = prdSup.spec3;
                afNew.spec4 = prdSup.spec4;
                afNew.spec5 = prdSup.spec5;
                afNew.spec6 = prdSup.spec6;
                afNew.reSaleable = prdSup.reSaleable;
                afNew.discontinuedItem = prdSup.discontinuedItem;
                afNew.defaultDeliveryToCust = prdSup.defaultDeliveryToCust;
                afNew.attr1 = prdSup.attr1;
                afNew.attr2 = prdSup.attr2;
                afNew.attr3 = prdSup.attr3;
                afNew.attr4 = prdSup.attr4;
                afNew.attr5 = prdSup.attr5;
                afNew.attr6 = prdSup.attr6;
                afNew.attr7 = prdSup.attr7;
                afNew.attr8 = prdSup.attr8;
                afNew.attr9 = prdSup.attr9;
                afNew.attr10 = prdSup.attr10;
                afNew.published = prdSup.published;
                afNew.featured = prdSup.featured;
                afNew.bestSeller = prdSup.bestSeller;
                afNew.supressCnetImage = prdSup.supressCnetImage;
                afNew.supressCnetDesc = prdSup.supressCnetDesc;
                afNew.additionalInfoUrl = prdSup.additionalInfoUrl;
                afNew.stockRecordType = prdSup.stockRecordType;
                afNew.dateLastUpdate = DateTime.Now;
                db.Entry(afNew).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdateAxisFields(ImportFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFields dbAfs = dbProd.AxisFields;
                dbAfs.stockReference = prdSup.stockRef != null ? prdSup.stockRef : dbAfs.stockReference;
                dbAfs.spec1 = prdSup.spec1 != null ? prdSup.spec1 : dbAfs.spec1;
                dbAfs.spec2 = prdSup.spec2 != null ? prdSup.spec2 : dbAfs.spec2;
                dbAfs.spec3 = prdSup.spec3 != null ? prdSup.spec3 : dbAfs.spec3;
                dbAfs.spec4 = prdSup.spec4 != null ? prdSup.spec4 : dbAfs.spec4;
                dbAfs.spec5 = prdSup.spec5 != null ? prdSup.spec5 : dbAfs.spec5;
                dbAfs.spec6 = prdSup.spec6 != null ? prdSup.spec6 : dbAfs.spec6;
                dbAfs.reSaleable = prdSup.reSaleable != null ? prdSup.reSaleable : dbAfs.reSaleable;
                dbAfs.discontinuedItem = prdSup.discontinuedItem != null ? prdSup.discontinuedItem : dbAfs.discontinuedItem;
                dbAfs.defaultDeliveryToCust = prdSup.defaultDeliveryToCust != null ? prdSup.defaultDeliveryToCust : dbAfs.defaultDeliveryToCust;
                dbAfs.attr1 = UpdateSingleAttribute(prdSup.attr1, dbAfs.attr1);
                dbAfs.attr2 = UpdateSingleAttribute(prdSup.attr2, dbAfs.attr2);
                dbAfs.attr3 = UpdateSingleAttribute(prdSup.attr3, dbAfs.attr3);
                dbAfs.attr4 = UpdateSingleAttribute(prdSup.attr4, dbAfs.attr4);
                dbAfs.attr5 = UpdateSingleAttribute(prdSup.attr5, dbAfs.attr5);
                dbAfs.attr6 = UpdateSingleAttribute(prdSup.attr6, dbAfs.attr6);
                dbAfs.attr7 = UpdateSingleAttribute(prdSup.attr7, dbAfs.attr7);
                dbAfs.attr8 = UpdateSingleAttribute(prdSup.attr8, dbAfs.attr8);
                dbAfs.attr9 = UpdateSingleAttribute(prdSup.attr9, dbAfs.attr9);
                dbAfs.attr10 = UpdateSingleAttribute(prdSup.attr10, dbAfs.attr10);
                dbAfs.published = prdSup.published != null ? prdSup.published : dbAfs.published;
                dbAfs.featured = prdSup.featured != null ? prdSup.featured : dbAfs.featured;
                dbAfs.bestSeller = prdSup.bestSeller != null ? prdSup.bestSeller : dbAfs.bestSeller;
                dbAfs.supressCnetImage = prdSup.supressCnetImage != null ? prdSup.supressCnetImage : dbAfs.supressCnetImage;
                dbAfs.supressCnetDesc = prdSup.supressCnetDesc != null ? prdSup.supressCnetDesc : dbAfs.supressCnetDesc;
                dbAfs.additionalInfoUrl = prdSup.additionalInfoUrl != null ? prdSup.additionalInfoUrl : dbAfs.additionalInfoUrl;
                dbAfs.stockRecordType = prdSup.stockRecordType != null ? prdSup.stockRecordType : dbAfs.stockRecordType;
                dbAfs.dateLastUpdate = DateTime.Now;
                db.Entry(dbAfs).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private static int? UpdateSingleAttribute(int? fileAttrId, int? currentAttrId)
        {
            int? returnValue = null;

            if (fileAttrId != null)
            {
                if (fileAttrId > 0)
                {
                    returnValue = fileAttrId;
                }
                else
                {
                    returnValue = null;
                }
            }
            else
            {
                returnValue = currentAttrId;
            }

            return returnValue;
        }

        private void CreateAxisFieldsAdditional(ImportFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFieldsAdditional afaNew = new AxisFieldsAdditional();
                afaNew.websiteFK = websiteFK;
                afaNew.productFK = dbProd.productID;
                afaNew.stockNoteDesc = prdSup.stockNoteDesc;
                afaNew.googleFeedSite = prdSup.googleFeedSite;
                afaNew.googleFeedInclude = prdSup.googleFeedInclude;
                afaNew.googleFeedCategory = prdSup.googleFeedCategory;
                afaNew.googleFeedAvailability = prdSup.googleFeedAvailability;
                afaNew.googleFeedCondition = prdSup.googleFeedCondition;
                afaNew.bespokeFeedSite = prdSup.bespokeFeedSite;
                afaNew.bespokeFeedInclude = prdSup.bespokeFeedInclude;
                afaNew.bespokeFeedUseCustomShipCost = prdSup.bespokeFeedUseCustomShipCost;
                afaNew.bespokeFeedAvailability = prdSup.bespokeFeedAvailability;
                afaNew.bespokeFeedCondition = prdSup.bespokeFeedCondition;
                afaNew.metaDesc = prdSup.metaDesc;
                afaNew.metaKeywords = prdSup.metaKeywords;
                afaNew.metaTitle = prdSup.metaTitle;
                afaNew.dateLastUpdate = DateTime.Now;
                db.Entry(afaNew).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdateAxisFieldsAdditional(ImportFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFieldsAdditional dbAxsAdditional = dbProd.AxisFields.AxisFieldsAdditional.Where(x => x.websiteFK == prdSup.websiteFK).FirstOrDefault();
                //websiteInventory wi = db.websiteInventory.Where(x => x.productFK == dbProd.productID && x.websiteFK == prdSup.websiteFK).FirstOrDefault();

                //if (wi != null)
                //{
                if (dbAxsAdditional != null)
                {
                    dbAxsAdditional.metaDesc = prdSup.metaDesc != null ? prdSup.metaDesc : dbAxsAdditional.metaDesc;
                    dbAxsAdditional.metaKeywords = prdSup.metaKeywords != null ? prdSup.metaKeywords : dbAxsAdditional.metaKeywords;
                    dbAxsAdditional.metaTitle = prdSup.metaTitle != null ? prdSup.metaTitle : dbAxsAdditional.metaTitle;
                    dbAxsAdditional.googleFeedSite = prdSup.googleFeedSite != null ? prdSup.googleFeedSite : dbAxsAdditional.googleFeedSite;
                    dbAxsAdditional.googleFeedInclude = prdSup.googleFeedInclude != null ? prdSup.googleFeedInclude : dbAxsAdditional.googleFeedInclude;
                    dbAxsAdditional.googleFeedCategory = prdSup.googleFeedCategory != null ? prdSup.googleFeedCategory : dbAxsAdditional.googleFeedCategory;
                    dbAxsAdditional.googleFeedAvailability = prdSup.googleFeedAvailability != null ? prdSup.googleFeedAvailability : dbAxsAdditional.googleFeedAvailability;
                    dbAxsAdditional.googleFeedCondition = prdSup.googleFeedCondition != null ? prdSup.googleFeedCondition : dbAxsAdditional.googleFeedCondition;
                    dbAxsAdditional.bespokeFeedSite = prdSup.bespokeFeedSite != null ? prdSup.bespokeFeedSite : dbAxsAdditional.bespokeFeedSite;
                    dbAxsAdditional.bespokeFeedInclude = prdSup.bespokeFeedInclude != null ? prdSup.bespokeFeedInclude : dbAxsAdditional.bespokeFeedInclude;
                    dbAxsAdditional.bespokeFeedUseCustomShipCost = prdSup.bespokeFeedUseCustomShipCost != null ? prdSup.bespokeFeedUseCustomShipCost : dbAxsAdditional.bespokeFeedUseCustomShipCost;
                    dbAxsAdditional.bespokeFeedAvailability = prdSup.bespokeFeedAvailability != null ? prdSup.bespokeFeedAvailability : dbAxsAdditional.bespokeFeedAvailability;
                    dbAxsAdditional.bespokeFeedCondition = prdSup.bespokeFeedCondition != null ? prdSup.bespokeFeedCondition : dbAxsAdditional.bespokeFeedCondition;
                    dbAxsAdditional.stockNoteDesc = prdSup.stockNoteDesc != null ? prdSup.stockNoteDesc : dbAxsAdditional.stockNoteDesc;
                    dbAxsAdditional.dateLastUpdate = DateTime.Now;
                    db.Entry(dbAxsAdditional).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    CreateAxisFieldsAdditional(prdSup, dbProd);
                }
                //}
            }
        }

        private void SetupAxisQueue(char crud, product dbProduct, ImportFields prodFields)
        {
            switch (crud)
            {
                case 'C':
                    if (dbProduct.productStatusFK == 1)
                    {
                        AXISQueue axQueue = CreateAxisQueueEntry(dbProduct);
                        CreateAxisQueueDetailsEntry(dbProduct, axQueue, "", "", true);
                    }
                    break;
                case 'R':
                    break;
                case 'U':
                    break;
                case 'D':
                    break;
            }
        }

        Func<object, object, bool> diff = (x, y) => ToSafeString(x) != ToSafeString(y);

        internal void CompareFields(product origDbProduct, product dbProduct, ImportFields prodFields)
        {
            CheckEntity(origDbProduct.manufacturerFK, prodFields.manufacturerFK, dbProduct, "manufacturerFK");
            CheckEntity(origDbProduct.productName, prodFields.productName, dbProduct, "productName");
            CheckEntity(origDbProduct.UNSPSCCode, prodFields.unspscCode, dbProduct, "UNSPSCCode");
            CheckEntity(origDbProduct.productStatusFK, prodFields.productStatusFK, dbProduct, "productStatusFK");
            CheckEntity(origDbProduct.productGroupFK, prodFields.productGroupFK, dbProduct, "productGroupFK");
            CheckEntity(origDbProduct.salesAreaGroupFK, prodFields.salesAreaGroupFK, dbProduct, "salesAreaGroupFK");
            CheckEntity(origDbProduct.dataSupplierFK, prodFields.dataSupplierFK, dbProduct, "dataSupplierFK");

            AxisFields af = origDbProduct.AxisFields;
            bool queueFull = false;

            if (af != null)
            {
                CompareAxisFields(origDbProduct, dbProduct, prodFields, af);
                AxisFieldsAdditional afa = af.AxisFieldsAdditional.Where(x => x.websiteFK == websiteFK).FirstOrDefault();

                if (afa != null)
                {
                    CompareAxisFieldsAdditional(origDbProduct, dbProduct, afa, prodFields);
                }
                else
                {
                    queueFull = true;
                }
            }
            else
            {
                queueFull = true;
            }

            if (queueFull)
                SetupAxisQueue('C', dbProduct, prodFields);
        }

        private void CompareAxisFieldsAdditional(product origDbProduct, product dbProduct, AxisFieldsAdditional afa, ImportFields prodFields)
        {
            CheckEntity(afa.metaDesc, prodFields.metaDesc, dbProduct, "metaDesc" + afa.websiteFK);
            CheckEntity(afa.metaKeywords, prodFields.metaKeywords, dbProduct, "metaKeywords" + afa.websiteFK);
            CheckEntity(afa.metaTitle, prodFields.metaTitle, dbProduct, "metaTitle" + afa.websiteFK);
            CheckEntity(afa.googleFeedInclude, prodFields.googleFeedInclude, dbProduct, "googleFeedInclude");
            CheckEntity(afa.bespokeFeedInclude, prodFields.bespokeFeedInclude, dbProduct, "bespokeFeedInclude");
            CheckEntity(afa.stockNoteDesc, prodFields.stockNoteDesc, dbProduct, "stockNoteDesc" + afa.websiteFK);
        }

        private void CompareAxisFields(product origDbProduct, product dbProduct, ImportFields prodFields, AxisFields af)
        {
            CheckEntity(af.additionalInfoUrl, prodFields.additionalInfoUrl, dbProduct, "additionalInfoUrl");
            CheckEntityAttribute(af.attr1, prodFields.attr1, dbProduct, "attr1");
            CheckEntityAttribute(af.attr2, prodFields.attr2, dbProduct, "attr2");
            CheckEntityAttribute(af.attr3, prodFields.attr3, dbProduct, "attr3");
            CheckEntityAttribute(af.attr4, prodFields.attr4, dbProduct, "attr4");
            CheckEntityAttribute(af.attr5, prodFields.attr5, dbProduct, "attr5");
            CheckEntityAttribute(af.attr6, prodFields.attr6, dbProduct, "attr6");
            CheckEntityAttribute(af.attr7, prodFields.attr7, dbProduct, "attr7");
            CheckEntityAttribute(af.attr8, prodFields.attr8, dbProduct, "attr8");
            CheckEntityAttribute(af.attr9, prodFields.attr9, dbProduct, "attr9");
            CheckEntityAttribute(af.attr10, prodFields.attr10, dbProduct, "attr10");
            CheckEntity(af.bestSeller, prodFields.bestSeller, dbProduct, "bestSeller");
            CheckEntity(af.defaultDeliveryToCust, prodFields.defaultDeliveryToCust, dbProduct, "defaultDeliveryToCust");
            CheckEntity(af.discontinuedItem, prodFields.discontinuedItem, dbProduct, "discontinuedItem");
            CheckEntity(af.featured, prodFields.featured, dbProduct, "featured");
            CheckEntity(af.published, prodFields.published, dbProduct, "published");
            CheckEntity(af.reSaleable, prodFields.reSaleable, dbProduct, "reSaleable");
            CheckEntity(af.spec1, prodFields.spec1, dbProduct, "spec1");
            CheckEntity(af.spec2, prodFields.spec2, dbProduct, "spec2");
            CheckEntity(af.spec3, prodFields.spec3, dbProduct, "spec3");
            CheckEntity(af.spec4, prodFields.spec4, dbProduct, "spec4");
            CheckEntity(af.spec5, prodFields.spec5, dbProduct, "spec5");
            CheckEntity(af.spec6, prodFields.spec6, dbProduct, "spec6");
            CheckEntity(af.stockRecordType, prodFields.stockRecordType, dbProduct, "stockRecordType");
            CheckEntity(af.stockReference, prodFields.stockRef, dbProduct, "stockReference");
            CheckEntity(af.supressCnetDesc, prodFields.supressCnetDesc, dbProduct, "supressCnetDesc");
            CheckEntity(af.supressCnetImage, prodFields.supressCnetImage, dbProduct, "supressCnetImage");
        }

        private void CheckEntity(object a, object b, product dbProduct, string fieldName)
        {
            if (b != null)
            {
                bool different = CompareEntity(diff, a, b);
                if (different)
                {
                    AXISQueue axQueue = CreateAxisQueueEntry(dbProduct);
                    CreateAxisQueueDetailsEntry(dbProduct, axQueue, "product", fieldName, false);
                }
            }
        }

        private void CheckEntityAttribute(object a, object b, product dbProduct, string fieldName)
        {
            if ((b != null && ToSafeString(b) != "0") || (ToSafeString(b) == "0" && a != null))
            {
                bool different = CompareEntity(diff, a, b);
                if (different)
                {
                    AXISQueue axQueue = CreateAxisQueueEntry(dbProduct);
                    CreateAxisQueueDetailsEntry(dbProduct, axQueue, "product", fieldName, false);
                }
            }
        }

        private bool CompareEntity(Func<object, object, bool> method, object newEntity, object dbOrigEntity)
        {
            bool different = false;

            if (method(newEntity, dbOrigEntity))
                different = true;

            return different;
        }

        private AXISQueue CreateAxisQueueEntry(product dbProduct)
        {
            AXISQueue aq;

            using (ngmdEntities db = new ngmdEntities())
            {
                aq = db.AXISQueue.Where(x => x.productFK == dbProduct.productID).FirstOrDefault();

                if (aq == null)
                {
                    aq = new AXISQueue();
                    aq.productFK = dbProduct.productID;
                    aq.dateLastUpdated = DateTime.Now;
                    db.Entry(aq).State = EntityState.Added;
                    db.SaveChanges();
                }
            }

            return aq;
        }

        private void CreateAxisQueueDetailsEntry(product dbProduct, AXISQueue aq, string entityName, string fieldName, bool isNew)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AXISQueueDetails aqd = new AXISQueueDetails();
                aqd.CRUD = isNew == true ? "C" : "U";
                aqd.createdDate = DateTime.Now;
                aqd.AXISQueueFK = aq.AXISQueueID;
                aqd.entityName = entityName != "" ? entityName : "All";
                aqd.fieldName = fieldName != "" ? fieldName : "All";
                db.Entry(aqd).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private int? GetProductFK(string partNo)
        {
            int? productFK = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                product prd = db.product.Where(x => x.partNo == partNo).FirstOrDefault();

                if (prd != null)
                {
                    productFK = prd.productID;
                }
            }

            return productFK;
        }

        private int? GetProviderInventoryFK(string providerPartNo, int? providerFK)
        {
            int? providerInventoryFK = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                providerInventory pi = db.providerInventory.Where(x => x.providerPartNo == providerPartNo &&
                                        x.providerFK == providerFK).FirstOrDefault();

                if (pi != null)
                {
                    providerInventoryFK = pi.providerInventoryID;
                }
            }

            return providerInventoryFK;
        }

        public static string ToSafeString(object obj)
        {
            return (obj ?? string.Empty).ToString();
        }

        internal void SaveEbusinessMapping(eBusinessMappingFields eBus)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisEbusinessMapping map = db.AxisEbusinessMapping.Where(x => x.productFK == eBus.productFK &&
                    x.eBusinessRef == eBus.eBusinessRef).FirstOrDefault();

                if (map == null)
                {
                    AxisEbusinessMapping newMapping = new AxisEbusinessMapping()
                    {
                        productFK = eBus.productFK,
                        eBusinessRef = eBus.eBusinessRef,
                        eBusinessPrimary = eBus.isPrimary == null || eBus.isPrimary == false ? false : true
                    };

                    SetEbusPrimaryOff(eBus, db);

                    db.Entry(newMapping).State = EntityState.Added;
                    db.SaveChanges();
                }
                else
                {
                    SetEbusPrimaryOff(eBus, db);
                    map.eBusinessPrimary = eBus.isPrimary == null || eBus.isPrimary == false ? false : true;

                    db.Entry(map).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
        }

        private static void SetEbusPrimaryOff(eBusinessMappingFields eBus, ngmdEntities db)
        {
            var mappingList = db.AxisEbusinessMapping.Where(x => x.productFK == eBus.productFK).ToList();

            if (eBus.isPrimary == true)
                mappingList.ForEach(x => x.eBusinessPrimary = false);
        }
    }
}
