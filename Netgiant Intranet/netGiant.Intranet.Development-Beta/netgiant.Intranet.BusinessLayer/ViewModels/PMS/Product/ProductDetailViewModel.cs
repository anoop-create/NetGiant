using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using PagedList;
using System;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class ProductDetailViewModel : CommonViewModel
    {
        public product ProductDetail { get; set; }
        public string ManufacturerName { get; set; }
        public string ProductStatus { get; set; }
        public string ProductItemType { get; set; }
        public IQueryable<SelectListItem> Websites { get; set; }
        public List<providerInventory> ProductCompetitors { get; set; }
        public List<providerInventory> ProductSuppliers { get; set; }
        public List<priorityProvider> PrioritySuppliers { get; set; }
        public List<product> WhereUsed { get; set; }
        public IPagedList<eqProductMembership> ProductMembership { get; set; }
        public IQueryable<SelectListItem> AllEquipment { get; set; }
        public int EquipmentId { get; set; }
        public IQueryable<TelerikProviderInventory> ProviderInventory { get; set; }
        public List<AddOnDetail> AddonProducts { get; set; }
        public ProductDetailViewModel GetProductDetail(int productFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ProductDetail = db.product
                    .Include("AxisFields.AxisFieldsAdditional")
                    .Include("websiteInventory.productPrice")
                    .Include("websiteInventory.productImage")
                    .Include("websiteInventory.website")
                    .Include("websiteInventory.categoryCode")
                    .Include("websiteInventory.secondaryCategoryLookup")
                    .Include("websiteInventory.secondaryCategoryLookup.categoryCode")
                    .Include("skuMapping.providerInventory.provider")
                    .Include("skuMapping.providerInventory.providerPrice")
                    .Include("AxisEbusinessMapping.AxisEbusiness")
                    .Include(x => x.productGroup)
                    .Include(x => x.salesAreaGroup)
                    .Include("assemblyComponent.product1.websiteInventory.productPrice")
                    .Include("crossSellingLink.product1")
                    .Where(x => x.productID == productFK)
                    .FirstOrDefault();

                ManufacturerName = ProductDetail.manufacturer.manufacturerName;
                ProductStatus = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "ProductStatus" && x.AltLookupId == ProductDetail.productStatusFK)
                                    .FirstOrDefault().LookupName;
                ProductItemType = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "ProductItemType" && x.AltLookupId == ProductDetail.productItemTypeFK)
                                    .FirstOrDefault().LookupName;

                var orderedSkuMappings = ProductDetail.skuMapping
                        .Where(x => x.providerInventory != null)
                        .OrderBy(x => x.providerInventory.providerPrice
                        .OrderByDescending(m => m.dateLastUpdate)
                        .FirstOrDefault().price);

                ProviderInventory = orderedSkuMappings.Select
                        (x => new TelerikProviderInventory
                        {
                            ProviderInventoryId = x.providerInventoryFK,
                            ProviderId = x.providerFK,
                            ProviderName = x.providerInventory.provider.providerName,
                            Description = x.providerInventory.description,
                            SupplierReference = x.providerInventory.providerPartNo,
                            PartNo = x.providerInventory.partNo,
                            Price = x.providerInventory.providerPrice.OrderByDescending(m => m.dateLastUpdate).FirstOrDefault().price,
                            Quantity = x.providerInventory.quantity,
                            InventoryUpdated = x.providerInventory.dateLastUpdate,
                            PriceChangedOn = x.providerInventory.providerPrice.OrderByDescending(m => m.dateLastUpdate).FirstOrDefault().dateLastUpdate,
                            AxisSupplierRef = x.providerInventory.provider.axisSupplierRef,
                            Barcode = x.providerInventory.barcode,
                            Untrusted = x.providerInventory.untrustedProvider,
                            Active = x.provider.active,
                            UntrustedAuto = x.providerInventory.untrustedProvider ? x.providerInventory.untrustedAuto ? "Auto" : "Manual" : ""
                        }).AsQueryable();

                AxisFields af = ProductDetail.AxisFields;
                Websites = SelectListViewModel.GetAllWebsites();
                GetProductCompetitors(ProductDetail.partNo);
                FindWhereUsed(productFK);

                if (af != null)
                {
                    af.Attribute1Description = LookupAttrDesc(db, 1, af.attr1);
                    af.Attribute2Description = LookupAttrDesc(db, 2, af.attr2);
                    af.Attribute3Description = LookupAttrDesc(db, 3, af.attr3);
                    af.Attribute4Description = LookupAttrDesc(db, 4, af.attr4);
                    af.Attribute5Description = LookupAttrDesc(db, 5, af.attr5);
                    af.Attribute6Description = LookupAttrDesc(db, 6, af.attr6);
                    af.Attribute7Description = LookupAttrDesc(db, 7, af.attr7);
                    af.Attribute8Description = LookupAttrDesc(db, 8, af.attr8);
                    af.Attribute9Description = LookupAttrDesc(db, 9, af.attr9);
                    af.Attribute10Description = LookupAttrDesc(db, 10, af.attr10);
                }

                NoLockInterceptor.ApplyNoLock = true;

                PrioritySuppliers = db.priorityProvider.Where(x => x.manufacturerFK == ProductDetail.manufacturerFK).ToList();

                ProductMembership = db.eqProductMembership
                .Include(x => x.eqEquipment)
                .Include(x => x.product)
                .Include("eqEquipment.manufacturer")
                .Where(x => x.productFK == productFK)
                .OrderBy(x => x.eqProductMembershipID)
                .ToPagedList(1, 10);
                AddonProducts =
                    (
                        from pa in db.ProductAddon
                        join p in db.product
                            on pa.AddonProductId equals p.productID
                        where pa.ProductId == productFK && pa.IsActive
                        orderby pa.DisplayOrder
                        select new AddOnDetail
                        {
                            ProductId = p.productID.ToString(),
                            ProductName = p.productName,
                            PartNo=p.partNo,
                            Barcode=p.barcode
                        }
                    ).ToList();
            }

            return this;
        }

        private static string LookupAttrDesc(ngmdEntities db, int attrNameId, int? attrId)
        {
            string returnValue = null;

            if (attrId != null)
            {
                returnValue = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1 &&
                                x.attrNameFK == attrNameId && x.attrValueID == attrId)
                                .FirstOrDefault().attrValueDesc;
            }

            return returnValue;
        }

        public string GetFullPathOfWebInventoryImages(websiteInventory websiteInventory)
        {
            //var strImageDomainRoot = SharedFunctions.GetConfigurationSetting("Website Application Variables", "CDN", websiteInventory.websiteFK);
            //var strVersionNumber = SharedFunctions.GetConfigurationSetting("Website Application Variables", "VersionNumber", websiteInventory.websiteFK);

            //strImageDomainRoot = strImageDomainRoot.Replace("[version]", strVersionNumber);

            //return strImageDomainRoot;

            switch (websiteInventory.websiteFK)
            {
                case 1:
                    {
                        return "../../../TGImages";
                    }
                case 2:
                    {
                        return "../../../CMImages";
                    }
            }

            return "../../../NGImages";            
        }

        public bool DeleteMembership(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqProductMembership prodMem = db.eqProductMembership.Find(id);
                    db.eqProductMembership.Remove(prodMem);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }

        public void GetEquipmentMemberships(int? page, int id, string orderBy)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities()) {
               
                IQueryable<eqProductMembership> membership = db.eqProductMembership
                    .Include(x => x.eqEquipment)
                    .Include(x => x.product)
                    .Include("eqEquipment.manufacturer")
                    .Where(x => x.productFK == id);

                switch (orderBy)
                {
                    case "equipmentAsc":
                        membership = membership.OrderBy(x => x.eqEquipment.description);
                        break;
                    case "equipmentDesc":
                        membership = membership.OrderByDescending(x => x.eqEquipment.description);
                        break;
                    case "manufacturerAsc":
                        membership = membership.OrderBy(x => x.eqEquipment.manufacturer.manufacturerName);
                        break;
                    case "manufacturerDesc":
                        membership = membership.OrderByDescending(x => x.eqEquipment.manufacturer.manufacturerName);
                        break;
                    default:
                        membership = membership.OrderBy(x => x.eqEquipmentFK);
                        break;
                }

                ProductMembership = membership.ToPagedList(pageNumber, pageSize);
            }
        }

        private void FindWhereUsed(int productFK)
        {
            WhereUsed = new List<product>();

            using (ngmdEntities db = new ngmdEntities())
            {
                db.assemblyComponent
                    .Where(x => x.assemblyComponentFK == productFK)
                    .ToList()
                    .ForEach(x => WhereUsed.Add(x.product));
            }
        }
        public class AddOnDetail{
            public string ProductName { get; set; }
            public string ProductId { get; set; }
            public string PartNo { get; set; }
            public string Barcode { get; set; }

        }
        public ProductDetailViewModel GetProductCompetitors(string selectedPartNo)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                NoLockInterceptor.ApplyNoLock = true;

                ProductCompetitors = db.providerInventory
                    .Include(m => m.providerPrice)
                    .Include(m => m.provider)
                    .Where(x => x.partNo == selectedPartNo
                        && (x.provider.providerTypeFK == 1
                        || x.provider.providerTypeFK == 5))
                    .ToList();
            }

            return this;
        }

        public ProductDetailViewModel GetEquipmentOptions(int id)
        {
            using(ngmdEntities db = new ngmdEntities())
            {
                ProductDetail = db.product.Where(x => x.productID == id).FirstOrDefault();

                AllEquipment = SelectListViewModel.GetAllEquipment();
            }

            return this;
        }

        public bool SaveMembership(ProductDetailViewModel model)
        {
            var success = true;

            try
            {
                using(ngmdEntities db = new ngmdEntities())
                {
                    eqProductMembership membership = new eqProductMembership();
                    membership.eqEquipmentFK = model.EquipmentId;
                    membership.productFK = model.ProductDetail.productID;

                    db.eqProductMembership.Add(membership);
                    db.SaveChanges();
                }
            }
            catch(Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }


            return success;
        }
    }

    public class TelerikProviderInventory
    {
        public int? ProviderInventoryId { get; set; }
        public int? ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string Description { get; set; }
        public string SupplierReference { get; set; } // supplier reference
        public string PartNo { get; set; }
        public double Price { get; set; }// price
        public int Quantity { get; set; }
        public DateTime InventoryUpdated { get; set; }
        public DateTime PriceChangedOn { get; set; }
        public int? AxisSupplierRef { get; set; }
        public string Barcode { get; set; }
        public bool Untrusted { get; set; }
        public bool Active { get; set; }
        public string UntrustedAuto { get; set; }
    }
}
