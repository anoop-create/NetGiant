using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class DeliveryConfigurationViewModel : CommonViewModel
    {
        public DeliveryConfigurationViewModel()
        {
            _ctx = new ngmdEntities();
            Services = new List<SelectListItem>();
        }

        public IQueryable<TelerikDeliveryZone> DeliveryZones { get; set; }
        public IQueryable<TelerikDeliveryService> DeliveryServices { get; set; }
        public IQueryable<TelerikDeliverySupplierCode> DeliverySupplierCodes { get; set; }
        public IQueryable<SelectListItem> DeliveryServiceDropDownList { get; set; }
        public IQueryable<SelectListItem> ProviderDropDownList { get; set; }
        public deliveryZone DeliveryZone { get; set; }
        public deliveryService DeliveryService { get; set; }
        public deliverySupplierCode DeliverySupplierCode { get; set; }
        public List<SelectListItem> Services { get; set; }
        public List<SelectListItem> Websites { get; set; }

        private readonly ngmdEntities _ctx;

        public void GetDeliveryZones()
        {
            DeliveryZones = _ctx.deliveryZone.AsQueryable().AsTelerikViewModel();
        }

        public void GetDeliveryZone(int id)
        {
            DeliveryZone = _ctx.deliveryZone.Find(id);
            GetDeliveryServicesForWebsite(DeliveryZone.WebsiteFK);
        }

        public void GetDeliveryServices()
        {
            DeliveryServices = _ctx.deliveryService.AsQueryable().AsTelerikViewModel();
        }

        public void GetDeliveryService(int id)
        {
            DeliveryService = _ctx.deliveryService.Find(id);
            GetDeliveryServicesForWebsite(DeliveryService.WebsiteFK);
        }
























        public void GetDeliverySupplierCodes()
        {
            DeliverySupplierCodes = _ctx.deliverySupplierCodes
                .GroupJoin
                (
                    _ctx.deliveryService,
                    delSupCode => delSupCode.deliveryServiceFk,
                    delServ => delServ.DeliveryServiceId,
                    (delSupCode, delServ) => new { delSupCode, delServ }
                )
                .SelectMany
                (
                    x => x.delServ.DefaultIfEmpty(),
                    (x, y) => new TelerikDeliverySupplierCode
                    {
                        DeliverySupplierCodeId = x.delSupCode.deliverySupplierCodeId,
                        DeliveryServiceFk = x.delSupCode.deliveryServiceFk,
                        DeliveryServiceName = y == null ? "##" : y.ServiceName,
                        ProviderFk = x.delSupCode.providerFk,
                        ProviderName = "##",
                        ProviderItemCode = x.delSupCode.providerItemCode,
                        Price = x.delSupCode.price,
                        WebsiteFk = y == null ? 0 : y.WebsiteFK,
                        Website = "##"
                    }
                )
                .GroupJoin
                (
                    _ctx.provider,
                    ServSupCode => ServSupCode.ProviderFk,
                    Prov => Prov.providerID,
                    (ServSupCode, Prov) => new { ServSupCode, Prov }
                )
                .SelectMany
                (
                    prov => prov.Prov.DefaultIfEmpty(),
                    (servsupcode, prov) => new TelerikDeliverySupplierCode
                    {
                        DeliverySupplierCodeId = servsupcode.ServSupCode.DeliverySupplierCodeId,
                        DeliveryServiceFk = servsupcode.ServSupCode.DeliveryServiceFk,
                        DeliveryServiceName = servsupcode.ServSupCode.DeliveryServiceName,
                        ProviderFk = servsupcode.ServSupCode.ProviderFk,
                        ProviderName = prov == null ? "##" : prov.providerName,
                        ProviderItemCode = servsupcode.ServSupCode.ProviderItemCode,
                        Price = servsupcode.ServSupCode.Price,
                        WebsiteFk = servsupcode.ServSupCode.WebsiteFk,
                        Website = "##"
                    }
                )
                .GroupJoin
                (
                    _ctx.Website,
                    provservsupcode => provservsupcode.WebsiteFk,
                    web => web.WebsiteID,
                    (provservsupcode, web) => new {provservsupcode, web }
                )
                .SelectMany
                (
                    web => web.web.DefaultIfEmpty(),
                    (provservsupcode, web) => new TelerikDeliverySupplierCode
                    {
                        DeliverySupplierCodeId = provservsupcode.provservsupcode.DeliverySupplierCodeId,
                        DeliveryServiceFk = provservsupcode.provservsupcode.DeliveryServiceFk,
                        DeliveryServiceName = provservsupcode.provservsupcode.DeliveryServiceName,
                        ProviderFk = provservsupcode.provservsupcode.ProviderFk,
                        ProviderName = provservsupcode.provservsupcode.ProviderName,
                        ProviderItemCode = provservsupcode.provservsupcode.ProviderItemCode,
                        Price = provservsupcode.provservsupcode.Price,
                        WebsiteFk = provservsupcode.provservsupcode.WebsiteFk,
                        Website = web == null ? "##" : web.FriendlyName
                    }
                )

                .AsQueryable();
        }

        public void GetDeliverySupplierCode(int id)
        {
            if (id == 0)
            {
                DeliverySupplierCode = new deliverySupplierCode();
                DeliverySupplierCode.providerItemCode = "";
            }
            else
            {
                //if we are here there IS an id above zero which means we get this
                DeliverySupplierCode = _ctx.deliverySupplierCodes
                    .Find(id);
            }

            DeliveryServiceDropDownList = _ctx.deliveryService
                .Join
                (
                    _ctx.Website,
                    delServ => delServ.WebsiteFK,
                    web => web.WebsiteID,
                    (delServ, web) => new {delServ, web}
                )
                .OrderBy
                (
                    x => x.web.FriendlyName
                )
                .ThenBy
                (
                    x => x.delServ.ServiceName
                )
                .Select
                (x => new SelectListItem
                {
                    Value = x.delServ.DeliveryServiceId.ToString(),
                    Text = x.web.FriendlyName + " - " + x.delServ.ServiceName
                });

            ProviderDropDownList = _ctx.provider
                .Where(x => x.providerTypeFK == 2)
                .Select(x => new SelectListItem
                {
                    Value = x.providerID.ToString(),
                    Text = x.providerName
                });
        }

        public bool CheckDeliverySupplierCodeExists(DeliveryConfigurationViewModel model)
        {
            //check there isn't one already for this providerFk and deliveryServiceFk with NOT this deliverySupplierCodeId
            deliverySupplierCode alreadyExists = _ctx.deliverySupplierCodes
                .Where(x => x.providerFk == model.DeliverySupplierCode.providerFk)
                .Where(x => x.deliveryServiceFk == model.DeliverySupplierCode.deliveryServiceFk)
                .Where(x => x.deliverySupplierCodeId != model.DeliverySupplierCode.deliverySupplierCodeId)
                .FirstOrDefault();
            if (alreadyExists != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void NewSaveDeliverySupplierCode(DeliveryConfigurationViewModel model)
        {
            if (model.DeliverySupplierCode.deliverySupplierCodeId == 0)
            {
                //we are creating a new entry here
                _ctx.deliverySupplierCodes.Add(model.DeliverySupplierCode);
                _ctx.SaveChanges();
            }
            else
            {
                //we are updating and existing entry
                _ctx.Entry(DeliverySupplierCode).State = EntityState.Modified;
                _ctx.SaveChanges();
            }
        }

        public static void DeleteDeliverySupplierCode(int id)
        {
            using (var db = new ngmdEntities())
            {
                var entryToDelete = db.deliverySupplierCodes.FirstOrDefault(x => x.deliverySupplierCodeId == id);

                if (entryToDelete != null)
                {
                    db.Entry(entryToDelete).State = EntityState.Deleted;
                }
                db.SaveChanges();
            }
        }


























        public void GetDeliveryServicesForWebsite(int websiteId)
        {
            using (var db = new ngmdEntities())
            {
                Services = db.deliveryService
                    .Where(x => x.WebsiteFK == websiteId)
                    .Select(x => new SelectListItem
                    {
                        Text = x.ServiceName,
                        Value = x.DeliveryServiceId.ToString()
                    })
                    .ToList();
            }

            Services.Insert(0, new SelectListItem
            {
                Text = "Please Select...",
                Value = "0"
            });
        }

        public void NewDeliveryZone()
        {
            DeliveryZone = new deliveryZone();
            Websites = SelectListViewModel.GetAllWebsites().ToList();
            Websites.Insert(0, new SelectListItem{ Text = "Please Select...", Value = ""});
        }

        public void NewDeliveryService()
        {
            DeliveryService = new deliveryService();
            DeliveryService.IsSpecialOrderOnly = true;
            Websites = SelectListViewModel.GetAllWebsites().ToList();
            Websites.Insert(0, new SelectListItem { Text = "Please Select...", Value = "" });
        }
        
        public deliveryLookup CreateNewDeliveryLookup(int deliveryZoneId, int deliveryServiceId, int sequence)
        {
            using (var db = new ngmdEntities())
            {
                return new deliveryLookup
                {
                    DeliveryServiceFK = deliveryServiceId,
                    DeliveryZoneFK = deliveryZoneId,
                    Sequence = sequence,
                    IsActive = true,
                    deliveryService = db.deliveryService.Find(deliveryServiceId),
                    deliveryZone = db.deliveryZone.Find(deliveryZoneId)
                };
            }
        }

        public void SaveDeliveryZone()
        {
            ICollection<deliveryLookup> originalZoneLookups = null;
            deliveryZone dZ;

            using (var db = new ngmdEntities())
            {
                dZ = db.deliveryZone.Include(x => x.deliveryLookup).FirstOrDefault(x => x.DeliveryZoneId == DeliveryZone.DeliveryZoneId);
            }

            using (var db = new ngmdEntities())
            {
                
                if (dZ != null)
                    originalZoneLookups = dZ.deliveryLookup;

                if (originalZoneLookups != null)
                {
                    List<int> removedServices = originalZoneLookups.Select(x => x.DeliveryLookupId)
                        .Except(DeliveryZone.deliveryLookup.Select(x => x.DeliveryLookupId))
                        .ToList();

                    for (var i = 0; i < removedServices.Count(); i++)
                    {
                        db.deliveryLookup.Remove(db.deliveryLookup.Find(removedServices[i]));
                    }
                }

                foreach (var deliverLookup in DeliveryZone.deliveryLookup)
                {
                    deliverLookup.deliveryService = null;
                    db.Entry(deliverLookup).State = deliverLookup.DeliveryLookupId > 0 ? EntityState.Modified : EntityState.Added;
                }

                db.SaveChanges();

                DeliveryZone.Postcodes = DeliveryZone.Postcodes ?? "";
                db.Entry(DeliveryZone).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void CreateDeliveryService()
        {
            using (var db = new ngmdEntities())
            {
                db.Entry(DeliveryService).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        public void SaveDeliveryService()
        {
            using (var db = new ngmdEntities())
            {
                db.Entry(DeliveryService).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void CreateDeliveryZone()
        {
            using (var db = new ngmdEntities())
            {
                DeliveryZone.Postcodes = DeliveryZone.Postcodes ?? "";

                foreach (var deliverLookup in DeliveryZone.deliveryLookup)
                {
                    deliverLookup.deliveryService = null;
                    db.Entry(deliverLookup).State = EntityState.Added;
                }

                db.Entry(DeliveryZone).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        public static void DeleteDeliveryService(int id)
        {
            using (var db = new ngmdEntities())
            {
                var entryToDelete = db.deliveryService.Include(x => x.deliveryLookup).FirstOrDefault(x => x.DeliveryServiceId == id);

                if (entryToDelete != null)
                {
                    db.deliveryLookup.RemoveRange(entryToDelete.deliveryLookup);
                    db.Entry(entryToDelete).State = EntityState.Deleted;
                }
                db.SaveChanges();
            }
        }
        
        public static void DeleteDeliveryZone(int id)
        {
            using (var db = new ngmdEntities())
            {
                var entryToDelete = db.deliveryZone.Include(x => x.deliveryLookup).FirstOrDefault(x => x.DeliveryZoneId == id);

                if (entryToDelete != null)
                {
                    db.deliveryLookup.RemoveRange(entryToDelete.deliveryLookup);
                    db.Entry(entryToDelete).State = EntityState.Deleted;
                }
                db.SaveChanges();
            }
        }

        public class TelerikDeliveryZone
        {
            public int DeliveryZoneId { get; set; }
            public int WebsiteFk { get; set; }
            public string WebsiteName { get; set; }
            public string ZoneName { get; set; }
            public bool IsDefault { get; set; }
            public bool ApplyVat { get; set; }
            public string Postcodes { get; set; }
        }

















        public class TelerikDeliverySupplierCode
        {
            public int DeliverySupplierCodeId { get; set; }
            public int DeliveryServiceFk { get; set; }
            public string DeliveryServiceName { get; set; }
            public int ProviderFk { get; set; }
            public string ProviderName { get; set; }
            public string ProviderItemCode { get; set; }
            public decimal Price { get; set; }
            public int WebsiteFk { get; set; }
            public string Website { get; set; }
        }



















            public class TelerikDeliveryService
            {
                public int DeliveryServiceId { get; set; }
                public int WebsiteFk { get; set; }
                public string WebsiteName { get; set; }
                public string ServiceName { get; set; }
                public string StockRef { get; set; }
                public decimal Price { get; set; }
                public string InfoMessage { get; set; }
                public bool? IsSpecialOrderOnly { get; set; }
                public bool IsSaturdayOnly { get; set; }
                public bool IsCompatibleInkOnly { get; set; }
                public bool IsBulky { get; set; }
                public bool UsesThresholds { get; set; }
                public decimal? ThresholdStart { get; set; }
                public decimal? ThresholdEnd { get; set; }
                public int DeliveryMethod { get; set; }
                public int ProductFk { get; set; }
            }
    }

    public static class DeliveryConfigurationViewModelExtensions
    {
        public static IQueryable<DeliveryConfigurationViewModel.TelerikDeliveryZone> AsTelerikViewModel(this IQueryable<deliveryZone> query)
        {
            return query.Select(o => new DeliveryConfigurationViewModel.TelerikDeliveryZone
            {
                DeliveryZoneId = o.DeliveryZoneId,
                WebsiteFk = o.WebsiteFK,
                WebsiteName = o.Website.WebsiteName,
                ZoneName = o.ZoneName,
                IsDefault = o.IsDefault,
                ApplyVat = o.ApplyVat,
                Postcodes = o.Postcodes
            });
        }

        public static IQueryable<DeliveryConfigurationViewModel.TelerikDeliveryService> AsTelerikViewModel(this IQueryable<deliveryService> query)
        {
            return query.Select(o => new DeliveryConfigurationViewModel.TelerikDeliveryService
            {
                DeliveryServiceId = o.DeliveryServiceId,
                WebsiteFk = o.WebsiteFK,
                WebsiteName = o.Website.WebsiteName,
                ServiceName = o.ServiceName,
                StockRef = o.StockRef,
                Price = o.Price,
                InfoMessage = o.InfoMessage,
                IsSpecialOrderOnly = o.IsSpecialOrderOnly,
                IsSaturdayOnly = o.IsSaturdayOnly,
                IsCompatibleInkOnly = o.IsCompatibleInkOnly,
                IsBulky = o.IsBulky,
                UsesThresholds = o.UsesThresholds,
                ThresholdStart = o.ThresholdStart,
                ThresholdEnd = o.ThresholdEnd,
                DeliveryMethod = o.DeliveryMethod,
                ProductFk = o.ProductFk    
            });
        }
    }
}
