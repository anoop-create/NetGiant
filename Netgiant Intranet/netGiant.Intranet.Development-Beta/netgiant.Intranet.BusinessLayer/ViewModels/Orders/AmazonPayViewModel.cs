using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Orders
{
    public class AmazonPayViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;
        private DateTime _datefrom;

        public AmazonPayViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikAmazonPayTransaction> TransactionList { get; set; }
        public DataTable AmazonPayTransactionEntry { get; set; }
        public int AmazonPayTransactionId { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public void GetTransactions()
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            DataTable amazonPayData = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetAmazonPayTransactions", sqlParms, "data")
                .Tables[0];

            // Convert DataTable to IQueryable
            TransactionList = amazonPayData.AsEnumerable()
                .Select(row => new TelerikAmazonPayTransaction
                {
                    Aut_Id = int.Parse(row["Aut_Id"].ToString()),
                    Aut_DateTime = Convert.ToDateTime(row["Aut_DateTime"]),
                    Aut_Action = row["Aut_Action"].ToString(),
                    Aut_Status = GetJsonItem(row["Aut_Response"].ToString(), "status"),
                    Aut_SessionId = row["Aut_SessionId"].ToString(),
                    Aut_Response = row["Aut_Response"].ToString(),
                    Aut_Amount = GetJsonItem(row["Aut_Response"].ToString(), "amount"),
                    Aut_Payee = GetJsonItem(row["Aut_Response"].ToString(), "customer"),
                    Aut_Postcode = GetJsonItem(row["Aut_Response"].ToString(), "postcode"),
                    Aut_Website = GetWebsite(int.Parse(row["Aut_WebsiteId"].ToString())),
                    Cap_Action = row["Cap_Id"].ToString() == "" ? "" : row["Cap_Action"]?.ToString(),
                    Cap_Status = row["Cap_Id"].ToString() == "" ? "" : GetJsonItem(row["Cap_Response"].ToString(), "status"),
                    Cap_ChargeId = row["Cap_Id"].ToString() == "" ? "" : GetJsonItem(row["Cap_Response"].ToString(), "chargeid"),
                    Cap_Response = row["Cap_Response"]?.ToString()
                })
                //.Where(x => x.Aut_Amount != "")
                .AsQueryable();
        }

        public void GetTransaction(string id)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@Id", SqlDbType.VarChar);
            sqlParm.Value = id;
            sqlParms.Add(sqlParm);
            AmazonPayTransactionEntry = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetAmazonPayTransactions", sqlParms, "data")
                .Tables[0];
        }

        public class TelerikAmazonPayTransaction
        {
            public int Aut_Id { get; set; }
            public DateTime Aut_DateTime { get; set; }
            public string Aut_Action { get; set; }
            public string Aut_Status { get; set; }
            public string Aut_SessionId { get; set; }
            public string Aut_Response { get; set; }
            public string Aut_Amount { get; set; }
            public string Aut_Payee { get; set; }
            public string Aut_Postcode { get; set; }
            public string Aut_Website { get; set; }
            public int Cap_Id { get; set; }
            public DateTime Cap_DateTime { get; set; }
            public string Cap_Action { get; set; }
            public string Cap_Status { get; set; }
            public string Cap_ChargeId { get; set; }
            public int Cap_Website { get; set; }
            public string Cap_SessionId { get; set; }
            public string Cap_Response { get; set; }
        }

        public void CreateAmazonPayTransactionsCSVFile()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\AmazonPayLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            GetTransactions();
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikAmazonPayTransaction transaction in TransactionList)
                {
                    InsertCSVData(writer, transaction);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, TelerikAmazonPayTransaction transaction)
        {
            CsvRow newRow = new CsvRow
            {
                transaction.Aut_SessionId,
                transaction.Aut_Id.ToString(),
                transaction.Aut_DateTime.ToString(),
                transaction.Aut_Website,
                transaction.Aut_Action,
                transaction.Aut_Amount,
                transaction.Aut_Payee,
                transaction.Aut_Postcode,
                transaction.Cap_Id.ToString(),
                transaction.Cap_DateTime.ToString(),
                transaction.Cap_Action,
                transaction.Cap_ChargeId
                };

            writer.WriteRow(newRow);
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("SessionId");
            firstRow.Add("Aut_Id");
            firstRow.Add("Aut_DateTime");
            firstRow.Add("Aut_Website");
            firstRow.Add("Aut_Action");
            firstRow.Add("Aut_RAmount");
            firstRow.Add("Aut_Payee");
            firstRow.Add("Aut_Postcode");
            firstRow.Add("Cap_Id");
            firstRow.Add("Cap_DateTime");
            firstRow.Add("Cap_Action");
            firstRow.Add("Cap_ChargeId");

            writer.WriteRow(firstRow);
        }

        private string GetWebsite(int websiteId)
        {
            switch (websiteId)
            {
                case 1:
                    {
                        return "TG";
                    }
                case 2:
                    {
                        return "CM";
                    }
            }
            return "NG";
        }

        private string GetJsonItem(string json, string item)
        {
            JObject jO = JsonConvert.DeserializeObject<JObject>(json);
            string val = "";

            if (item == "customer")
            {
                val = "";
                if (jO["buyer"].ToString() != "")
                {
                    val = jO["buyer"]?["name"]?.ToString();
                }
            }
            if (item == "amount")
            {                
                val = "";
                if (jO["paymentDetails"].ToString() != "")
                {
                    val = jO["paymentDetails"]?["chargeAmount"]?["amount"]?.ToString();
                }
            }
            if (item == "postcode")
            {
                val = "";
                if (jO["shippingAddress"].ToString() != "")
                {
                    val = jO["shippingAddress"]?["postalCode"]?.ToString();
                }
            }
            if (item == "chargeid")
            {
                val = jO["chargeId"]?.ToString();
            }
            if (item == "status")
            {
                val = jO["statusDetails"]?["state"]?.ToString();
            }

            return val == null ? "n/a" : val;
        }
    }
}


