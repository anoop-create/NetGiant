using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.Entity;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using EntityState = System.Data.Entity.EntityState;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Orders
{
    public class OrderTrackingViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;
        public OrderTrackingViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<Telerik> OrderTrackingList { get; set; }
        public OrderTracking OrderTrackingEntry { get; set; }

        public void GetOrderTracking()
        {
            List<Telerik> otl = _ctx.OrderTracking
                .Include(x => x.provider)
                .OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNumber)
                .Select(x => new Telerik
                {
                    Id = x.OrderTrackingId,
                    Website = x.WebsiteFk == 1 ? "TG" : x.WebsiteFk == 2 ? "CM" : "NG",
                    OrderNumber = x.OrderNumber,
                    PurchaseOrderNumber = x.PurchaseOrderNumber,
                    OrderDate = x.OrderDate,
                    CustomerRef = x.CustomerRef,
                    Courier = x.provider.providerName,
                    Name = x.FirstName + " " + x.Surname,
                    Email = x.Email,
                    TrackingCode = x.TrackingCode,
                    TrackingLink = x.TrackingLink,
                    IsSent = x.IsSent
                })
                .ToList();

            OrderTrackingList = otl.AsQueryable();
        }

        public void GetOrderTracking(int id)
        {
            OrderTrackingEntry = _ctx.OrderTracking
                .Where(x => x.OrderTrackingId == id)
                .FirstOrDefault();
        }

        public OrderTrackingViewModel EditEntry(int id)
        {
            OrderTrackingEntry = _ctx.OrderTracking
                .Where(x => x.OrderTrackingId == id).FirstOrDefault();

            return this;
        }

        public bool SaveEntry()
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (OrderTrackingEntry.OrderTrackingId > 0)
                    {
                        db.Entry(OrderTrackingEntry).State = EntityState.Modified;

                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public class Telerik
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string OrderNumber { get; set; }
            public string PurchaseOrderNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public string CustomerRef { get; set; }
            public string Courier { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string TrackingCode { get; set; }
            public string TrackingLink { get; set; }
            public bool IsSent { get; set; }
        }
    }
}



