using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Shared
{
    public class SelectListViewModel
    {
        public IQueryable<SelectListItem> allOverrideTypes { get; set; }
        public IQueryable<SelectListItem> allProducts { get; set; }
        public IQueryable<SelectListItem> allCategoryCodes { get; set; }
        public IQueryable<SelectListItem> allWebsites { get; set; }
        public List<SelectListItem> allManufacturers { get; set; }
        public IQueryable<SelectListItem> allProductTypes { get; set; }
        public IQueryable<SelectListItem> allEquipFamilies { get; set; }
        public IQueryable<SelectListItem> allEquipment { get; set; }
        public IQueryable<SelectListItem> allGranularities { get; set; }
        public IQueryable<SelectListItem> allProductStatuses { get; set; }
        public IQueryable<SelectListItem> allProductGroups { get; set; }
        public IQueryable<SelectListItem> allSalesAreaGroup { get; set; }
        public IQueryable<SelectListItem> allProviders { get; set; }
        public IQueryable<SelectListItem> allProviderTypes { get; set; }
        public IQueryable<SelectListItem> allDataSuppliers { get; set; }
        public IQueryable<SelectListItem> allFilterableAtts { get; set; }
        public IQueryable<SelectListItem> allEquipManufacturers { get; set; }
        public IQueryable<SelectListItem> allPMSMappingFields { get; set; }
        public IQueryable<SelectListItem> allProductsPartNoDesc { get; set; }
        public IQueryable<SelectListItem> allFieldSections { get; set; }
        public IQueryable<SelectListItem> allFieldSubSections { get; set; }
        public IQueryable<SelectListItem> allFieldTypes { get; set; }
        public IQueryable<SelectListItem> allSuppliers { get; set; }
        public IQueryable<SelectListItem> allRuleTypes { get; set; }
        public IQueryable<SelectListItem> allUnspscClasses { get; set; }
        public IQueryable<SelectListItem> allAxisAttributes { get; set; }
        public IQueryable<SelectListItem> AllCrossSellingLinkTypes { get; set; }
        public IQueryable<SelectListItem> AllEbusinessGroups { get; set; }
        public IQueryable<SelectListItem> AllProductItemType { get; set; }
        public IQueryable<SelectListItem> AllStockItems { get; set; }
        public IQueryable<SelectListItem> AllCartridgeTypes { get; set; }

        public static IQueryable<SelectListItem> AllOverrideTypes()
        {
            SelectListViewModel model = new SelectListViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allOverrideTypes = db.overrideTypes.Select(x => new SelectListItem
                    {
                        Value = x.overrideTypeID.ToString(),
                        Text = x.overrideTypeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allOverrideTypes;
        }

        public static IQueryable<SelectListItem> AllProducts()
        {
            SelectListViewModel model = new SelectListViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allProducts = db.product.Select(x => new SelectListItem
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

            return model.allProducts;
        }

        public static IQueryable<SelectListItem> AllProductsPartNoDesc(string searchTerm = null)
        {
            SelectListViewModel model = new SelectListViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<product> query = db.product.OrderBy(x => x.partNo);
                    model.allProductsPartNoDesc = query.Select(x => new SelectListItem
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

            return model.allProductsPartNoDesc;
        }

        public class JqueryFormatted
        {
            public string label { get; set; }
            public string value { get; set; }
        }

        public static List<JqueryFormatted> GetProductsArray(string searchTerm = null, int productItemType = 0)
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
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return list;
        }

        public static IQueryable<SelectListItem> AllCategoryCodes(int? websiteID = null, bool primaryOnly = false)
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<categoryCode> list = db.categoryCode.OrderBy(x => x.categoryCodeName);

                    if (websiteID != null)
                    {
                        list = list.Where(x => x.websiteFK == websiteID);
                    }

                    if (primaryOnly)
                        list = list.Where(x => x.isPrimary == true);

                    list = list.Where(x => !x.categoryCodeName.Contains("_OLD"));

                    model.allCategoryCodes = list.Select(x => new SelectListItem
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

            return model.allCategoryCodes;
        }

        public static IQueryable<SelectListItem> AllWebsites()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allWebsites = db.Websites.OrderBy(x => x.FriendlyName).Select(x => new SelectListItem
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

            return model.allWebsites;
        }

        public static List<SelectListItem> AllManufacturers(bool showUnknown = false, bool showAll = false)
        {
            SelectListViewModel model = new SelectListViewModel();

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

                    model.allManufacturers = list.OrderBy(x => x.Text).ToList();

                    if (showAll == true)
                    {
                        model.allManufacturers.Insert(0, new SelectListItem { Text = "All", Value = "-2" });
                    }

                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allManufacturers;
        }

        public static IQueryable<SelectListItem> AllProductTypes()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allProductTypes = db.productType.Distinct().OrderBy(x => x.productTypeName).Select(x => new SelectListItem
                    {
                        Value = x.productTypeID.ToString(),
                        Text = x.productTypeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allProductTypes;
        }

        public static IQueryable<SelectListItem> AllEquipFamilies(int? manuID = null)
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<eqFamily> query = db.eqFamily.OrderBy(x => x.description);
                    
                    if (manuID != null)
                        query = query.Where(x => x.manufacturerFK == manuID);
                    
                    model.allEquipFamilies = query.Select(x => new SelectListItem
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

            return model.allEquipFamilies;
        }

        public static IQueryable<SelectListItem> AllEquipment()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allEquipment = db.eqEquipment.OrderBy(x => x.description).Select(x => new SelectListItem
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

            return model.allEquipment;
        }

        public static IQueryable<SelectListItem> AllGranularities()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allGranularities = db.qa_Granularity.OrderBy(x => x.Granularity).Select(x => new SelectListItem
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

            return model.allGranularities;
        }

        public static IQueryable<SelectListItem> AllProductStatuses()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allProductStatuses = db.productStatus.OrderBy(x => x.productStatusName).Select(x => new SelectListItem
                    {
                        Value = x.productStatusID.ToString(),
                        Text = x.productStatusName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allProductStatuses;
        }

        public static IQueryable<SelectListItem> AllProductGroups()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allProductGroups = db.productGroup.OrderBy(x => x.productGroupName).Select(x => new SelectListItem
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

            return model.allProductGroups;
        }

        public static IQueryable<SelectListItem> AllSalesAreaGroups()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allSalesAreaGroup = db.salesAreaGroup.OrderBy(x => x.salesAreaGroupName).Select(x => new SelectListItem
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

            return model.allSalesAreaGroup;
        }

        public static IQueryable<SelectListItem> AllProviders(int? providerTypeFK = null)
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<provider> list = db.provider.OrderBy(x => x.providerName);

                    if (providerTypeFK != null)
                    {
                        list = list.Where(x => x.providerTypeFK == providerTypeFK);
                    }

                    model.allProviders = list.Select(x => new SelectListItem
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

            return model.allProviders;
        }

        public static IQueryable<SelectListItem> AllProviderTypes()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allProviderTypes = db.providerType.OrderBy(x => x.providerTypeID).Select(x => new SelectListItem
                    {
                        Value = x.providerTypeID.ToString(),
                        Text = x.providerTypeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allProviderTypes;
        }

        public static IQueryable<SelectListItem> AllDataSuppliers()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allDataSuppliers = db.dataSupplier.OrderBy(x => x.dataSupplierName).Select(x => new SelectListItem
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

            return model.allDataSuppliers;
        }

        public static IQueryable<SelectListItem> AllFilterableAtts()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allFilterableAtts = db.filterableAttribute.OrderBy(x => x.attributeName).Select(x => new SelectListItem
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

            return model.allFilterableAtts;
        }

        public static IQueryable<SelectListItem> AllEquipManufacturers()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allEquipManufacturers = db.manufacturer.Where(x => x.equipmentManuName != null)
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

            return model.allEquipManufacturers;
        }

        public static IQueryable<SelectListItem> AllPMSMappingFields()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                var list = new List<SelectListItem>()
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
                    new SelectListItem() { Text = "Barcode", Value = "barcode"},
                };

                model.allPMSMappingFields = list.AsQueryable();
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.allPMSMappingFields;
        }

        public static IQueryable<SelectListItem> AllFieldSections()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allFieldSections = db.fieldSection.OrderBy(x => x.fieldSectionName).Select(x => new SelectListItem
                    {
                        Value = x.fieldSectionID.ToString(),
                        Text = x.fieldSectionName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.allFieldSections;
        }

        public static IQueryable<SelectListItem> AllFieldSubSections()
        {
            return AllFieldSubSections(null);
        }

        public static IQueryable<SelectListItem> AllFieldSubSections(int? fieldSectionID)
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldSubSection> list = null;

                    list = fieldSectionID.HasValue && fieldSectionID.Value > 0 ? db.fieldSubSection.Where(x => x.fieldSectionFK == fieldSectionID).AsQueryable() :
                        db.fieldSubSection.AsQueryable();

                    model.allFieldSubSections = list.OrderBy(x => x.fieldSubSectionName).Select(x => new SelectListItem
                    {
                        Value = x.fieldSubSectionID.ToString(),
                        Text = x.fieldSubSectionName + " - " + x.fieldSection.fieldSectionName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.allFieldSubSections;
        }

        public static IQueryable<SelectListItem> AllFieldTypes()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allFieldTypes = db.fieldType.OrderBy(x => x.fieldTypeName).Select(x => new SelectListItem
                    {
                        Value = x.fieldTypeID.ToString(),
                        Text = x.fieldTypeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.allFieldTypes;
        }

        public static IQueryable<SelectListItem> AllSuppliers()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allSuppliers = db.provider.Where(x => x.providerTypeFK == 2).OrderBy(x => x.providerName)
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

            return model.allSuppliers;
        }

        public static IQueryable<SelectListItem> AllRuleTypes()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allRuleTypes = db.priceRuleType.OrderBy(x => x.ruleName)
                        .Select(x => new SelectListItem
                        {
                            Value = x.ruleTypeID.ToString(),
                            Text = x.ruleName.ToString()
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.allRuleTypes;
        }

        public static IQueryable<SelectListItem> AllUnspscClasses()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allUnspscClasses = db.providerInventory
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

            return model.allUnspscClasses;
        }

        public static IQueryable<SelectListItem> GetAttributeDescs(int attrTypeFK, int attrNameFK)
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.allAxisAttributes = db.AxisValueLookup.OrderBy(x => x.attrValueDesc)
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

            return model.allAxisAttributes;
        }

        public static IQueryable<SelectListItem> GetCrossSellingLinkTypes()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.AllCrossSellingLinkTypes = db.crossSellingLinkType.OrderBy(x => x.crossSellingLinkTypeName)
                        .Select(x => new SelectListItem
                        {
                            Value = x.crossSellingLinkTypeID.ToString(),
                            Text = x.crossSellingLinkTypeName.ToString()
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.AllCrossSellingLinkTypes;
        }

        public static IQueryable<SelectListItem> GetAllEbusinessGroups()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.AllEbusinessGroups = db.AxisEbusiness.OrderBy(x => x.description)
                        .Select(x => new SelectListItem
                        {
                            Value = x.eBusinessRef.ToString(),
                            Text = x.eBusinessRef + " - " + x.description
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.AllEbusinessGroups;
        }

        public static IQueryable<SelectListItem> GetAllProductItemType()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.AllProductItemType = db.productItemType.OrderBy(x => x.productItemTypeName)
                        .Select(x => new SelectListItem
                        {
                            Value = x.productItemTypeID.ToString(),
                            Text = x.productItemTypeName.ToString()
                        }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.InnerException + e.Message + e.StackTrace);
            }

            return model.AllProductItemType;
        }

        public static IQueryable<SelectListItem> GetAllStockItems()
        {
            SelectListViewModel model = new SelectListViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.AllStockItems = db.product.Where(x => x.productItemTypeFK == 1)
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

            return model.AllStockItems;
        }

        public static IQueryable<SelectListItem> GetAllCartridgeTypes()
        {
            SelectListViewModel model = new SelectListViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    model.AllCartridgeTypes = db.eqCartridgeType.OrderBy(x => x.eqCartridgeTypeName).Select(x => new SelectListItem
                    {
                        Value = x.eqCartridgeTypeID.ToString(),
                        Text = x.eqCartridgeTypeName
                    }).ToList().AsQueryable();
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model.AllCartridgeTypes;
        }
    }
}
