using MoreLinq;
using netGiant.Intranet.DataLayer;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class PriceologyFeeds
    {
        public PriceologyFeeds(Dictionary<string, string> parms)
        {
            _suppliedParms = parms;
            _stnFunc = new StandardFunctions();
        }

        private static Dictionary<string, string> _suppliedParms;
        private readonly StandardFunctions _stnFunc;

        public void Generate()
        {
            _stnFunc.AddToActivityLog(_suppliedParms["type"] + " Started");

            try
            {
                GenerateProductFeeds();
                GenerateSupplierFeeds();
                GenerateAssemblySupplierFeeds();
                GenerateCompetitorFeeds();
                GenerateSkuuudleLiteSummary();
            }
            catch (Exception ex)
            {
                _stnFunc.AddToActivityLog("Error: " + ex.Message);
                _stnFunc.AddToActivityLog("Stack: " + ex.StackTrace);
            }

            _stnFunc.AddToActivityLog(_suppliedParms["type"] + " Ended");
            _stnFunc.LogActivity(_suppliedParms["type"]);
        }

        private static void GenerateProductFeeds()
        {
            var websitesList = StandardFunctions.GetAllWebsites();

            foreach (var website in websitesList)
            {
                var data = GetProductFeedData(website.WebsiteID);
                var filePath = $"{_suppliedParms["output"]}Products_{website.FriendlyName}.txt".Replace("\"", "\\");

                using (var writer = new CsvFileWriter(filePath, '\t'))
                {
                    var firstRow = new CsvRow
                    {
                        "ClientProductID",
                        "MFPN",
                        "Brand",
                        "ProductDescription",
                        "ProductCategory",
                        "BasePrice",
                        "SalesFrequency",
                        "RelatedProductMfpn",
                        "RelatedProductManufacturer",
                        "KeyLineGroup",
                        "KeyLine"
                    };
                    writer.WriteRow(firstRow);

                    foreach (var record in data)
                    {
                        var newRow = new CsvRow
                        {
                            record.ClientProductID.ToString(),
                            record.Mfpn,
                            record.Brand,
                            record.ProductDescription,
                            record.ProductCategory,
                            record.BasePrice.ToString(),
                            record.SalesFrequency.ToString(),
                            record.RelatedProductMfpn,
                            record.RelatedProductManufacturer,
                            record.KeyLineGroup,
                            record.KeyLine.ToString()
                        };
                        writer.WriteRow(newRow);
                    }
                }
            }
        }

        private static void GenerateSupplierFeeds()
        {
            var supplierFeeds = StandardFunctions.GetProviderList(x => x.providerTypeFK == 2);
            var websitesList = StandardFunctions.GetAllWebsites();

            foreach (var website in websitesList)
            {
                foreach (var supplier in supplierFeeds)
                {
                    var supplierData = GetSupplierFeedData(website.WebsiteID, supplier.providerID);
                    var filePath =
                        $"{_suppliedParms["output"]}{GetWebsiteAbbreviation(website.WebsiteID)}_{supplier.providerName.Replace(" ", "_")}.txt"
                            .Replace("\"", "\\");

                    using (var writer = new CsvFileWriter(filePath, '\t'))
                    {
                        var firstRow = new CsvRow
                        {
                            "ClientProductID",
                            "MFPN",
                            "Brand",
                            "ProductDescription",
                            "StockQuantity",
                            "Price"
                        };
                        writer.WriteRow(firstRow);

                        foreach (var record in supplierData)
                        {
                            var newRow = new CsvRow
                            {
                                record.ClientProductID.ToString(),
                                record.MFPN,
                                record.BrandName,
                                record.Description,
                                record.Stock.ToString(),
                                record.Price.ToString()
                            };
                            writer.WriteRow(newRow);
                        }
                    }
                }
            }
        }

        private static void GenerateAssemblySupplierFeeds()
        {
            var websitesList = StandardFunctions.GetAllWebsites();

            foreach (var website in websitesList)
            {
                var supplierData = GetAssemblySupplierFeedData(website.WebsiteID);
                var filePath =
                    $"{_suppliedParms["output"]}{GetWebsiteAbbreviation(website.WebsiteID)}_Assemblies.txt".Replace("\"", "\\");

                using (var writer = new CsvFileWriter(filePath, '\t'))
                {
                    var firstRow = new CsvRow
                    {
                        "ClientProductID",
                        "MFPN",
                        "Brand",
                        "ProductDescription",
                        "StockQuantity",
                        "BasePrice"
                    };
                    writer.WriteRow(firstRow);

                    foreach (var record in supplierData)
                    {
                        var newRow = new CsvRow
                        {
                            record.ClientProductID.ToString(),
                            record.MFPN,
                            record.BrandName,
                            record.Description,
                            record.Stock.ToString(),
                            record.CostPrice.ToString(CultureInfo.InvariantCulture)
                        };
                        writer.WriteRow(newRow);
                    }
                }
            }
        }

        private static void GenerateCompetitorFeeds()
        {
            var competitorTypes = new Dictionary<string, int>
            {
                { "SkuuudlePro", 1 },
                { "SkuuudleLite", 5 }
            };

            var websitesList = StandardFunctions.GetAllWebsites();

            foreach (var website in websitesList)
            {
                foreach (var competitorType in competitorTypes)
                {
                    var competitorData = GetCompetitorFeedData(website.WebsiteID, competitorType.Value);
                    var filePath =
                        $"{_suppliedParms["output"]}Competitors_{website.FriendlyName}_{competitorType.Key}.txt".Replace("\"", "\\");

                    using (var writer = new CsvFileWriter(filePath, '\t'))
                    {
                        var firstRow = new CsvRow
                        {
                            "ClientProductID",
                            "CompetitorName",
                            "MFPN",
                            "Brand",
                            "ProductDescription",
                            "Price"
                        };
                        writer.WriteRow(firstRow);

                        foreach (var record in competitorData)
                        {
                            var newRow = new CsvRow
                            {
                                record.ClientProductID.ToString(),
                                record.CompetitorName,
                                record.MFPN,
                                record.BrandName,
                                record.Description,
                                record.Price.ToString()
                            };
                            writer.WriteRow(newRow);
                        }
                    }
                }
            }
        }

        private static void GenerateSkuuudleLiteSummary()
        {
            var skuuudleCompetitors = StandardFunctions.GetProviderList(x => x.providerTypeFK == 5).DistinctBy(x => x.providerName);
            var filePath = $"{_suppliedParms["output"]}Skuuudle_Lite_Summary.txt".Replace("\"", "\\");

            using (var writer = new CsvFileWriter(filePath, '\t'))
            {
                var firstRow = new CsvRow {"Competitor name", "Average rating", "No. of reviews (qty)"};
                writer.WriteRow(firstRow);

                foreach (var record in skuuudleCompetitors)
                {
                    var newRow = new CsvRow
                    {
                        record.providerName,
                        record.reviewRating.IsNull(0).ToString(CultureInfo.InvariantCulture),
                        record.reviewTotal.IsNull(0).ToString()
                    };
                    writer.WriteRow(newRow);
                }
            }
        }

        private static IEnumerable<GetPriceologyProductFeed_Result> GetProductFeedData(int websiteId)
        {
            using (var db = new ngmdEntities())
            {
                return db.GetPriceologyProductFeed(websiteId).ToList();
            }
        }

        private static IEnumerable<GetPriceologySupplierFeed_Result> GetSupplierFeedData(int websiteId, int supplierId)
        {
            using (var db = new ngmdEntities())
            {
                return db.GetPriceologySupplierFeed(websiteId, supplierId).ToList();
            }
        }

        private static IEnumerable<GetPriceologyAssemblySupplierFeed_Result> GetAssemblySupplierFeedData(int websiteId)
        {
            using (var db = new ngmdEntities())
            {
                return db.GetPriceologyAssemblySupplierFeed(websiteId).ToList();
            }
        }

        private static IEnumerable<GetPriceologyCompetitorFeed_Result> GetCompetitorFeedData(int websiteId, int competitorTypeId)
        {
            using (var db = new ngmdEntities())
            {
                return db.GetPriceologyCompetitorFeed(websiteId, competitorTypeId).ToList();
            }
        }

        private static string GetWebsiteAbbreviation(int websiteId)
        {
            switch (websiteId)
            {
                case 1:
                    return "TG";
                case 2:
                    return "CM";
                case 3:
                    return "NG";
                default:
                    return "";
            }
        }
    }
}
