using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using MoreLinq;
using System.Linq.Expressions;
using DP001DataAccess.Entities;

namespace DP001BusinessLogic.ViewModels
{
    public class SharedViewModel
    {
        public static List<SelectListItem> GetLookupList(string lookupType)
        {
            var crudLookup = new CrudLookup();
            var fileTypes = crudLookup.Read(x => x.LookupType.LookupTypeName == lookupType);

            return fileTypes.Select(x => new SelectListItem
            {
                Text = x.LookupName,
                Value = x.LookupID.ToString()
            }).ToList();
        }
        public static List<SelectListItem> GetCategoryList(int channelId, int brandFK = 0)
        {
            var crudCategory = new CrudProductCategory();
            List<ProductCategory> categories = new List<ProductCategory>();
            if (brandFK > 0)
            {
                categories = crudCategory.Read(x => x.ChannelFK == channelId && x.MapBrandCategories.Any(y => y.BrandFK == brandFK));
            }
            else
            {
                categories = crudCategory.Read(x => x.ChannelFK == channelId);
            }

            return categories.Select(x => new SelectListItem
            {
                Text = x.CategoryName,
                Value = x.ProductCategoryID.ToString()
            }).ToList();
        }

        public static List<SelectListItem> GetBrandList(int channelId, bool productTableOnly = false, bool emptyItem = false)
        {
            var crudBrand = new CrudBrand();
            List<Brand> brands;

            if (productTableOnly)
            {
                brands = crudBrand.Read(x => x.ChannelFK == channelId && x.ProductInventories.Count > 0);
            }
            else
            {
                brands = crudBrand.Read(x => x.ChannelFK == channelId);
            }

            var selectList = brands.Select(x => new SelectListItem
            {
                Text = x.BrandName,
                Value = x.BrandID.ToString()
            })
            .OrderBy(x => x.Text)
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        public static List<SelectListItem> GetSuppBrandList(int channelId, bool productTableOnly = false, bool emptyItem = false)
        {
            CrudSupplierInventory crud = new CrudSupplierInventory();
            List<string> brands;

            brands = crud.ReadSuppBrands(channelId);

            var selectList = brands.Select(x => new SelectListItem
            {
                Text = x,
                Value = x
            })
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        public static List<SelectListItem> GetCompetitorList(int channelId, bool emptyItem = false)
        {
            var crudCompetitor = new CrudCompetitor();
            List<Competitor> competitor;

            competitor = crudCompetitor.Read(x => x.ChannelFK == channelId);

            var selectList = competitor.Select(x => new SelectListItem
            {
                Text = x.CompetitorName,
                Value = x.CompetitorID.ToString()
            })
            .OrderBy(x => x.Text)
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        public static List<SelectListItem> GetSupplierList(int channelId, bool emptyItem = false)
        {
            var crudSupplier = new CrudSupplier();
            List<Supplier> suppliers;

            suppliers = crudSupplier.Read(x => x.ChannelFK == channelId);

            var selectList = suppliers.Select(x => new SelectListItem
            {
                Text = x.SupplierName,
                Value = x.SupplierID.ToString()
            })
            .OrderBy(x => x.Text)
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        public static List<SelectListItem> GetRuleTypeList(int channelId, bool emptyItem = false)
        {
            var crudPriceRule = new CrudPriceRule();
            var lookups = crudPriceRule.Read(x => x.ChannelFK == channelId).DistinctBy(y => y.RuleTypeFK);
            var selectList = lookups.Select(x => new SelectListItem
            {
                Text = x.Lookup1.LookupName,
                Value = x.Lookup1.LookupID.ToString()
            })
            .OrderBy(x => x.Text)
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        public static List<SelectListItem> GetMethodList(int channelId, bool emptyItem = false)
        {
            var crudPriceRule = new CrudPriceRule();
            var lookups = crudPriceRule.Read(x => x.ChannelFK == channelId).DistinctBy(y => y.MethodFK);
            var selectList = lookups.Select(x => new SelectListItem
            {
                Text = x.Lookup.LookupName,
                Value = x.Lookup.LookupID.ToString()
            })
            .OrderBy(x => x.Text)
            .ToList();

            if (emptyItem)
                selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }

        //public static List<SelectListItem> GetProductList(int tenantFK)
        //{
        //    var crudProduct = new CrudProductInventory();
        //    var products = crudProduct.Read(x => x.TenantFK == tenantFK);

        //    return products.Select(x => new SelectListItem
        //    {
        //        Text = x.Description,
        //        Value = x.ProductInventoryID.ToString()
        //    }).ToList();
        //}

        public static List<SelectListItem> GetChannelList(int tenantFK)
        {
            var crudChannel = new CrudChannel();
            var channels = crudChannel.Read(x => x.TenantFK == tenantFK);

            return channels.Select(x => new SelectListItem
            {
                Text = x.ChannelName,
                Value = x.ChannelID.ToString()
            }).ToList();
        }

        public static List<SelectListItem> GetAuditActionList()
        {
            var selectList = new List<SelectListItem>()
            {
                new SelectListItem { Text = "Please Select...", Value = "" },
                new SelectListItem { Text = "Add", Value = "A" },
                new SelectListItem { Text = "Change", Value = "C" },
                new SelectListItem { Text = "Delete", Value = "D" }
            };

            return selectList;
        }

        public static List<SelectListItem> GetCalculationOutcomeList(int channelId, bool productTableOnly = false)
        {
            var crudLookup = new CrudLookup();
            List<Lookup> outcomes;

            if (productTableOnly)
            {
                outcomes = crudLookup.Read(x => x.LookupType.LookupTypeName == "CalculationOutcome" &&
                    x.ProductInventories.Any(y => y.ChannelFK == channelId));
            }
            else
            {
                outcomes = crudLookup.Read(x => x.LookupType.LookupTypeName == "CalculationOutcome");
            }

            return outcomes.Select(x => new SelectListItem
            {
                Text = x.LookupName,
                Value = x.LookupID.ToString()
            }).ToList();
        }
        public static List<SelectListItem> GetRuleNameList(int channelId, bool productTableOnly = false)
        {
            var crudPriceRule = new CrudPriceRule();
            List<PriceRule> priceRuleNames;

            if (productTableOnly)
            {
                priceRuleNames = crudPriceRule.Read(x => x.ChannelFK == channelId && x.ProductInventories.Count > 0);
            }
            else
            {
                priceRuleNames = crudPriceRule.Read(x => x.ChannelFK == channelId);
            }

            var selectList = priceRuleNames
               .DistinctBy(x => x.RuleName)
               .Select(x => new SelectListItem
               {
                   Text = x.RuleName,
                   Value = x.PriceRuleID.ToString()
               }).ToList();

            //selectList.Insert(0, new SelectListItem() { Text = "Please Select...", Value = "" });

            return selectList;
        }
    }
}
