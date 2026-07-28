using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Data;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Orders
{
    public class OpayoViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;
        private DateTime _datefrom;

        public OpayoViewModel()
        {
            _ctx = new ngmdEntities();
            _datefrom = DateTime.Now.AddYears(-1);
        }

        public IQueryable<TelerikOpayoTransaction> TransactionList { get; set; }
        public List<OpayoLog> TransactionShortList { get; set; }
        public IQueryable<TelerikOpayoToken> TokenList { get; set; }
        public DataTable OpayoTransactionEntry { get; set; }
        public int OpayoTransactionId { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public OpayoViewModel GetTransactions()
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            DataTable opayoData = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetOpayoTransactions", sqlParms, "data")
                .Tables[0];

            TransactionList = opayoData.AsEnumerable()
                .Select(r => new TelerikOpayoTransaction
                {
                    Id = int.Parse(r["Id"].ToString()),
                    DateTime = Convert.ToDateTime(r["DateTime"]),
                    Website = r["WebsiteName"].ToString(),
                    OrderNumber = r["OrderNumber"].ToString(),
                    Action = r["Action"].ToString(),
                    Name = GetJsonItem(r["Json"].ToString(), "name"),
                    Amount = GetJsonItem(r["Json"].ToString(), "amount"),
                    MerchantSessionKey = GetJsonItem(r["Json"].ToString(), "merchantSessionKey"),
                    Json = r["Json"].ToString(),
                    OrderIsPlaced = r["OrderIsPlaced"].ToString()
                })
                .AsQueryable();

            return this;
        }

        public void GetTransaction(int id, string merchantSessionKey)
        {
            TransactionShortList = _ctx.OpayoLog
                .Include(x => x.Lookup)
                .Where(x => x.MerchandiseSessionKey == merchantSessionKey)
                .OrderBy(x => x.Lookup.Sequence).ThenBy(x => x.DateTime)
                .ToList();
        }

        public void GetTokens()
        {
            TokenList = _ctx.SagePayTokens
                .Where(w => w.timestamp > _datefrom)
                .Select(x => new TelerikOpayoToken
                {
                    Id = x.id,
                    Account = x.account,
                    Email = x.email,
                    Uid = x.uid,
                    Website = (_ctx.Website.Where(w => w.WebsiteID == x.websiteID).FirstOrDefault().FriendlyName),
                    Token = x.token,
                    Date = x.timestamp
                })
                .AsQueryable();
        }

        public class TelerikOpayoTransaction
        {
            public int Id { get; set; }
            public DateTime DateTime { get; set; }
            public string Website { get; set; }
            public string OrderNumber { get; set; }
            public string Name { get; set; }
            public string Amount { get; set; }
            public string MerchantSessionKey { get; set; }
            public string Action { get; set; }
            public string Json { get; set; }
            public string OrderIsPlaced { get; set; }
        }

        public class TelerikOpayoToken
        {
            public int Id { get; set; }
            public string Account { get; set; }
            public string Email { get; set; }
            public string Uid { get; set; }
            public string Website { get; set; }
            public string Token { get; set; }
            public DateTime Date { get; set; }
        }

        //public SagePayTransactions GetProtxData(int protxID)
        //{
        //    SagePayTransactions tran;

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        tran = db.SagePayTransactions.Find(protxID);
        //    }
        //    return tran;
        //}

        public SagePayTokens GetCardDetails(int id)
        {
            SagePayTokens tokens;

            using (ngmdEntities db = new ngmdEntities())
            {
                tokens = db.SagePayTokens.Find(id);
            }
            return tokens;
        }

        public void CreateOpayoTransactionsCSVFile()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ImageSpecExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            GetTransactions();
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikOpayoTransaction transaction in TransactionList)
                {
                    InsertCSVData(writer, transaction);
                }
            }
        }
        
        private void InsertCSVData(CsvFileWriter writer, TelerikOpayoTransaction transaction)
        {
            CsvRow newRow = new CsvRow();

            newRow.Add(transaction.Id.ToString());
            newRow.Add(transaction.DateTime.ToString("dd/MM/yyyy"));
            newRow.Add(transaction.Website);
            newRow.Add(transaction.OrderNumber);
            newRow.Add(transaction.Name);
            newRow.Add(transaction.Amount);
            newRow.Add(transaction.Action);
            newRow.Add(transaction.Json);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Id");
            firstRow.Add("Date");
            firstRow.Add("Website");
            firstRow.Add("Order Number");
            firstRow.Add("Name");
            firstRow.Add("Amount");
            firstRow.Add("Action");
            firstRow.Add("Json");

            writer.WriteRow(firstRow);
        }

        private string GetJsonItem(string json, string item)
        {
            JObject jO = JsonConvert.DeserializeObject<JObject>(json);
            string val = "";

            if (item == "name")
            {
                val = jO["customerFirstName"]?.ToString() + " " + jO["customerLastName"]?.ToString();
            }
            if (item == "amount")
            {
                val = (Decimal.Parse((jO["amount"] ?? 0).ToString()) / 100).ToString("0.00");
            }
            if (item == "merchantSessionKey")
            {
                val = jO["paymentMethod"]["card"]["merchantSessionKey"].ToString();
            }

            return val == null ? "n/a" : val;
        }
    }

    public static class OpayoLogModelExtensions
    {
        public static IQueryable<OpayoViewModel.TelerikOpayoTransaction> AsTelerikViewModel(this IQueryable<OpayoLog> opayoQuery)
        {

            return opayoQuery.Select(o => new OpayoViewModel.TelerikOpayoTransaction
            {
                Id = o.OpayoLogId,
                DateTime = o.DateTime,
                Website = o.Website.FriendlyName,
                OrderNumber = o.OrderNumber,
                Action = o.Lookup.LookupName,
                Json = o.Json
            });
        }
    }
}
