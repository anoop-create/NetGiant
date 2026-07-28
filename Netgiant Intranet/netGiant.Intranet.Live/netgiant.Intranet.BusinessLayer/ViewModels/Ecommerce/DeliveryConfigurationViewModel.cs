using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class DeliveryConfigurationViewModel
    {
        public DeliveryConfigurationViewModel()
        {
            _ctx = new ngmdEntities();
            Services = new List<SelectListItem>();
        }

        public IQueryable<TelerikDeliveryZone> DeliveryZones { get; set; }
        public IQueryable<TelerikDeliveryService> DeliveryServices { get; set; }
        public deliveryZone DeliveryZone { get; set; }
        public deliveryService DeliveryService { get; set; }
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
            Websites = SelectListViewModel.AllWebsites().ToList();
            Websites.Insert(0, new SelectListItem{ Text = "Please Select...", Value = ""});
        }

        public void NewDeliveryService()
        {
            DeliveryService = new deliveryService();
            Websites = SelectListViewModel.AllWebsites().ToList();
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

        public class TelerikDeliveryService
        {
            public int DeliveryServiceId { get; set; }
            public int WebsiteFk { get; set; }
            public string WebsiteName { get; set; }
            public string ServiceName { get; set; }
            public string StockRef { get; set; }
            public decimal Price { get; set; }
            public string InfoMessage { get; set; }
            public bool IsSaturdayOnly { get; set; }
            public bool IsCompatibleInkOnly { get; set; }
            public bool IsBulky { get; set; }
            public bool UsesThresholds { get; set; }
            public decimal? ThresholdStart { get; set; }
            public decimal? ThresholdEnd { get; set; }
            public int DeliveryMethod { get; set; }
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
                IsSaturdayOnly = o.IsSaturdayOnly,
                IsCompatibleInkOnly = o.IsCompatibleInkOnly,
                IsBulky = o.IsBulky,
                UsesThresholds = o.UsesThresholds,
                ThresholdStart = o.ThresholdStart,
                ThresholdEnd = o.ThresholdEnd,
                DeliveryMethod = o.DeliveryMethod
            });
        }
    }
}
