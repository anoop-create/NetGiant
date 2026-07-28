using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class StuckOrdersViewModel : CommonViewModel
    {
        public StuckOrdersViewModel()
        {

        }

        public List<Telerik> StuckOrdersList { get; set; }
        public StuckOrder StuckOrdersEntry { get; set; }

        public void GetStuckOrders()
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT 
                'TG' As [Website], 'tonergiant' As [DbName], WO.drf As [Ref], WO.cor As [OrderNumber], 
                WO.cus As [AccountNumber], WO.user_no As [UserNumber], WO.net As [Net], WO.vat As [Vat], 
                WO.email As [Email], WO.time As [Timestamp], WO.imp As [Imported]
                FROM tonergiant2.dbo.web_orders WO
                WHERE WO.imp = 0

                UNION

                SELECT 
                'CM' As [Website], 'cartridgemonkey' As [DbName], WO.drf As [Ref], WO.cor As [OrderNumber], 
                WO.cus As [AccountNumber], WO.user_no As [UserNumber], WO.net As [Net], WO.vat As [Vat], 
                WO.email As [Email], WO.time As [Timestamp], WO.imp As [Imported]
                FROM cartridgemonkey.dbo.web_orders WO
                WHERE WO.imp = 0

                UNION

                SELECT 
                'NG' As [Website], 'netgiant' As [DbName], WO.drf As [Ref], WO.cor As [OrderNumber], 
                WO.cus As [AccountNumber], WO.user_no As [UserNumber], WO.net As [Net], WO.vat As [Vat], 
                WO.email As [Email], WO.time As [Timestamp], WO.imp As [Imported]
                FROM netgiant.dbo.web_orders WO
                WHERE WO.imp = 0
 
                ORDER BY Timestamp DESC";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "stuck");
            dt = ds.Tables[0];

            StuckOrdersList = new List<Telerik>();
            foreach (DataRow dr in dt.Rows)
            {
                Telerik t = new Telerik
                {
                    Ref = Int64.Parse(dr["Ref"].ToString()),
                    Website = dr["Website"].ToString().Trim(),
                    DbName = dr["DbName"].ToString().Trim(),
                    OrderNumber = dr["OrderNumber"].ToString().Trim(),
                    AccountNumber = dr["AccountNumber"].ToString().Trim(),
                    UserNumber = dr["UserNumber"].ToString().Trim(),
                    Net = Convert.ToDecimal(dr["Net"]),
                    Vat = Convert.ToDecimal(dr["Vat"]),
                    Email = dr["Email"].ToString().Trim(),
                    Timestamp = Convert.ToDateTime(dr["Timestamp"]),
                    Imported = Int16.Parse(dr["Imported"].ToString())
                };
                StuckOrdersList.Add(t);
            }
        }

        public SaveReturn StuckOrderResolved(int id, string dbName)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            string sql = @"UPDATE dbo.web_orders 
                SET imp = 99
                WHERE drf = " + id.ToString();

            if (!SQLUtilities.ExecuteInlineProcedure(dbName, sql))
            {
                sr.IsSuccess = false;
            }

            return sr;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public SaveReturn StuckOrderUpdRecord()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            string sql = @"UPDATE dbo.web_orders 
                SET cus = '" +  StuckOrdersEntry.AccountNumber + @"',
                    user_no = '" + StuckOrdersEntry.UserNumber + @"'
                WHERE drf = " + StuckOrdersEntry.Ref.ToString();

            if (!SQLUtilities.ExecuteInlineProcedure(StuckOrdersEntry.DbName, sql))
            {
                sr.IsSuccess = false;
            }

            return sr;
        }

        public class Telerik
        {
            public long Ref { get; set; }
            public string Website { get; set; }
            public string DbName { get; set; }
            public string OrderNumber { get; set; }
            public string AccountNumber { get; set; }
            public string UserNumber { get; set; }
            public decimal Net { get; set; }
            public decimal Vat { get; set; }
            public string Email { get; set; }
            public DateTime Timestamp { get; set; }
            public int Imported { get; set; }
        }
    }

    public static class StuckOrdersModeExtensions
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

    public class StuckOrder
    {
        public long Ref { get; set; }
        public string Website { get; set; }
        public string DbName { get; set; }
        public string OrderNumber { get; set; }
        public string AccountNumber { get; set; }
        public string UserNumber { get; set; }
        public decimal Net { get; set; }
        public decimal Vat { get; set; }
        public string Email { get; set; }
        public DateTime Timestamp { get; set; }
        public int Imported { get; set; }
    }

}

