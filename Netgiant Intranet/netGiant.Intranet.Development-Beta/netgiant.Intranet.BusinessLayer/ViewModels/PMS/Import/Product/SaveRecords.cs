using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product
{
    public class SaveRecords
    {
        public SaveRecords(int _websiteFK, int _importPrimaryKey = 1) // _importPrimaryKey not relevant for ebusiness and sku mappings
        {
            websiteFK = _websiteFK;
            importPrimaryKey = _importPrimaryKey;
        }

        private int websiteFK;
        private int importPrimaryKey;

        internal product PopulateProduct(ProductFields prodFields, product dbProduct)
        {
            if (prodFields.IsNew)
            {
                dbProduct = CreateProduct(prodFields);
                AutoSkuMapProduct(dbProduct);
                SetupAxisQueue('C', dbProduct, prodFields);
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

        private void UpdateAssemblyComponents(ProductFields prodFields, product prod)
        {
            if (prodFields.AssemblyComponents == null) // nothing to import
                return;

            bool assemblyComponentsSaveNeeded = false;
            if (prodFields.AssemblyComponents.Count() != prod.assemblyComponent.Count())
            {
                assemblyComponentsSaveNeeded = true;
            }
            else
            {
                var orderedProdFields = prodFields.AssemblyComponents.OrderBy(x => x).ToList();
                var orderedDBFields = prod.assemblyComponent.OrderBy(x => x.assemblyComponentFK).ToList();

                int i = 0;
                foreach (var assCmpt in orderedDBFields)
                {
                    if (assCmpt.assemblyComponentFK != orderedProdFields[i])
                    {
                        assemblyComponentsSaveNeeded = true;
                        break;
                    }
                    i++;
                }
            }

            if (!assemblyComponentsSaveNeeded)
                return;


            // Delete old mappings and add new. Leave others untouched.
            using (ngmdEntities db = new ngmdEntities())
            {
                var deletionCandidateFK = new List<int>();

                db.assemblyComponent.
                    Where(x => x.assemblyProductFK == prod.productID) // all assemblyComponentFKs for this product
                    .ToList()
                    .ForEach(x => deletionCandidateFK.Add(x.assemblyComponentFK));

                // intersection to be left alone. Do not delete and write a new mapping, as mappings have data along with the mapping
                var intersectionOfOldAndNewFKs = deletionCandidateFK.Intersect(prodFields.AssemblyComponents); // prodFields.AssemblyComponents are component FK to add

                try
                {
                    // DB Deletion
                    foreach (var id in deletionCandidateFK)
                    {
                        if (!intersectionOfOldAndNewFKs.Contains(id))  // this is an int item, not an index
                        {
                            assemblyComponent assemblyComponent = db.assemblyComponent.Where(x => x.assemblyComponentFK == id && x.assemblyProductFK == prod.productID).FirstOrDefault();
                            db.assemblyComponent.Remove(assemblyComponent);
                            db.SaveChanges();
                        }
                    }

                    // DB Addition
                    foreach (var assCmpt in prodFields.AssemblyComponents)
                    {
                        if (!intersectionOfOldAndNewFKs.Contains(assCmpt))  // this is an int item, not an index
                        {
                            assemblyComponent assemblyComponent = new assemblyComponent();
                            assemblyComponent.assemblyProductFK = prod.productID;
                            assemblyComponent.assemblyComponentFK = assCmpt;
                            assemblyComponent.quantity = 1;
                            db.assemblyComponent.Add(assemblyComponent);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new ApplicationException(e.Message + e.StackTrace);
                }
            }
        }
        
        private void UpdatePrimaryAndSecondaryEbusinessGroups(ProductFields prodFields, product prod)
        {
            if (prodFields.PrimaryEbusinessGroup == null && prodFields.SecondaryEbusinessGroups == null) // nothing to import
                return;

            List<string> allEbusinessGroups = new List<string>();
            if (!string.IsNullOrEmpty(prodFields.PrimaryEbusinessGroup))
            {
                allEbusinessGroups.Add(prodFields.PrimaryEbusinessGroup);
            }

            if (prodFields.SecondaryEbusinessGroups != null)
            {
                foreach (var ebusgrp in prodFields.SecondaryEbusinessGroups)
                {
                    allEbusinessGroups.Add(ebusgrp);
                }
            }

            string oldDBPrimary = "";
            var thePrimary = prod.AxisEbusinessMapping.Where(x => x.eBusinessPrimary == true).FirstOrDefault();
            if (thePrimary != null)
            {
                oldDBPrimary = thePrimary.AxisEbusiness.eBusinessCode;
            }

            // Delete old mappings and add new. Primary flags may also need altering.
            using (ngmdEntities db = new ngmdEntities())
            {
                var deletionCandidateFK = new List<string>();

                db.AxisEbusinessMapping.
                    Where(x => x.productFK == prod.productID)
                    .ToList()
                    .ForEach(x => deletionCandidateFK.Add(x.AxisEbusiness.eBusinessCode));

                // Do not delete and write a new mapping, as mappings have data along with the mapping
                var intersectionOfOldAndNewBusinessRefs = deletionCandidateFK.Intersect(allEbusinessGroups);
                
                try
                {
                    // DB Deletion
                    foreach (var id in deletionCandidateFK)
                    {
                        if (!intersectionOfOldAndNewBusinessRefs.Contains(id))  // this is a string item, not an index
                        {
                            AxisEbusinessMapping axisEbusinessMapping = db.AxisEbusinessMapping.Where(x => x.AxisEbusiness.eBusinessCode.ToUpper().Equals(id.ToUpper()) && x.productFK == prod.productID).FirstOrDefault();
                            if (axisEbusinessMapping.eBusinessPrimary)
                            {
                                if (prodFields.PrimaryEbusinessGroup != null)
                                {
                                    db.AxisEbusinessMapping.Remove(axisEbusinessMapping);
                                    db.SaveChanges();
                                }
                                // else PrimaryEbusinessGroup was not specified in spreadsheet, don't deem it for deletion. We dealing only with secondaries
                            }
                            else
                            {
                                if (prodFields.SecondaryEbusinessGroups != null)
                                {
                                    db.AxisEbusinessMapping.Remove(axisEbusinessMapping);
                                    db.SaveChanges();
                                }
                                // else SecondaryEbusinessGroups was not specified in spreadsheet, don't deem any for deletion. We dealing only with primary
                            }
                        }
                    }

                    // DB Addition
                    foreach (var ebusCmpt in allEbusinessGroups)
                    {
                        if (!intersectionOfOldAndNewBusinessRefs.Contains(ebusCmpt))  // this is a string item, not an index
                        {
                            AxisEbusinessMapping axisEbusinessMapping = new AxisEbusinessMapping();
                            axisEbusinessMapping.productFK = prod.productID;

                            AxisEbusiness axisEbusiness = db.AxisEbusiness.Where(x => x.eBusinessCode.ToUpper().Equals(ebusCmpt.ToUpper())).FirstOrDefault();
                            axisEbusinessMapping.eBusinessRef = axisEbusiness.eBusinessRef;

                            if (prodFields.PrimaryEbusinessGroup != null && ebusCmpt.ToUpper().Equals(prodFields.PrimaryEbusinessGroup.ToUpper()))
                            {
                                axisEbusinessMapping.eBusinessPrimary = true;
                            }
                            db.AxisEbusinessMapping.Add(axisEbusinessMapping);
                            db.SaveChanges();
                        }
                    }

                    // DB Modify possible old primary is no longer primary
                    if (prodFields.PrimaryEbusinessGroup != null && !prodFields.PrimaryEbusinessGroup.ToUpper().Equals(oldDBPrimary.ToUpper()))
                    {
                        AxisEbusinessMapping newPrimary = db.AxisEbusinessMapping
                            .Where(x => x.AxisEbusiness.eBusinessCode.ToUpper().Equals(prodFields.PrimaryEbusinessGroup.ToUpper()) && x.productFK == prod.productID).FirstOrDefault();
                        if (newPrimary != null)
                        {
                            newPrimary.eBusinessPrimary = true;
                            db.Entry(newPrimary).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                        AxisEbusinessMapping oldPrimary = db.AxisEbusinessMapping
                            .Where(x => x.AxisEbusiness.eBusinessCode.ToUpper().Equals(oldDBPrimary.ToUpper()) && x.productFK == prod.productID).FirstOrDefault();
                        if (oldPrimary != null) // could be deleted now
                        {
                            oldPrimary.eBusinessPrimary = false;
                            db.Entry(oldPrimary).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    var queue = CreateAxisQueueEntry(prod.productID);
                    CreateAxisQueueDetailsEntry(queue, "product", "eBusiness", false);

                }
                catch (Exception e)
                {
                    throw new ApplicationException(e.Message + e.StackTrace);
                }
            }
        }

        internal void PopulateAxisFields(ProductFields prdSup, product dbProd)
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

        private void UpdateProduct(ProductFields prd, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (importPrimaryKey == 2) // Stock Ref
                {
                    dbProd.partNo = prd.PartNo != null ? prd.PartNo : dbProd.partNo; // can change Alt Ref
                }
                dbProd.productName = prd.ProductName ?? dbProd.productName;
                dbProd.UNSPSCCode = prd.UnspscCode ?? dbProd.UNSPSCCode;
                dbProd.UNSPSCCode = string.IsNullOrEmpty(dbProd.UNSPSCCode) ? "99999999" : dbProd.UNSPSCCode;
                dbProd.manufacturerFK = prd.ManufacturerFK ?? dbProd.manufacturerFK;
                dbProd.productGroupFK = prd.ProductGroupFK ?? dbProd.productGroupFK;
                dbProd.salesAreaGroupFK = prd.SalesAreaGroupFK ?? dbProd.salesAreaGroupFK;

                if (prd.StockRecordType != null && prd.StockRecordType.Equals("Stock Item"))/////////REN
                {
                    dbProd.productItemTypeFK = 1;
                }
                else if (prd.StockRecordType != null && prd.StockRecordType.Equals("Assembly"))
                {
                    dbProd.productItemTypeFK = 2;
                }
                else if (prd.StockRecordType != null && prd.StockRecordType.Equals("Manufacturer Assembly"))
                {
                    dbProd.productItemTypeFK = 3;
                }

                dbProd.pageYield = prd.PageYield ?? dbProd.pageYield;
                dbProd.capacity = prd.Capacity ?? dbProd.capacity;
                dbProd.barcode = prd.Barcode ?? dbProd.barcode;
                dbProd.secondaryCrossSellGroupIdent = prd.SecondaryCrossSellGroup ?? dbProd.secondaryCrossSellGroupIdent;

                if (prd.ProductStatusFK != null)
                {
                    int prdStatusFK = Convert.ToInt32(prd.ProductStatusFK);
                    dbProd.productStatusFK = prdStatusFK;

                    //If Inactive or Not Required
                    if (prdStatusFK == 3 || prdStatusFK == 5)
                        RemoveFromWebsites(dbProd, db);

                }
                if (prd.DataSupplierFK != null)
                {
                    int dsFK = Convert.ToInt32(prd.DataSupplierFK);
                    dbProd.dataSupplierFK = dsFK;
                }

                db.Entry(dbProd).State = EntityState.Modified;
                db.SaveChanges();

                UpdateAssemblyComponents(prd, dbProd);

                UpdatePrimaryAndSecondaryEbusinessGroups(prd, dbProd);
            }
        }

        private void RemoveFromWebsites(product dbProd, ngmdEntities db)
        {
            db.websiteInventory.Where(x => x.productFK == dbProd.productID).ToList()
                                .ForEach(item => db.websiteInventory.Remove(item));
            db.SaveChanges();

        }

        private product CreateProduct(ProductFields prodFields)
        {
            product prod = new product();

            using (ngmdEntities db = new ngmdEntities())
            {
                prod.productName = prodFields.ProductName;
                prod.partNo = prodFields.PartNo;
                prod.UNSPSCCode = prodFields.UnspscCode;
                prod.manufacturerFK = prodFields.ManufacturerFK;
                prod.productStatusFK = Convert.ToInt32(prodFields.ProductStatusFK);
                prod.productGroupFK = prodFields.ProductGroupFK;
                prod.salesAreaGroupFK = prodFields.SalesAreaGroupFK;
                prod.dateLastUpdate = DateTime.Now;
                prod.dataSupplierFK = Convert.ToInt32(prodFields.DataSupplierFK);
                prod.pageYield = prodFields.PageYield;
                prod.capacity = prodFields.Capacity;
                prod.productItemTypeFK = 1;
                db.Entry(prod).State = EntityState.Added;
                db.SaveChanges();
            }

            return prod;
        }

        private void CreateAxisFields(ProductFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFields afNew = new AxisFields();
                afNew.productFK = dbProd.productID;
                afNew.stockReference = prdSup.StockRef;
                afNew.spec1 = prdSup.Spec1;
                afNew.spec2 = prdSup.Spec2;
                afNew.spec3 = prdSup.Spec3;
                afNew.spec4 = prdSup.Spec4;
                afNew.spec5 = prdSup.Spec5;
                afNew.spec6 = prdSup.Spec6;
                afNew.reSaleable = prdSup.ReSaleable;
                afNew.discontinuedItem = prdSup.DiscontinuedItem;
                afNew.defaultDeliveryToCust = prdSup.DefaultDeliveryToCust;
                afNew.attr1 = prdSup.Attr1;
                afNew.attr2 = prdSup.Attr2;
                afNew.attr3 = prdSup.Attr3;
                afNew.attr4 = prdSup.Attr4;
                afNew.attr5 = prdSup.Attr5;
                afNew.attr6 = prdSup.Attr6;
                afNew.attr7 = prdSup.Attr7;
                afNew.attr8 = prdSup.Attr8;
                afNew.attr9 = prdSup.Attr9;
                afNew.attr10 = prdSup.Attr10;
                afNew.published = prdSup.Published;
                afNew.featured = prdSup.Featured;
                afNew.bestSeller = prdSup.BestSeller;
                afNew.supressOpenRangeImage = prdSup.SupressOpenRangeImage;
                afNew.supressOpenRangeSpec = prdSup.SupressOpenRangeSpec;
                afNew.additionalInfoUrl = prdSup.AdditionalInfoUrl;
                afNew.stockRecordType = prdSup.StockRecordType;
                afNew.dateLastUpdate = DateTime.Now;
                db.Entry(afNew).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdateAxisFields(ProductFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFields dbAfs = dbProd.AxisFields;
                if (importPrimaryKey == 1) // Alt Ref
                {
                    dbAfs.stockReference = prdSup.StockRef != null ? prdSup.StockRef : dbAfs.stockReference; // can change Stock Ref
                }
                dbAfs.spec1 = prdSup.Spec1 ?? dbAfs.spec1;
                dbAfs.spec2 = prdSup.Spec2 ?? dbAfs.spec2;
                dbAfs.spec3 = prdSup.Spec3 ?? dbAfs.spec3;
                dbAfs.spec4 = prdSup.Spec4 ?? dbAfs.spec4;
                dbAfs.spec5 = prdSup.Spec5 ?? dbAfs.spec5;
                dbAfs.spec6 = prdSup.Spec6 ?? dbAfs.spec6;
                dbAfs.reSaleable = prdSup.ReSaleable ?? dbAfs.reSaleable;
                dbAfs.discontinuedItem = prdSup.DiscontinuedItem ?? dbAfs.discontinuedItem;
                dbAfs.defaultDeliveryToCust = prdSup.DefaultDeliveryToCust ?? dbAfs.defaultDeliveryToCust;
                dbAfs.attr1 = UpdateSingleAttribute(prdSup.Attr1, dbAfs.attr1);
                dbAfs.attr2 = UpdateSingleAttribute(prdSup.Attr2, dbAfs.attr2);
                dbAfs.attr3 = UpdateSingleAttribute(prdSup.Attr3, dbAfs.attr3);
                dbAfs.attr4 = UpdateSingleAttribute(prdSup.Attr4, dbAfs.attr4);
                dbAfs.attr5 = UpdateSingleAttribute(prdSup.Attr5, dbAfs.attr5);
                dbAfs.attr6 = UpdateSingleAttribute(prdSup.Attr6, dbAfs.attr6);
                dbAfs.attr7 = UpdateSingleAttribute(prdSup.Attr7, dbAfs.attr7);
                dbAfs.attr8 = UpdateSingleAttribute(prdSup.Attr8, dbAfs.attr8);
                dbAfs.attr9 = UpdateSingleAttribute(prdSup.Attr9, dbAfs.attr9);
                dbAfs.attr10 = UpdateSingleAttribute(prdSup.Attr10, dbAfs.attr10);
                dbAfs.published = prdSup.Published ?? dbAfs.published;
                dbAfs.featured = prdSup.Featured ?? dbAfs.featured;
                dbAfs.bestSeller = prdSup.BestSeller ?? dbAfs.bestSeller;
                dbAfs.supressOpenRangeImage = prdSup.SupressOpenRangeImage ?? dbAfs.supressOpenRangeImage;
                dbAfs.supressOpenRangeSpec = prdSup.SupressOpenRangeSpec ?? dbAfs.supressOpenRangeSpec;
                dbAfs.additionalInfoUrl = prdSup.AdditionalInfoUrl ?? dbAfs.additionalInfoUrl;
                dbAfs.stockRecordType = prdSup.StockRecordType ?? dbAfs.stockRecordType;
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

        private void CreateAxisFieldsAdditional(ProductFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFieldsAdditional afaNew = new AxisFieldsAdditional();
                afaNew.websiteFK = websiteFK;
                afaNew.productFK = dbProd.productID;
                afaNew.stockNoteDesc = prdSup.StockNoteDesc;
                afaNew.priorityNote = prdSup.PriorityNote;
                afaNew.googleFeedSite = prdSup.GoogleFeedSite;
                afaNew.googleFeedInclude = prdSup.GoogleFeedInclude;
                afaNew.googleFeedCategory = prdSup.GoogleFeedCategory;
                afaNew.googleFeedAvailability = prdSup.GoogleFeedAvailability;
                afaNew.googleFeedCondition = prdSup.GoogleFeedCondition;
                afaNew.bespokeFeedSite = prdSup.BespokeFeedSite;
                afaNew.bespokeFeedInclude = prdSup.BespokeFeedInclude;
                afaNew.bespokeFeedUseCustomShipCost = prdSup.BespokeFeedUseCustomShipCost;
                afaNew.bespokeFeedAvailability = prdSup.BespokeFeedAvailability;
                afaNew.bespokeFeedCondition = prdSup.BespokeFeedCondition;
                afaNew.metaDesc = prdSup.MetaDesc;
                afaNew.metaKeywords = prdSup.MetaKeywords;
                afaNew.metaTitle = prdSup.MetaTitle;
                afaNew.dateLastUpdate = DateTime.Now;
                afaNew.googlePromotionId = prdSup.GooglePromotionId;
                afaNew.breakQuantity1 = prdSup.BreakQuantity1;
                afaNew.breakQuantity2 = prdSup.BreakQuantity2;
                afaNew.breakQuantity3 = prdSup.BreakQuantity3;
                db.Entry(afaNew).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        private void UpdateAxisFieldsAdditional(ProductFields prdSup, product dbProd)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AxisFieldsAdditional dbAxsAdditional = dbProd.AxisFields.AxisFieldsAdditional.Where(x => x.websiteFK == prdSup.WebsiteFK && x.productFK == dbProd.productID).FirstOrDefault();

                if (dbAxsAdditional != null)
                {
                    dbAxsAdditional.metaDesc = prdSup.MetaDesc ?? dbAxsAdditional.metaDesc;
                    dbAxsAdditional.metaKeywords = prdSup.MetaKeywords ?? dbAxsAdditional.metaKeywords;
                    dbAxsAdditional.metaTitle = prdSup.MetaTitle ?? dbAxsAdditional.metaTitle;
                    dbAxsAdditional.googleFeedSite = prdSup.GoogleFeedSite ?? dbAxsAdditional.googleFeedSite;
                    dbAxsAdditional.googleFeedInclude = prdSup.GoogleFeedInclude ?? dbAxsAdditional.googleFeedInclude;
                    dbAxsAdditional.googleFeedCategory = prdSup.GoogleFeedCategory ?? dbAxsAdditional.googleFeedCategory;
                    dbAxsAdditional.googleFeedAvailability = prdSup.GoogleFeedAvailability ?? dbAxsAdditional.googleFeedAvailability;
                    dbAxsAdditional.googleFeedCondition = prdSup.GoogleFeedCondition ?? dbAxsAdditional.googleFeedCondition;
                    dbAxsAdditional.bespokeFeedSite = prdSup.BespokeFeedSite ?? dbAxsAdditional.bespokeFeedSite;
                    dbAxsAdditional.bespokeFeedInclude = prdSup.BespokeFeedInclude ?? dbAxsAdditional.bespokeFeedInclude;
                    dbAxsAdditional.bespokeFeedUseCustomShipCost = prdSup.BespokeFeedUseCustomShipCost ?? dbAxsAdditional.bespokeFeedUseCustomShipCost;
                    dbAxsAdditional.bespokeFeedAvailability = prdSup.BespokeFeedAvailability ?? dbAxsAdditional.bespokeFeedAvailability;
                    dbAxsAdditional.bespokeFeedCondition = prdSup.BespokeFeedCondition ?? dbAxsAdditional.bespokeFeedCondition;
                    dbAxsAdditional.stockNoteDesc = prdSup.StockNoteDesc ?? dbAxsAdditional.stockNoteDesc;
                    dbAxsAdditional.priorityNote = prdSup.PriorityNote ?? dbAxsAdditional.priorityNote;
                    dbAxsAdditional.googlePromotionId = prdSup.GooglePromotionId ?? dbAxsAdditional.googlePromotionId;
                    dbAxsAdditional.breakQuantity1 = prdSup.BreakQuantity1 ?? dbAxsAdditional.breakQuantity1;
                    dbAxsAdditional.breakQuantity2 = prdSup.BreakQuantity2 ?? dbAxsAdditional.breakQuantity2;
                    dbAxsAdditional.breakQuantity3 = prdSup.BreakQuantity3 ?? dbAxsAdditional.breakQuantity3;
                    dbAxsAdditional.dateLastUpdate = DateTime.Now;
                    db.Entry(dbAxsAdditional).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    CreateAxisFieldsAdditional(prdSup, dbProd);
                }
            }
        }

        private void SetupAxisQueue(char crud, product dbProduct, ProductFields prodFields)
        {
            switch (crud)
            {
                case 'C':
                    if (dbProduct.productStatusFK == 1)
                    {
                        AXISQueue axQueue = CreateAxisQueueEntry(dbProduct.productID);
                        CreateAxisQueueDetailsEntry(axQueue, "", "", true);
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

        internal void CompareFields(product origDbProduct, product dbProduct, ProductFields prodFields)
        {
            CheckEntity(origDbProduct.manufacturerFK, prodFields.ManufacturerFK, dbProduct, "manufacturerFK");
            CheckEntity(origDbProduct.productName, prodFields.ProductName, dbProduct, "productName");
            CheckEntity(origDbProduct.UNSPSCCode, prodFields.UnspscCode, dbProduct, "UNSPSCCode");
            CheckEntity(origDbProduct.productStatusFK, prodFields.ProductStatusFK, dbProduct, "productStatusFK");
            CheckEntity(origDbProduct.productGroupFK, prodFields.ProductGroupFK, dbProduct, "productGroupNo");
            CheckEntity(origDbProduct.salesAreaGroupFK, prodFields.SalesAreaGroupFK, dbProduct, "salesAreaGroupNo");
            CheckEntity(origDbProduct.dataSupplierFK, prodFields.DataSupplierFK, dbProduct, "dataSupplierFK");

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

        private void CompareAxisFieldsAdditional(product origDbProduct, product dbProduct, AxisFieldsAdditional afa, ProductFields prodFields)
        {
            CheckEntity(afa.metaDesc, prodFields.MetaDesc, dbProduct, "metaDesc" + afa.websiteFK);
            CheckEntity(afa.metaKeywords, prodFields.MetaKeywords, dbProduct, "metaKeywords" + afa.websiteFK);
            CheckEntity(afa.metaTitle, prodFields.MetaTitle, dbProduct, "metaTitle" + afa.websiteFK);
            CheckEntity(afa.googleFeedInclude, prodFields.GoogleFeedInclude, dbProduct, "googleFeedInclude");
            CheckEntity(afa.bespokeFeedInclude, prodFields.BespokeFeedInclude, dbProduct, "bespokeFeedInclude");
            CheckEntity(afa.stockNoteDesc, prodFields.StockNoteDesc, dbProduct, "stockNoteDesc" + afa.websiteFK);
        }

        private void CompareAxisFields(product origDbProduct, product dbProduct, ProductFields prodFields, AxisFields af)
        {
            CheckEntity(af.additionalInfoUrl, prodFields.AdditionalInfoUrl, dbProduct, "additionalInfoUrl");
            CheckEntityAttribute(af.attr1, prodFields.Attr1, dbProduct, "attr1");
            CheckEntityAttribute(af.attr2, prodFields.Attr2, dbProduct, "attr2");
            CheckEntityAttribute(af.attr3, prodFields.Attr3, dbProduct, "attr3");
            CheckEntityAttribute(af.attr4, prodFields.Attr4, dbProduct, "attr4");
            CheckEntityAttribute(af.attr5, prodFields.Attr5, dbProduct, "attr5");
            CheckEntityAttribute(af.attr6, prodFields.Attr6, dbProduct, "attr6");
            CheckEntityAttribute(af.attr7, prodFields.Attr7, dbProduct, "attr7");
            CheckEntityAttribute(af.attr8, prodFields.Attr8, dbProduct, "attr8");
            CheckEntityAttribute(af.attr9, prodFields.Attr9, dbProduct, "attr9");
            CheckEntityAttribute(af.attr10, prodFields.Attr10, dbProduct, "attr10");
            CheckEntity(af.bestSeller, prodFields.BestSeller, dbProduct, "bestSeller");
            CheckEntity(af.defaultDeliveryToCust, prodFields.DefaultDeliveryToCust, dbProduct, "defaultDeliveryToCust");
            CheckEntity(af.discontinuedItem, prodFields.DiscontinuedItem, dbProduct, "discontinuedItem");
            CheckEntity(af.featured, prodFields.Featured, dbProduct, "featured");
            CheckEntity(af.published, prodFields.Published, dbProduct, "published");
            CheckEntity(af.reSaleable, prodFields.ReSaleable, dbProduct, "reSaleable");
            CheckEntity(af.spec1, prodFields.Spec1, dbProduct, "spec1");
            CheckEntity(af.spec2, prodFields.Spec2, dbProduct, "spec2");
            CheckEntity(af.spec3, prodFields.Spec3, dbProduct, "spec3");
            CheckEntity(af.spec4, prodFields.Spec4, dbProduct, "spec4");
            CheckEntity(af.spec5, prodFields.Spec5, dbProduct, "spec5");
            CheckEntity(af.spec6, prodFields.Spec6, dbProduct, "spec6");
            CheckEntity(af.stockRecordType, prodFields.StockRecordType, dbProduct, "stockRecordType");
            CheckEntity(af.stockReference, prodFields.StockRef, dbProduct, "stockReference");
            CheckEntity(af.supressOpenRangeSpec, prodFields.SupressOpenRangeSpec, dbProduct, "supressOpenRangeSpec");
            CheckEntity(af.supressOpenRangeImage, prodFields.SupressOpenRangeImage, dbProduct, "supressOpenRangeImage");
        }

        private void CheckEntity(object a, object b, product dbProduct, string fieldName)
        {
            if (b != null)
            {
                bool different = CompareEntity(diff, a, b);
                if (different)
                {
                    AXISQueue axQueue = CreateAxisQueueEntry(dbProduct.productID);
                    CreateAxisQueueDetailsEntry(axQueue, "product", fieldName, false);
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
                    AXISQueue axQueue = CreateAxisQueueEntry(dbProduct.productID);
                    CreateAxisQueueDetailsEntry(axQueue, "product", fieldName, false);
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

        private AXISQueue CreateAxisQueueEntry(int productId)
        {
            AXISQueue aq;

            using (ngmdEntities db = new ngmdEntities())
            {
                aq = db.AXISQueue.Where(x => x.productFK == productId).FirstOrDefault();

                if (aq == null)
                {
                    aq = new AXISQueue();
                    aq.productFK = productId;
                    aq.dateLastUpdated = DateTime.Now;
                    db.Entry(aq).State = EntityState.Added;
                    db.SaveChanges();
                }
            }

            return aq;
        }

        private void CreateAxisQueueDetailsEntry(AXISQueue aq, string entityName, string fieldName, bool isNew)
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
