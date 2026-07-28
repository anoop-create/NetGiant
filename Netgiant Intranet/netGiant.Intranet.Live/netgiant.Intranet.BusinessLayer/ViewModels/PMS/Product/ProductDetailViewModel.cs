using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using PagedList;
using System;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class ProductDetailViewModel
    {
        public product ProductDetail { get; set; }
        public IQueryable<SelectListItem> Websites { get; set; }
        public List<providerInventory> ProductCompetitors { get; set; }
        public List<providerInventory> ProductSuppliers { get; set; }
        public List<priorityProvider> PrioritySuppliers { get; set; }
        public List<product> WhereUsed { get; set; }
        public IPagedList<eqProductMembership> ProductMembership { get; set; }
        public IQueryable<SelectListItem> AllEquipment { get; set; }
        public int EquipmentId { get; set; }

        public ProductDetailViewModel GetProductDetail(int productFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ProductDetail = db.product
                    .Include("AxisFields.AxisFieldsAdditional")
                    .Include("websiteInventory.productPrice")
                    .Include("websiteInventory.website")
                    .Include("websiteInventory.categoryCode")
                    .Include("websiteInventory.secondaryCategoryLookup")
                    .Include("websiteInventory.secondaryCategoryLookup.categoryCode")
                    .Include("skuMapping.providerInventory.provider")
                    .Include("skuMapping.providerInventory.providerPrice")
                    .Include("AxisEbusinessMapping.AxisEbusiness")
                    .Include(x => x.productGroup)
                    .Include(x => x.salesAreaGroup)
                    .Include(x => x.productStatus)
                    .Include(x => x.productItemType)
                    .Include("assemblyComponent.product1.websiteInventory.productPrice")
                    .Include("crossSellingLink.product1")
                    .Where(x => x.productID == productFK)
                    .FirstOrDefault();

                AxisFields af = ProductDetail.AxisFields;
                Websites = SelectListViewModel.AllWebsites();
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

        public ProductDetailViewModel GetProductCompetitors(string selectedPartNo)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                NoLockInterceptor.ApplyNoLock = true;

                ProductCompetitors = db.providerInventory.Include(m => m.providerPrice)
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

                AllEquipment = SelectListViewModel.AllEquipment();
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
}
