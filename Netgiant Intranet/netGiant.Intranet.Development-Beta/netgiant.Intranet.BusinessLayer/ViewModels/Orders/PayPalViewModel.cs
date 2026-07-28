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
    public class PayPalViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;
        private DateTime _datefrom;

        public PayPalViewModel()
        {
            _ctx = new ngmdEntities();
            _datefrom = DateTime.Now.AddYears(-1);
        }

        public IQueryable<TelerikPayPalTransaction> TransactionList { get; set; }
        public DataTable PayPalTransactionEntry { get; set; }
        public int PayPalTransactionId { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public void GetTransactions()
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            DataTable payPalData = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPayPalTransactions", sqlParms, "data")
                .Tables[0];

            // Convert DataTable to IQueryable
            TransactionList = payPalData.AsEnumerable()
                .Select(row => new TelerikPayPalTransaction
                {
                    Aut_Id = int.Parse(row["Aut_Id"].ToString()),
                    Aut_DateTime = Convert.ToDateTime(row["Aut_DateTime"]),
                    Aut_Action = row["Aut_Action"].ToString(),
                    Aut_AuthId = row["Aut_AuthId"].ToString(),
                    Aut_Response = row["Aut_Response"].ToString(),
                    Aut_Amount = GetJsonItem(row["Aut_Response"].ToString(), "amount"),
                    Aut_Payee = GetJsonItem(row["Aut_Response"].ToString(), "payee"),
                    Aut_Status = GetJsonItem(row["Aut_Response"].ToString(), "status"),
                    Aut_Website = GetWebsite(GetJsonItem(row["Aut_Response"].ToString(), "email").Substring(0,2).ToUpper()),
                    Cap_Id = int.Parse(row["Cap_Id"].ToString()),
                    Cap_DateTime = Convert.ToDateTime(row["Cap_DateTime"]),
                    Cap_Action = row["Cap_Action"].ToString(),
                    Cap_AuthId = row["Cap_AuthId"].ToString(),
                    Cap_CapId = GetJsonItem(row["Cap_Response"].ToString(), "id"),
                    Cap_Response = row["Cap_Response"].ToString(),
                    Cap_Status = GetJsonItem(row["Cap_Response"].ToString(), "status"),
                    Cap_PPProtection = GetJsonItem(row["Cap_Response"].ToString(), "protection"),
                    OrderIsPlaced = row["OrderIsPlaced"].ToString()
                })
                .AsQueryable();
        }
        public void GetTransaction(string id)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@Id", SqlDbType.VarChar);
            sqlParm.Value = id;
            sqlParms.Add(sqlParm);
            PayPalTransactionEntry = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPayPalTransactions", sqlParms, "data")
                .Tables[0];
        }

        public class TelerikPayPalTransaction
        {
            public int Aut_Id { get; set; }
            public DateTime Aut_DateTime { get; set; }
            public string Aut_Website { get; set; }
            public string Aut_Action { get; set; }
            public string Aut_AuthId { get; set; }
            public string Aut_Response { get; set; }
            public string Aut_Amount { get; set; }
            public string Aut_Payee { get; set; }
            public string Aut_Status { get; set; }
            public int Cap_Id { get; set; }
            public DateTime Cap_DateTime { get; set; }
            public string Cap_Action { get; set; }
            public string Cap_AuthId { get; set; }
            public string Cap_CapId { get; set; }
            public string Cap_Response { get; set; }
            public string Cap_Status { get; set; }
            public string Cap_PPProtection { get; set; }
            public string OrderIsPlaced { get; set; }
        }

        public SagePayTokens GetCardDetails(int id)
        {
            SagePayTokens tokens;

            using (ngmdEntities db = new ngmdEntities())
            {
                tokens = db.SagePayTokens.Find(id);
            }
            return tokens;
        }

        private string FindItem(string item, string detail)
        {
            if (detail.Contains(item))
            {
                string[] arr = detail.Split('&');
                string el = arr.First(x => x.StartsWith(item));
                if (!string.IsNullOrEmpty(el))
                {
                    return el.Split('=')[1];
                }
            }
            return "";
        }

        public void CreatePayPalTransactionsCSVFile()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ImageSpecExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            GetTransactions();
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikPayPalTransaction transaction in TransactionList)
                {
                    InsertCSVData(writer, transaction);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, TelerikPayPalTransaction transaction)
        {
            CsvRow newRow = new CsvRow();

            newRow.Add(transaction.Aut_Id.ToString());
            newRow.Add(transaction.Aut_DateTime.ToString("dd/MM/yyyy"));
            newRow.Add(transaction.Aut_Action);
            newRow.Add(transaction.Aut_AuthId);
            newRow.Add(transaction.Aut_Response);
            newRow.Add(transaction.Cap_Id.ToString());
            newRow.Add(transaction.Cap_DateTime.ToString("dd/MM/yyyy"));
            newRow.Add(transaction.Cap_Action);
            newRow.Add(transaction.Cap_AuthId);
            newRow.Add(transaction.Cap_Response);
            newRow.Add(transaction.OrderIsPlaced);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Aut_Id");
            firstRow.Add("Aut_DateTime");
            firstRow.Add("Aut_Action");
            firstRow.Add("Aut_AuthId");
            firstRow.Add("Aut_Response");
            firstRow.Add("Cap_Id");
            firstRow.Add("Cap_DateTime");
            firstRow.Add("Cap_Action");
            firstRow.Add("Cap_AuthId");
            firstRow.Add("Cap_Response");
            firstRow.Add("OrderIsPlaced");

            writer.WriteRow(firstRow);
        }

        private string GetJsonItem(string json, string item)
        {
            JObject jO = JsonConvert.DeserializeObject<JObject>(json);
            string val = "";

            if (item == "payee")
            {
                val = jO["purchase_units"]?.First()["shipping"]?["name"]?["full_name"]?.ToString();
                //val = jO["payer"]?["name"]?["given_name"]?.ToString();
                //val += " ";
                //val += jO["payer"]?["name"]?["surname"]?.ToString();
            }
            if (item == "amount")
            {
                val = jO["purchase_units"]?.First()["amount"]?["value"]?.ToString();
            }
            if (item == "status")
            {
                val = jO["status"]?.ToString();
            }
            if (item == "email")
            {
                val = jO["purchase_units"]?.First()["payee"]?["email_address"]?.ToString();
            }
            if (item == "protection")
            {
                val = jO["seller_protection"]?["status"]?.ToString();
            }
            if (item == "id")
            {
                val = jO["id"]?.ToString();
            }

            return val == null ? "n/a" : val;
        }

        private string GetWebsite(string prefix)
        {
            if (prefix == "TG" || prefix == "CM" || prefix == "NG")
            {
                return prefix;
            }

            return "TG";
        }
    }
}

