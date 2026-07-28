using Nest;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;

namespace ngBatchProcesses.BusinessObjects.Searching
{
    public class ElasticSearch
    {
        public ElasticSearch()
        {
            Results = new List<SearchEntry>();
            LookupExists = new HashSet<string>();
            Node = new Uri(EntityFunctions.GetConfigurationSettings(x => x.settingName == "ElasticsearchUri").FirstOrDefault().settingValue);
            Settings = new ConnectionSettings(Node);
            Client = new ElasticClient(Settings);
            DefaultIndexName = EntityFunctions.GetConfigurationSettings(x => x.settingName == "ElasticSearchIndexName").FirstOrDefault().settingValue;
        }

        private List<SearchEntry> Results { get; set; }
        private HashSet<string> LookupExists { get; set; }
        private Uri Node { get; set; }
        private ConnectionSettings Settings { get; set; }
        private ElasticClient Client { get; set; }
        private string DefaultIndexName { get; set; }

        private bool _errorOccurred;
        
        public void Build()
        {
            StandardFunctions.WriteProcessStarted();

            try
            {
                List<Website> websites = EntityFunctions.GetWebsiteList(x => x.WebsiteID < 4);
                using (var db = new ngmdEntities())
                {
                    foreach (var website in websites)
                    {
                        string vn = EntityFunctions.GetConfigurationSetting("Website Application Variables", "VersionNumber", website.WebsiteID);
                        var useHttps = GetHttpsSetting(website.WebsiteID);
                        //var dbProducts = EntityFunctions.GetProduct(x => (x.productStatusFK == 1 || x.productStatusFK == 8) && x.websiteInventory.Any(y => y.websiteFK == website.WebsiteID));
                        var dbProducts = db.product
                            .Include(x => x.crossSellingLink)
                            .Include(x => x.websiteInventory)
                            .Include(x => x.manufacturer)
                            .Where(x => (x.productStatusFK == 1 || x.productStatusFK == 8) && x.websiteInventory.Any(y => y.websiteFK == website.WebsiteID))
                            .ToList();

                        var dbEquipment = db.eqEquipment
                            .Include("eqProductMembership.product")
                            .Where(x => x.statusFK == 1)
                            .ToList();

                        var dbCatgeories = new List<categoryCode>();

                        foreach (var item in dbProducts)
                        {
                            var productImage = ProductFunctions.GetProductImage(website, item, vn, "S");
                            var productUrl = "/product/" + StandardFunctions.CleanupURL(item.productName + "-" + item.partNo + "-" + item.AxisFields.stockReference);

                            var crossSellItem = item.crossSellingLink.FirstOrDefault(x => x.crossSellingLinkTypeFK == 1);
                            if (crossSellItem != null)
                            {
                                var crossSellProd = crossSellItem.product1;
                                var afa = item.AxisFields.AxisFieldsAdditional.FirstOrDefault(x => x.websiteFK == website.WebsiteID);

                                //if (item.productID == 33678)
                                //{
                                //    Console.WriteLine("Stop");
                                //}
                                AddToList(new SearchEntry
                                {
                                    Model = item.partNo.FormatForIndex(),
                                    Description = item.productName.FormatForIndex(),
                                    CrossSellModel = crossSellProd.partNo.FormatForIndex(),
                                    CrossSellDescription = crossSellProd.productName.FormatForIndex(),
                                    CrossSellManufacturer = crossSellProd.manufacturer.manufacturerName.FormatForIndex(),
                                    FriendlyModel = item.partNo,
                                    FriendlyDescription = item.productName,
                                    ItemType = "Product",
                                    ItemId = item.productID,
                                    ManufacturerName = item.manufacturer.manufacturerName.FormatForIndex(),
                                    ImageUrl = productImage,
                                    ProductUrl = productUrl,
                                    ProductType = (db.Lookup
                                            .Where(y => y.LookupType.LookupTypeName == "ProductType" && y.AltLookupId == item.productItemTypeFK)
                                            .AsQueryable()
                                            .FirstOrDefault()
                                            .LookupName),
                                    MetaKeywords = afa != null ? afa.metaKeywords : ""
                                });
                            }
                            var crossSellItem1 = item.crossSellingLink1.FirstOrDefault(x => x.crossSellingLinkTypeFK == 1);
                            if (crossSellItem1 != null)
                            {
                                var crossSellProd = crossSellItem1.product;
                                var afa = item.AxisFields.AxisFieldsAdditional.FirstOrDefault(x => x.websiteFK == website.WebsiteID);

                                AddToList(new SearchEntry
                                {
                                    Model = item.partNo.FormatForIndex(),
                                    Description = item.productName.FormatForIndex(),
                                    CrossSellModel = crossSellProd.partNo.FormatForIndex(),
                                    CrossSellDescription = crossSellProd.productName.FormatForIndex(),
                                    CrossSellManufacturer = crossSellProd.manufacturer.manufacturerName.FormatForIndex(),
                                    FriendlyModel = item.partNo,
                                    FriendlyDescription = item.productName,
                                    ItemType = "Product",
                                    ItemId = item.productID,
                                    ManufacturerName = item.manufacturer.manufacturerName.FormatForIndex(),
                                    ImageUrl = productImage,
                                    ProductUrl = productUrl,
                                    ProductType = (db.Lookup
                                            .Where(y => y.LookupType.LookupTypeName == "ProductType" && y.AltLookupId == item.productItemTypeFK)
                                            .AsQueryable()
                                            .FirstOrDefault()
                                            .LookupName),
                                    MetaKeywords = afa != null ? afa.metaKeywords : ""
                                });
                            }

                            if (crossSellItem == null && crossSellItem1 == null)
                            {
                                AddToList(new SearchEntry
                                {
                                    Model = item.partNo.FormatForIndex(),
                                    Description = item.productName.FormatForIndex(),
                                    FriendlyModel = item.partNo,
                                    FriendlyDescription = item.productName,
                                    ItemType = "Product",
                                    ItemId = item.productID,
                                    ManufacturerName = item.manufacturer.manufacturerName.FormatForIndex(),
                                    ImageUrl = productImage,
                                    ProductUrl = productUrl,
                                    ProductType = (db.Lookup
                                            .Where(y => y.LookupType.LookupTypeName == "ProductType" && y.AltLookupId == item.productItemTypeFK)
                                            .AsQueryable()
                                            .FirstOrDefault()
                                            .LookupName),
                                });
                            }
                        }

                        List<Lookup> lct = EntityFunctions.GetNgmdLookup(x => x.LookupType.LookupTypeName == "CartridgeType");
                        foreach (var item in dbEquipment)
                        {
                            var eqipImage = ProductFunctions.GenerateImageURL(website, item.thumbnailURL, vn, "S");
                            AddToList(new SearchEntry
                            {
                                Model = item.description.FormatForIndex(),
                                Description = item.description.FormatForIndex(),
                                FriendlyModel = item.description,
                                FriendlyDescription = "Equipment",
                                ItemType = "Equipment",
                                ItemId = item.eqEquipmentID,
                                ImageUrl = eqipImage,
                                CartridgeType = lct.Find(x => x.AltLookupId == item.eqCartridgeTypeFK).LookupName,
                                ManufacturerId = item.manufacturerFK,
                                ManufacturerName = item.manufacturer.manufacturerName.FormatForIndex(),
                                ProductCount = item.eqProductMembership.Count(x => x.product.productStatusFK == 1 || x.product.productStatusFK == 8),
                                MetaKeywords = item.metaKeywords
                            });
                        }

                        // Categories
                        var validCategories = new List<categoryCode>();
                        if (website.WebsiteID == 3)
                        {
                            dbCatgeories = db.categoryCode
                                .Include("websiteInventory.product")
                                .Include("secondaryCategoryLookup.websiteInventory.product")
                                .Where(x => x.websiteFK == 3 && !x.categoryCodeName.Contains("_OLD"))
                                .ToList();

                            foreach (var category in dbCatgeories)
                            {
                                if (category.AXISGroupNo == null) continue;
                                if (!CategoryHasProducts(category)) continue;

                                int.TryParse(category.AXISGroupNo, out int axisGroupNo);

                                AddToList(new SearchEntry
                                {
                                    Model = category.categoryCodeName.FormatForIndex(),
                                    Description = category.categoryCodeName.FormatForIndex(),
                                    FriendlyModel = category.categoryCodeName,
                                    FriendlyDescription = "Category",
                                    ItemType = "Category",
                                    ItemId = axisGroupNo,
                                    ProductCount = category.websiteInventory.Count + category.secondaryCategoryLookup.Count
                                });

                                validCategories.Add(category);
                            }
                        }

                        var response = Client.Bulk(GetBulkDescriptor(website.WebsiteName));
                        _errorOccurred = !_errorOccurred ? !response.IsValid : _errorOccurred;

                        Client.DeleteByQuery<SearchEntry>(q => q
                            .Index(DefaultIndexName + website.WebsiteName)
                            .Query(rq => rq
                                .Bool(b => b
                                    .MustNot(
                                        bs => bs.Ids(x => x.Values(dbProducts.Select(y => y.partNo)))
                                    )
                                    .Must(
                                        bs => bs.Match(x => x.Field(y => y.ItemType).Query("Product"))
                                    )
                                )
                            )
                        );

                        Client.DeleteByQuery<SearchEntry>(q => q
                            .Index(DefaultIndexName + website.WebsiteName)
                            .Query(rq => rq
                                .Bool(b => b
                                    .MustNot(
                                        bs => bs.Ids(x => x.Values(dbEquipment.Select(y => y.description)))
                                    )
                                    .Must(
                                        bs => bs.Match(x => x.Field(y => y.ItemType).Query("Equipment"))
                                    )
                                )
                            )
                        );

                        if (website.WebsiteID == 3)
                        {
                            Client.DeleteByQuery<SearchEntry>(q => q
                                .Index(DefaultIndexName + website.WebsiteName)
                                .Query(rq => rq
                                    .Bool(b => b
                                        .MustNot(
                                            bs => bs.Ids(x => x.Values(validCategories.Select(y => y.categoryCodeName)))
                                        )
                                        .Must(
                                            bs => bs.Match(x => x.Field(y => y.ItemType).Query("Category"))
                                        )
                                    )
                                )
                            );
                        }

                        Results.Clear();
                        LookupExists.Clear();

                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Elastic index built for website " + website.WebsiteName });
                    }
                }

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteException(ex);
                _errorOccurred = true;
            }
        }

        private void AddToList(SearchEntry searchItem)
        {
            if (!LookupExists.Contains(searchItem.Model))
            {
                Results.Add(searchItem);
                LookupExists.Add(searchItem.Model);
            }
        }

        private BulkDescriptor GetBulkDescriptor(string websiteName)
        {
            var descriptor = new BulkDescriptor();
            descriptor.Index(DefaultIndexName + websiteName);

            foreach (var searchItem in Results)
            {
                descriptor.Update<SearchEntry>(o => o.Doc(searchItem).Upsert(searchItem).Id(searchItem.FriendlyModel));
            }

            return descriptor;
        }

        private static bool CategoryHasProducts(categoryCode cc)
        {
            bool returnValue = false;
            var cnt1 = cc.websiteInventory.Count(x => x.product.productStatusFK == 1);
            if (cnt1 > 0)
            {
                returnValue = true;
            }
            var cnt2 = cc.secondaryCategoryLookup.Count(x => x.websiteInventory.product.productStatusFK == 1);
            if (cnt2 > 0)
            {
                returnValue = true;
            }

            return returnValue;
        }

        private class SearchEntry
        {
            public string Model { get; set; }
            public string Description { get; set; }
            public string CrossSellModel { get; set; }
            public string CrossSellDescription { get; set; }
            public string CrossSellManufacturer { get; set; }
            public string FriendlyModel { get; set; }
            public string FriendlyDescription { get; set; }
            public string ItemType { get; set; }
            public int ItemId { get; set; }
            public string ImageUrl { get; set; }
            public string CartridgeType { get; set; }
            public string ManufacturerName { get; set; }
            public int ManufacturerId { get; set; }
            public int ProductCount { get; set; }
            public string ProductUrl { get; set; }
            public string ProductType { get; set; }
            public string MetaKeywords { get; set; }
        }

        private static bool GetHttpsSetting(int websiteId)
        {
            return Convert.ToBoolean(EntityFunctions.GetConfigurationSetting("Website Application Variables", "UseHTTPS", websiteId));
        }
    }

    public static class StringExtensions
    {
        public static string FormatForIndex(this string value)
        {
            return value.Replace("-", "").ToLower().Trim();
        }
    }
}
