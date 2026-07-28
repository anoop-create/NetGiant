using PagedList;
using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class SkuMappingViewModel
    {
        public SkuMappingViewModel(int channelId, TenantSetting settings)
        {
            _channelId = channelId;
            _tenantSettings = settings;
            _ctx = new DP001Entities();
        }
        public SkuMappingViewModel()
        {

        }

        public IQueryable<TelerikSkuMappings> SkuMappingList { get; set; }
        public int ProductInventoryCount { get; set; }
        public SkuMappingsDisplayModel SkuMappingEntry { get; set; }
        public IQueryable<TelerikExceptions> SupplierExceptions { get; set; }
        public IQueryable<TelerikExceptions> CompetitorExceptions { get; set; }
        public ProductInventory SupplierException { get; set; }
        public ProductInventory CompetitorException { get; set; }
        public List<SupplierInventory> SuggestedSupplierMappings { get; set; }
        public List<CompetitorInventory> SuggestedCompetitorMappings { get; set; }
        public SkuMappingEditModel SkuMappingRecord { get; set; }
        public List<SelectListItem> Brands { get; set; }
        public List<SelectListItem> FileTypes { get; set; }
        public int SupplierInventoryFK { get; set; }
        public int CompetitorInventoryFK { get; set; }
        public int SupplierExceptionCount { get; set; }
        public int CompetitorExceptionCount { get; set; }

        private TenantSetting _tenantSettings;
        private int _channelId;
        private DP001Entities _ctx;

        public SkuMappingViewModel InitializeReport()
        {
            var crudProd = new CrudProductInventory();
            ProductInventoryCount = crudProd.ReadCount(_channelId);
            SupplierExceptionCount = crudProd.ReadSupplierExceptionCount(_channelId, _tenantSettings.SupplierMatchLimit);
            CompetitorExceptionCount = crudProd.ReadCompetitorExceptionCount(_channelId, _tenantSettings.SupplierMatchLimit);

            return this;
        }

        public SkuMappingViewModel Get()
        {
            var crud = new CrudSkuMapping();
            SkuMappingList = crud.ReadMappingsQuery(_channelId, _ctx).AsTelerikSkuMappingsViewModel();
            
            return this;
        }

        public SkuMappingViewModel EditSkuMapping(int id)
        {
            var crud = new CrudSkuMapping();
            SkuMappingRecord = crud.ReadSkuMap(id, _channelId);
            Brands = SharedViewModel.GetBrandList(_channelId);
            FileTypes = SharedViewModel.GetLookupList("FileType");

            return this;
        }

        public SkuMappingViewModel GetSupplierExceptions()
        {
            var crud = new CrudSkuMapping();
            SupplierExceptions = crud.ReadProductSupplierExceptionsQuery(_channelId, _ctx, _tenantSettings.SupplierMatchLimit).AsTelerikExceptionsViewModel();

            return this;
        }

        public SkuMappingViewModel GetCompetitorExceptions()
        {
            var crud = new CrudSkuMapping();
            CompetitorExceptions = crud.ReadProductCompetitorExceptionsQuery(_channelId, _ctx, _tenantSettings.SupplierMatchLimit).AsTelerikExceptionsViewModel();

            return this;
        }

        public SkuMappingViewModel Edit(int id)
        {
            var crud = new CrudSkuMapping();
            SkuMappingEntry = crud.Read(id);

            return this;
        }

        public SaveReturn Delete(int id, int invId)
        {
            var sr = new SaveReturn();

            try
            {
                var crud = new CrudSkuMapping();

                var deleteRecord = crud.ReadSingle(id);

                if (deleteRecord != null)
                {
                    var crudLookup = new CrudLookup();
                    string fileTypeName = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupID == deleteRecord.FileTypeFK).FirstOrDefault().LookupName;

                    if (fileTypeName == "Supplier Inventory")
                    {
                        var crudInventory = new CrudSupplierInventory();
                        var inventoryRecord = crudInventory.ReadOnly(x => x.SupplierInventoryID == invId && x.ChannelFK == _channelId).FirstOrDefault();
                        inventoryRecord.ProductInventoryFK = null;
                        inventoryRecord.ProductInventory = null;
                        crudInventory.Update(inventoryRecord);
                    }
                    else
                    {
                        var crudInventory = new CrudCompetitorInventory();
                        var inventoryRecord = crudInventory.Read(x => x.CompetitorInventoryID == invId && x.ChannelFK == _channelId).FirstOrDefault();
                        inventoryRecord.ProductInventoryFK = null;
                        inventoryRecord.ProductInventory = null;
                        crudInventory.Update(inventoryRecord);
                    }

                    crud.Delete(deleteRecord);
                }

                sr.IsSuccess = true;
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public SkuMappingViewModel EditSupplierException(int id)
        {
            var crudProduct = new CrudProductInventory();
            var crudSku = new CrudSkuMapping();
            SupplierException = crudProduct.Read(x => x.ProductInventoryID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (SupplierException != null)
            {
                SuggestedSupplierMappings = crudSku.GetSuggestedSupplierMappings(SupplierException);
            }

            return this;
        }

        public SkuMappingViewModel EditCompetitorException(int id)
        {
            var crudProduct = new CrudProductInventory();
            var crudSku = new CrudSkuMapping();
            CompetitorException = crudProduct.Read(x => x.ProductInventoryID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (CompetitorException != null)
            {
                SuggestedCompetitorMappings = crudSku.GetSuggestedCompetitorMappings(CompetitorException);
            }

            return this;
        }

        public SaveReturn UpdateSupplierMappings(List<SupplierInventory> mappings, ProductInventory product, int channelId)
        {
            var saveReturn = new SaveReturn();
            var crudProduct = new CrudProductInventory();

            var hasPermission = crudProduct.Read(x => x.ProductInventoryID == product.ProductInventoryID &&
                x.ChannelFK == channelId).Count > 0;

            if (hasPermission)
            {
                try
                {
                    SuggestedSupplierMappings.ForEach(x => x.ChannelFK = channelId);

                    var crud = new CrudSupplierInventory();
                    var crudSkuMapping = new CrudSkuMapping();
                    crud.UpdateList(mappings);

                    var crudLookup = new CrudLookup();
                    int fileTypeID = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Supplier Inventory").FirstOrDefault().LookupID;

                    foreach (var map in mappings)
                    {
                        var skuMap = new SKUMapping()
                        {
                            SKUMapFrom = map.ManufacturerPartNo,
                            SKUMapTo = product.ManufacturerPartNo,
                            BrandFK = map.BrandFK,
                            FileTypeFK = fileTypeID,
                            ChannelFK = map.ChannelFK,
                            InventoryFK = map.SupplierFK
                        };

                        crudSkuMapping.Create(skuMap);

                        if (map.ProductInventoryFK > 0)
                        {
                            var crudProd = new CrudProductInventory();
                            var prod = crudProd.Read(map.ProductInventoryFK ?? 0);
                            if (prod.SupplierCount != null)
                            {
                                prod.SupplierCount++;
                            }
                            else
                            {
                                prod.SupplierCount = 1;
                            }

                            crudProd.Update(prod);
                        }
                    }

                    saveReturn.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    saveReturn.IsSuccess = false;
                    saveReturn.Message = ex.Message;
                    saveReturn.InnerException = ex.InnerException != null ? ex.InnerException.Message : "";
                }
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Product not found or you do not have permission to change it";
            }

            return saveReturn;
        }

        public SaveReturn UpdateCompetitorMappings(List<CompetitorInventory> mappings, ProductInventory product, int channelId)
        {
            var saveReturn = new SaveReturn();
            var crudProduct = new CrudProductInventory();

            var hasPermission = crudProduct.Read(x => x.ProductInventoryID == product.ProductInventoryID &&
                x.ChannelFK == channelId).Count > 0;

            if (hasPermission)
            {
                try
                {
                    SuggestedCompetitorMappings.ForEach(x => x.ChannelFK = channelId);

                    var crud = new CrudCompetitorInventory();
                    var crudSkuMapping = new CrudSkuMapping();
                    crud.UpdateList(mappings);

                    var crudLookup = new CrudLookup();
                    int fileTypeID = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory").FirstOrDefault().LookupID;

                    foreach (var map in mappings)
                    {
                        var skuMap = new SKUMapping()
                        {
                            SKUMapFrom = map.ManufacturerPartNo,
                            SKUMapTo = product.ManufacturerPartNo,
                            BrandFK = map.BrandFK,
                            FileTypeFK = fileTypeID,
                            ChannelFK = map.ChannelFK,
                            InventoryFK = map.CompetitorFK
                        };

                        crudSkuMapping.Create(skuMap);

                        if (map.ProductInventoryFK > 0)
                        {
                            var crudProd = new CrudProductInventory();
                            var prod = crudProd.Read(map.ProductInventoryFK ?? 0);
                            prod.CompetitorCount += 1;
                        }
                    }

                    saveReturn.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    saveReturn.IsSuccess = false;
                    saveReturn.Message = ex.Message;
                    saveReturn.InnerException = ex.InnerException != null ? ex.InnerException.Message : "";
                }
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Product not found or you do not have permission to change it";
            }

            return saveReturn;
        }

        public SaveReturn UpateSkuMapping()
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                sr.IsSuccess = false;

                if (SkuMappingRecord.Mapping.ProductInventoryFK > 0)
                {
                    CrudProductInventory crudProd = new CrudProductInventory();
                    ProductInventory pi = crudProd.Read(SkuMappingRecord.Mapping.ProductInventoryFK);
                    SkuMappingRecord.Mapping.SKUMapTo = pi.ManufacturerPartNo;
                }

                CrudSkuMapping crud = new CrudSkuMapping();
                if (crud.ReadCheck(SkuMappingRecord.Mapping.BrandFK, SkuMappingRecord.Mapping.SKUMapTo))
                {
                    sr.Message = "A mapping for this Brand/MfPN combination already exists";
                }
                else
                {
                    crud.Update(SkuMappingRecord.Mapping);
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                sr.Message = ex.Message;
                sr.IsSuccess = false;
                sr.InnerException = ex.InnerException != null ? ex.InnerException.Message : "";
            }

            return sr;
        }

        public SaveReturn UpdateSingleSupplierMapping(long supplierInventoryFK, long productInventoryFK, int channelID)
        {
            var saveReturn = new SaveReturn();
            var crudProduct = new CrudProductInventory();

            var hasPermission = crudProduct.Read(x => x.ProductInventoryID == productInventoryFK &&
                x.ChannelFK == channelID).Count > 0;

            if (hasPermission)
            {
                try
                {
                    var crudSupplier = new CrudSupplierInventory();
                    var supplierInv = crudSupplier.Read(supplierInventoryFK);

                    if (supplierInv.ProductInventoryFK != null)
                    {
                        throw new ApplicationException("This supplier item is already mapped to a product");
                    }

                    supplierInv.ProductInventoryFK = productInventoryFK;
                    crudSupplier.Update(supplierInv);

                    var crudLookup = new CrudLookup();
                    int fileTypeID = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Supplier Inventory").FirstOrDefault().LookupID;

                    var crudSkuMapping = new CrudSkuMapping();
                    var skuMap = new SKUMapping()
                    {
                        SKUMapFrom = supplierInv.ManufacturerPartNo,
                        SKUMapTo = crudProduct.Read(y => y.ChannelFK == supplierInv.ChannelFK &&
                            y.ProductInventoryID == productInventoryFK).FirstOrDefault().ManufacturerPartNo,
                        BrandFK = supplierInv.BrandFK,
                        FileTypeFK = fileTypeID,
                        ChannelFK = supplierInv.ChannelFK,
                        InventoryFK = supplierInv.SupplierFK
                    };

                    crudSkuMapping.Create(skuMap);
                    saveReturn.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    saveReturn.IsSuccess = false;
                    saveReturn.Message = ex.Message;
                    saveReturn.InnerException = ex.InnerException != null ? ex.InnerException.Message : "";
                }
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Product not found or you do not have permission to change it";
            }

            return saveReturn;
        }

        public SaveReturn UpdateSingleCompetitorMapping(long competitorInventoryFK, long productInventoryFK, int channeID)
        {
            var saveReturn = new SaveReturn();
            var crudProduct = new CrudProductInventory();

            var hasPermission = crudProduct.Read(x => x.ProductInventoryID == productInventoryFK &&
                x.ChannelFK == channeID).Count > 0;

            if (hasPermission)
            {

                try
                {
                    var crudCompetitor = new CrudCompetitorInventory();
                    var competitorInv = crudCompetitor.Read(competitorInventoryFK);

                    if (competitorInv.ProductInventoryFK != null)
                    {
                        throw new ApplicationException("This item is already mapped to a product");
                    }

                    competitorInv.ProductInventoryFK = productInventoryFK;
                    crudCompetitor.Update(competitorInv);

                    var crudLookup = new CrudLookup();
                    int fileTypeID = crudLookup.Read(x => x.LookupType.LookupTypeName == "FileType" && x.LookupName == "Competitor Inventory").FirstOrDefault().LookupID;

                    var crudSkuMapping = new CrudSkuMapping();
                    var skuMap = new SKUMapping()
                    {
                        SKUMapFrom = competitorInv.ManufacturerPartNo,
                        SKUMapTo = crudProduct.Read(y => y.ChannelFK == competitorInv.ChannelFK &&
                            y.ProductInventoryID == productInventoryFK).FirstOrDefault().ManufacturerPartNo,
                        BrandFK = competitorInv.BrandFK,
                        FileTypeFK = fileTypeID,
                        ChannelFK = competitorInv.ChannelFK,
                        InventoryFK = competitorInv.CompetitorFK
                    };

                    crudSkuMapping.Create(skuMap);
                    saveReturn.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    saveReturn.IsSuccess = false;
                    saveReturn.Message = ex.Message;
                    saveReturn.InnerException = ex.InnerException != null ? ex.InnerException.Message : "";
                }
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Product not found or you do not have permission to change it";
            }

            return saveReturn;
        }

        public List<string> GetSuppliersAndCompetitors()
        {
            var crud = new CrudSkuMapping();
            return crud.GetSkuMapSuppliersCompetitorsList(_channelId, _ctx);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public class TelerikSkuMappings
        {
            public long SkuMappingId { get; set; }
            public string MappingType { get; set; }
            public string SupplierCompetitorName { get; set; }
            public string ProductName { get; set; }
            public string BrandName { get; set; }
            public string Mfpn { get; set; }
        }

        public class TelerikExceptions
        {
            public long ProductInventoryId { get; set; }
            public string Description { get; set; }
            public string BrandName { get; set; }
            public string Mfpn { get; set; }
            public string ProdId { get; set; }
            public int MappedCount { get; set; }
        }
    }

    public static class SkuMappingExtensions
    {
        public static IQueryable<SkuMappingViewModel.TelerikSkuMappings> AsTelerikSkuMappingsViewModel(this IQueryable<SkuMappingsDisplayModel> skuMappingQuery)
        {
            return skuMappingQuery.Select(o => new SkuMappingViewModel.TelerikSkuMappings
            {
                SkuMappingId = o.SkuMappingId,
                MappingType = o.Type,
                SupplierCompetitorName = o.SupplierCompetitorName,
                ProductName = o.Prod.Description,
                BrandName = o.Brnd.BrandName,
                Mfpn = o.Prod.ManufacturerPartNo
            });
        }

        public static IQueryable<SkuMappingViewModel.TelerikExceptions> AsTelerikExceptionsViewModel(this IQueryable<SkuMappingsDisplayModel> exceptionsQuery)
        {
            return exceptionsQuery.Select(o => new SkuMappingViewModel.TelerikExceptions
            {
                ProductInventoryId = o.Prod.ProductInventoryID,
                Description = o.Prod.Description,
                BrandName = o.Prod.Brand.BrandName,
                Mfpn = o.Prod.ManufacturerPartNo,
                ProdId = o.Prod.ClientProductID,
                MappedCount = o.Type == "Supplier" ? o.Prod.SupplierInventories.Count : o.Prod.CompetitorInventories.Count
            });
        }
    }
}
