using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class SelectListViewModel : CommonViewModel
    {
        public static IQueryable<SelectListItem> GetAllProducts()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.product.Select(x => new SelectListItem
                    {
                        Value = x.productID.ToString(),
                        Text = x.productName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> SearchProductsPartNoDesc(string searchTerm)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<product> query = db.product
                        .Where(x => x.productStatusFK == 1 || x.productStatusFK == 8) // Active products only
                        .OrderBy(x => x.partNo);
                    return query.Select(x => new SelectListItem
                    {
                        Value = x.productID.ToString(),
                        Text = x.partNo + " - " + x.productName
                    })
                        .Where(w => w.Text.Contains(searchTerm)).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllProductsPartNoDesc(string searchTerm = null)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<product> query = db.product.OrderBy(x => x.partNo);
                    return query.Select(x => new SelectListItem
                    {
                        Value = x.productID.ToString(),
                        Text = x.partNo + " - " + x.productName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static List<JqueryFormatted> GetProductsArray(string searchTerm = null, int productItemType = 0, bool includeEmpty = false)
        {
            List<JqueryFormatted> list = new List<JqueryFormatted>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<product> query = db.product.OrderBy(x => x.partNo);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(x => x.partNo.ToLower().Contains(searchTerm.ToLower()) ||
                            x.productName.ToLower().Contains(searchTerm.ToLower()));
                    }

                    if (productItemType > 0)
                        query = query.Where(x => x.productItemTypeFK == productItemType);

                    list = query.Select(x => new JqueryFormatted { label = x.partNo + " - " + x.productName, 
                                    value = x.productID.ToString() }).Take(200).ToList();
                    if (includeEmpty)
                    {
                        JqueryFormatted emptyItem = new JqueryFormatted { label = "Empty Item", value = "NULL" };
                        list.Insert(0, emptyItem);
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return list;
        }

        public static List<JqueryFormatted> GetModelsArray(string searchTerm = null, int productItemType = 0, bool includeEmpty = false)
        {
            List<JqueryFormatted> list = new List<JqueryFormatted>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<eqEquipment> query = db.eqEquipment.OrderBy(x => x.description);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(x => x.description.ToLower().Contains(searchTerm.ToLower()) && x.dateInactive == null);
                    }

                    list = query.Select(x => new JqueryFormatted
                    {
                        label = x.description,
                        value = x.eqEquipmentID.ToString()
                    }).Take(200).ToList();
                    if (includeEmpty)
                    {
                        JqueryFormatted emptyItem = new JqueryFormatted { label = "Empty Item", value = "NULL" };
                        list.Insert(0, emptyItem);
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return list;
        }

        public static IQueryable<SelectListItem> GetAllPromotionalGroups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<promotionalGroup> query = db.promotionalGroup.OrderBy(x => x.promotionalGroupName);

                    return query.Select(x => new SelectListItem
                    {
                        Value = x.promotionalGroupId.ToString(),
                        Text = x.promotionalGroupName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllCategoryCodes(int? websiteID = null, bool primaryOnly = false)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<categoryCode> query = db.categoryCode.OrderBy(x => x.categoryCodeName);

                    if (websiteID != null)
                    {
                        query = query.Where(x => x.websiteFK == websiteID);
                    }

                    if (primaryOnly)
                        query = query.Where(x => x.isPrimary == true);

                    query = query.Where(x => !x.categoryCodeName.Contains("_OLD"));

                    return query.Select(x => new SelectListItem
                     {                      
                         Value = x.categoryCodeID.ToString(),
                         Text = x.categoryCodeName + " (" + db.categoryCode.Where(y => y.categoryCodeID == x.parentCategoryCodeID).FirstOrDefault().categoryCodeName + ")"
                     }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllWebsites()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.Website.OrderBy(x => x.FriendlyName).Select(x => new SelectListItem
                    {
                        Value = x.WebsiteID.ToString(),
                        Text = x.FriendlyName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static List<SelectListItem> GetAllManufacturers(bool showUnknown = false, bool showAll = false)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    List<SelectListItem> list = db.manufacturer.Distinct().OrderBy(x => x.manufacturerName).Select(x => new SelectListItem
                    {
                        Value = x.manufacturerID.ToString(),
                        Text = x.manufacturerName
                    }).ToList();

                    if (showUnknown == true)
                    {
                        list.Add(new SelectListItem { Text = "Unknown", Value = "-1" });
                    }

                    var manufacturers = list.OrderBy(x => x.Text).ToList();

                    if (showAll == true)
                    {
                        manufacturers.Insert(0, new SelectListItem { Text = "All", Value = "-2" });
                    }

                    return manufacturers;
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        //public static IQueryable<SelectListItem> GetAllProductTypes()
        //{
        //    return DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "ProductType")
        //            .OrderBy(x => x.LookupName)
        //            .Select(x => new SelectListItem
        //            {
        //                Value = x.AltLookupId.ToString(),
        //                Text = x.LookupName.ToString()
        //            }).ToList().AsQueryable();
        //}

        public static IQueryable<SelectListItem> GetAllEquipFamilies(int? manuID = null)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<eqFamily> query = db.eqFamily.OrderBy(x => x.description);
                    
                    if (manuID != null)
                        query = query.Where(x => x.manufacturerFK == manuID);
                    
                    return query.Select(x => new SelectListItem
                    {
                        Value = x.eqFamilyID.ToString(),
                        Text = x.description
                    }).ToList().AsQueryable();

                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllEquipment()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.eqEquipment.OrderBy(x => x.description).Select(x => new SelectListItem
                    {
                        Value = x.eqEquipmentID.ToString(),
                        Text = x.description
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllGranularities()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.qa_Granularity.OrderBy(x => x.Granularity).Select(x => new SelectListItem
                    {
                        Value = x.GranularityID.ToString(),
                        Text = x.Granularity
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        //public static IQueryable<SelectListItem> GetAllProductStatuses()
        //{
        //    try
        //    {
        //        using (ngmdEntities db = new ngmdEntities())
        //        {
        //            return db.productStatus.OrderBy(x => x.productStatusName).Select(x => new SelectListItem
        //            {
        //                Value = x.productStatusID.ToString(),
        //                Text = x.productStatusName
        //            }).ToList().AsQueryable();
        //        }
        //    }
        //    catch (InvalidOperationException e)
        //    {
        //        throw new ApplicationException(e.Message + e.StackTrace);
        //    }
        //}

        public static IQueryable<SelectListItem> GetAllProductGroups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.productGroup.OrderBy(x => x.productGroupName).Select(x => new SelectListItem
                    {
                        Value = x.productGroupID.ToString(),
                        Text = x.productGroupName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllSalesAreaGroups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.salesAreaGroup.OrderBy(x => x.salesAreaGroupName).Select(x => new SelectListItem
                    {
                        Value = x.salesAreaGroupID.ToString(),
                        Text = x.salesAreaGroupName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllProviders(int? providerTypeFK = null)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<provider> list = 
                        db.provider
                        .Where(x => x.providerTypeFK != 5)
                        .OrderBy(x => x.providerName);

                    if (providerTypeFK != null)
                    {
                        list = list.Where(x => x.providerTypeFK == providerTypeFK);
                    }

                    return list.Select(x => new SelectListItem
                    {
                        Value = x.providerID.ToString(),
                        Text = x.providerName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllProviderTypes()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.Lookup
                        .Where(x => x.LookupType.LookupTypeName == "ProviderType")
                        .OrderBy(x => x.AltLookupId)
                        .Select(x => new SelectListItem
                        {
                            Value = x.AltLookupId.ToString(),
                            Text = x.LookupName
                        })
                        .ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllDataSuppliers()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.dataSupplier.OrderBy(x => x.dataSupplierName).Select(x => new SelectListItem
                    {
                        Value = x.dataSupplierID.ToString(),
                        Text = x.dataSupplierName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllFilterableAtts()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.filterableAttribute.OrderBy(x => x.attributeName).Select(x => new SelectListItem
                    {
                        Value = x.filterableAttributeID.ToString(),
                        Text = x.attributeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllEquipManufacturers()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.manufacturer.Where(x => x.equipmentManuName != null)
                                        .OrderBy(x => x.manufacturerName).Select(x => new SelectListItem
                    {
                        Value = x.manufacturerID.ToString(),
                        Text = x.equipmentManuName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllPMSMappingFields(string providerType)
        {
            try
            {
                switch (providerType)
                {
                    case "Back Order Supplier":
                        {
                            return new List<SelectListItem>()
                            {
                                new SelectListItem() { Text = "Purchase Order Number", Value = "purchaseOrderNumber" },
                                new SelectListItem() { Text = "Supplier Order Number", Value = "supplierOrderNumber" },
                                new SelectListItem() { Text = "Order Date", Value = "orderDate"},
                                new SelectListItem() { Text = "Supplier Item Reference", Value = "supplierItemReference"},
                                new SelectListItem() { Text = "Item Reference", Value = "itemReference"},
                                new SelectListItem() { Text = "Quantity Ordered", Value = "quantityOrdered"},
                                new SelectListItem() { Text = "Quantity Supplied", Value = "quantitySupplied"},
                                new SelectListItem() { Text = "Quantity Outstanding", Value = "quantityOutstanding"},
                                new SelectListItem() { Text = "Stock Replenishment Date", Value = "stockReplenishmentDate"}
                            }.AsQueryable();
                        }
                    case "Dispatch Supplier":
                        {
                            return new List<SelectListItem>()
                            {
                                new SelectListItem() { Text = "Order Number", Value = "orderNumber" },
                                new SelectListItem() { Text = "First Name", Value = "firstName" },
                                new SelectListItem() { Text = "Tracking Link", Value = "trackingLink" },
                                new SelectListItem() { Text = "Product Rows", Value = "productRows" },
                                new SelectListItem() { Text = "Signature", Value = "signature" },
                                new SelectListItem() { Text = "Purchase Order Number", Value = "purchaseOrderNumber" },
                                new SelectListItem() { Text = "Courier", Value = "courier" },
                                new SelectListItem() { Text = "Email Address", Value = "emailAddress" },
                                new SelectListItem() { Text = "Tracking Number", Value = "trackingNumber" },
                                new SelectListItem() { Text = "Product Description", Value = "productDescription" },
                                new SelectListItem() { Text = "Product Quantity", Value = "productQuantity" }
                            }.AsQueryable();
                        }
                    default:
                        {
                            return new List<SelectListItem>()
                            {
                                ///Values are hardcoded here. we should consider to store these values in a table.
                                ///Note: if anyone changes the value or text then please change the values in the case statement
                                ///ProviderViewModel line: 315 approximately
                                ///
                                new SelectListItem() { Text = "Supplier Part No", Value = "spn" },
                                new SelectListItem() { Text = "Price", Value = "price" },
                                new SelectListItem() { Text = "Quantity", Value = "quantity"},
                                new SelectListItem() { Text = "Description", Value = "description"},
                                new SelectListItem() { Text = "Supplier Manu Ref", Value = "supManuRef"},
                                new SelectListItem() { Text = "Manu Part No", Value = "mfpn"},
                                new SelectListItem() { Text = "Barcode", Value = "barcode"}
                            }.AsQueryable();
                        }
                }

                //var list = new List<SelectListItem>()
                //{
                //    ///Values are hardcoded here. we should consider to store these values in a table.
                //    ///Note: if anyone changes the value or text then please change the values in the case statement
                //    ///ProviderViewModel line: 315 approximately
                //    ///
                //    new SelectListItem() { Text = "Supplier Part No", Value = "spn" },
                //    new SelectListItem() { Text = "Price", Value = "price" },
                //    new SelectListItem() { Text = "Quantity", Value = "quantity"},
                //    new SelectListItem() { Text = "Description", Value = "description"},
                //    new SelectListItem() { Text = "Supplier Manu Ref", Value = "supManuRef"},
                //    new SelectListItem() { Text = "Manu Part No", Value = "mfpn"},
                //    new SelectListItem() { Text = "Barcode", Value = "barcode"},
                //};
                //return list.AsQueryable();
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllSuppliers()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.provider.Where(x => x.providerTypeFK == 2).OrderBy(x => x.providerName)
                        .Select(x => new SelectListItem
                    {
                        Value = x.providerID.ToString(),
                        Text = x.providerName.ToString()
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllUnspscClasses()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.providerInventory
                             .Where(x => x.unspscClass != null)
                             .OrderBy(x => x.unspscClass)
                             .Select(x => new SelectListItem
                             {
                                 Value = x.unspscClass.ToString(),
                                 Text = x.unspscClass.ToString()
                             }).Distinct().ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAttributeDescs(int attrTypeFK, int attrNameFK)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.AxisValueLookup.OrderBy(x => x.attrValueDesc)
                             .Where(x => x.axisTypeNameFK == attrTypeFK &&
                                         x.attrNameFK == attrNameFK).Select(m => new SelectListItem
                                         {
                                             Text = m.attrValueDesc,
                                             Value = m.attrValueID.ToString()
                                         }).ToList().AsQueryable();
                }
            }
            catch (Exception e)
            {

                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetNgmdLookupSelectList(string lookupType, bool useAlt = true, bool alphaSequence = true)
        {
            try
            {
                Func<Lookup, object> sequence;
                if (alphaSequence) { sequence = x => x.LookupName; } else { sequence = x => x.Sequence; }

                using (ngmdEntities db = new ngmdEntities())
                {
                    return DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == lookupType)
                        .OrderBy(sequence)
                        //.OrderBy(x => x.LookupName)
                        .Select(x => new SelectListItem
                        {
                            Value = useAlt ? x.AltLookupId.ToString() : x.LookupId.ToString(),
                            Text = x.LookupName.ToString()
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetLookupTypeList(string GetThisLookupType)
        {
            IQueryable<SelectListItem> MyIQueryable;

            using (ngmdEntities db = new ngmdEntities())
            {
                MyIQueryable = db.Lookup
                    .Where(x => x.LookupType.LookupTypeName == GetThisLookupType)
                    .Select(x => new SelectListItem
                    {
                        Value = x.LookupId.ToString(),
                        Text = x.LookupName
                    })
                    .ToList()
                    .AsQueryable();
                return MyIQueryable;
            }
        }

        public static IQueryable<SelectListItem> GetAllEbusinessGroups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.AxisEbusiness.OrderBy(x => x.description)
                        .Select(x => new SelectListItem
                        {
                            Value = x.eBusinessRef.ToString(),
                            Text = x.eBusinessCode + ", " + x.eBusinessRef + " - " + x.description
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        //public static IQueryable<SelectListItem> GetAllProductItemType()
        //{
        //    return DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "ProductItemType")
        //            .OrderBy(x => x.LookupName)
        //            .Select(x => new SelectListItem
        //            {
        //                Value = x.AltLookupId.ToString(),
        //                Text = x.LookupName.ToString()
        //            }).ToList().AsQueryable();
        //}

        public static IQueryable<SelectListItem> GetAllStockItems()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.product.Where(x => x.productItemTypeFK != 2)
                        .Select(x => new SelectListItem
                        {
                            Value = x.productID.ToString(),
                            Text = x.partNo.ToString() + " - " + x.productName
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }
        }

        public static IQueryable<SelectListItem> GetAllVoucherPromoGroups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    return db.VoucherPromoGroup.OrderBy(x => x.GroupName).Select(x => new SelectListItem
                    {
                        Value = x.VoucherPromoGroupId.ToString(),
                        Text = x.GroupName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
        public static List<JqueryFormatted> GetProductsAddOn(string searchTerm = null, int productItemType = 0, bool includeEmpty = false)
        {
            List<JqueryFormatted> list = new List<JqueryFormatted>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    // Was "productStatusFK == 1 || productStatusFK == 7" - status 7 isn't the code used
                    // anywhere else in this codebase for "active"; SearchProductsPartNoDesc above (the
                    // other product-search method in this same file) uses 1 or 8, and comments that pair
                    // as "Active products only". This one-character typo meant any real product whose
                    // status is actually 8 - i.e. most/all of them - got filtered out before the search
                    // term was even evaluated, so both the part-number and description Contains() checks
                    // below always ran against an empty candidate set and returned no results either way -
                    // matching the reported "search by code" and "search by title" both finding nothing.
                    IQueryable<product> query = db.product.Where(x => x.productStatusFK == 1 || x.productStatusFK == 8); // Active products only

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query = query.Where(x => x.partNo.ToLower().Contains(searchTerm.ToLower()) ||
                            x.productName.ToLower().Contains(searchTerm.ToLower()));
                    }

                    if (productItemType > 0)
                        query = query.Where(x => x.productItemTypeFK == productItemType);

                    list = query.Select(x => new JqueryFormatted
                    {
                        label = x.productName,
                        value = x.productID.ToString()
                    }).Take(200).ToList();
                    if (includeEmpty)
                    {
                        JqueryFormatted emptyItem = new JqueryFormatted { label = "Empty Item", value = "NULL" };
                        list.Insert(0, emptyItem);
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return list;
        }
    }

    public class JqueryFormatted
    {
        public string label { get; set; }
        public string value { get; set; }
    }
}
