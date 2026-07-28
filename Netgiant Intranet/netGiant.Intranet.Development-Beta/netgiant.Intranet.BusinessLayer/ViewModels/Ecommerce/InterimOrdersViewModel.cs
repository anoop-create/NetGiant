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

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class InterimOrdersViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;
        public InterimOrdersViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<Telerik> InterimOrdersList { get; set; }
        public InterimOrder InterimOrdersEntry { get; set; }

        public void GetInterimOrders()
        {
            List<Telerik> iol = _ctx.InterimOrder
                .OrderByDescending(x => x.DateTime)
                .Select(x => new Telerik
                {
                    Id = x.InterimOrderId,
                    Website = x.WebsiteFk == 1 ? "TG" : x.WebsiteFk == 2 ? "CM" : "NG",
                    DateTime = x.DateTime,
                    IsOrdered = x.IsOrdered,
                    InterimOrderTypeId = x.InterimOrderTypeFk,
                    Json = x.Json,
                    Reason = x.Reason ?? ""
                })
                .ToList();

            foreach (Telerik t in iol)
            {
                t.InterimOrderType = GetInterimOrderType(t.InterimOrderTypeId);
                t.Name = GetJsonItem(t.Json, "name", t.InterimOrderType);
                t.Amount = GetJsonItem(t.Json, "amount", t.InterimOrderType);
                t.Type = GetJsonItem(t.Json, "type", t.InterimOrderType);
            }

            InterimOrdersList = iol.AsQueryable();
        }

        public void GetInterimOrder(int id)
        {
            InterimOrdersEntry = _ctx.InterimOrder
                .Where(x => x.InterimOrderId == id)
                .FirstOrDefault();
        }

        public InterimOrdersViewModel EditEntry(int id)
        {
            InterimOrdersEntry = _ctx.InterimOrder
                .Where(x => x.InterimOrderId == id).FirstOrDefault();

            return this;
        }

        public bool SaveEntry()
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (InterimOrdersEntry.InterimOrderId > 0)
                    {
                        db.Entry(InterimOrdersEntry).State = EntityState.Modified;

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

        public SaveReturn MarkAsOrdered(int id, bool deleted)
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    var interimOrderEntry = db.InterimOrder.Find(id);
                    interimOrderEntry.IsOrdered = true;
                    db.Entry(interimOrderEntry).State = EntityState.Modified;
                    db.SaveChanges();
                }

                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }

        public class Telerik
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public DateTime DateTime { get; set; }
            public string Name { get; set; }
            public string Amount { get; set; }
            public bool IsOrdered { get; set; }
            public string Json { get; set; }
            public string Type { get; set; }
            public int InterimOrderTypeId { get; set; }
            public string InterimOrderType { get; set; }
            public string Reason { get; set; }
        }

        private string GetInterimOrderType(int id)
        {
            var y = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "InterimOrderType" && x.AltLookupId == id).ToList();
            return y[0].LookupName;
        }

        private string GetJsonItem(string json, string item, string type)
        {
            JObject j = JsonConvert.DeserializeObject<JObject>(json);
            JObject jO = new JObject();
            string val = "";

            if (type == "Order")
            {
                jO = JsonConvert.DeserializeObject<JObject>(j["OrderObject"].ToString());
                if (item == "name")
                {
                    val = jO["BillingUser"]["ContactName"].ToString();
                }
                if (item == "amount")
                {
                    val = jO["Net"].ToString();
                }
                if (item == "type")
                {
                    val = Enum.GetName(typeof(PaymentSource), int.Parse(jO["PaymentSource"].ToString()));
                }
            }
            if (type == "Customer")
            {
                jO = JsonConvert.DeserializeObject<JObject>(j["CustomerObject"].ToString());
                if (item == "name")
                {
                    val = jO["ContactName"].ToString();
                }
                if (item == "amount")
                {
                    val = "-";
                }
                if (item == "type")
                {
                    val = "-";
                }
            }

            return val == null ? "n/a" : val;
        }

        public enum PaymentSource
        {
            Account = 0,
            SagePay = 1,
            PayPal = 5,
            AmazonPay = 9,
            Cheque = 10,
            Telephone = 11,
            BACS = 26
        }
    }

    public static class InterimkOrdersModeExtensions
    {
        public static IQueryable<StuckOrdersViewModel.Telerik> AsTelerikViewModel(this IQueryable<StuckOrder> stuckOrderQuery)
        {
            return stuckOrderQuery.Select(o => new StuckOrdersViewModel.Telerik
            {
                Ref = o.Ref,
                Website = o.Website,
                DbName = o.DbName,
                OrderNumber = o.OrderNumber,
                AccountNumber = o.AccountNumber,
                UserNumber = o.UserNumber,
                Net = o.Net,
                Vat = o.Vat,
                Email = o.Email,
                Timestamp = o.Timestamp,
                Imported = o.Imported
            });
        }
    }

    //public class InterimOrder
    //{
    //    public long Ref { get; set; }
    //    public string Website { get; set; }
    //    public string DbName { get; set; }
    //    public string OrderNumber { get; set; }
    //    public string AccountNumber { get; set; }
    //    public string UserNumber { get; set; }
    //    public decimal Net { get; set; }
    //    public decimal Vat { get; set; }
    //    public string Email { get; set; }
    //    public DateTime Timestamp { get; set; }
    //    public int Imported { get; set; }
    //}

}


