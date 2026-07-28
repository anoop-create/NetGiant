using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DP001BusinessLogic.ViewModels
{
    public class ProductViewModel
    {
        public ProductViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public IQueryable<ProductInventory> InventoryList { get; set; }
        public List<ProductInventory> SearchResults { get; set; }
        public List<PriceHistory> PriceHistory { get; set; }
        public List<CustomField> CustomFieldList { get; set; }
        public List<CustomField> CustomAdjustmentList { get; set; }
        public Channel Channel { get; set; }
        public ProductInventory ProductEntry { get; set; }
        public int SuppplierSuggestions { get; set; }
        public int CompetitorSuggestions { get; set; }
        public ProductInventory VariantOf { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        public ProductViewModel GetInventory()
        {
            var crud = new CrudProductInventory();
            InventoryList = crud.ReadProductsQuery(x => x.ChannelFK == _channelId && x.Lookup1.LookupName == "Active", _ctx);

            return this;
        }

        public ProductViewModel Search(string term, int brandFK)
        {
            var crud = new CrudProductInventory();
            if (brandFK > 0)
            {
                SearchResults = crud.Read(x =>
                    x.ChannelFK == _channelId &&
                    (x.ManufacturerPartNo.Contains(term) ||
                    x.Description.Contains(term)) &&
                    x.BrandFK == brandFK, 20);
            }
            else
            {
                SearchResults = crud.Read(x =>
                    x.ChannelFK == _channelId &&
                    (x.ManufacturerPartNo.Contains(term) ||
                    x.Description.Contains(term)), 20);
            }

            return this;
        }

        public int ProductInventoryCount()
        {
            var crud = new CrudProductInventory();
            return crud.ReadCount(_channelId);
        }

        public ProductViewModel GetPriceHistory(int id, bool addCurrentPrice = false)
        {
            var prodCrud = new CrudProductInventory();
            var prod = prodCrud.Read(x => x.ProductInventoryID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (prod != null)
            {

                var clientProductID = prod.ClientProductID;

                if (!string.IsNullOrEmpty(clientProductID))
                {
                    var priceHistoryCrud = new CrudPriceHistory();
                    PriceHistory = priceHistoryCrud.Read(x => x.ChannelFK == _channelId && x.ClientProductID == clientProductID);
                }
                else
                {
                    PriceHistory = new List<PriceHistory>();
                }

                if (addCurrentPrice)
                {
                    PriceHistory.Add(new PriceHistory()
                    {
                        Price = prod.Price,
                        ChannelFK = prod.ChannelFK,
                        ClientProductID = clientProductID,
                        Date = prod.DatePriceChanged ?? new DateTime()
                    });
                }
            }
            else
            {
                throw new ApplicationException("Product not found or you do not have permission to view it's prices");
            }

            return this;
        }

        public Stream CreateExportFile()
        {
            var data = InventoryList.Select(x => new
            {
                Part_Number = x.ManufacturerPartNo,
                Description = x.Description,
                Brand = x.Brand.BrandName,
                Category = x.ProductCategory.CategoryName,
                Price = x.Price.ToString()
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public ProductViewModel GetProductDetail(int productInventoryID)
        {
            var crudProduct = new CrudProductInventory();
            ProductEntry = crudProduct.Read(x => x.ProductInventoryID == productInventoryID && x.ChannelFK == _channelId).FirstOrDefault();

            if (ProductEntry != null)
            {
                GetPriceHistory(productInventoryID, true);

                if (ProductEntry.VariantOf != null)
                {
                    VariantOf = crudProduct.Read(x => x.ClientProductID == ProductEntry.VariantOf && x.ChannelFK == _channelId).FirstOrDefault();
                }

                var crudSkuMap = new CrudSkuMapping();

                if (ProductEntry.SupplierInventories.Count == 0)
                    SuppplierSuggestions = crudSkuMap.GetSuggestedSupplierMappings(ProductEntry).Count;

                if (ProductEntry.CompetitorInventories.Count == 0)
                    CompetitorSuggestions = crudSkuMap.GetSuggestedCompetitorMappings(ProductEntry).Count;
            }
            else
            {
                throw new ApplicationException("Product not found or you do not have permission to view it");
            }

            return this;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
