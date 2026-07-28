using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class IISLog
    {
        public void Analyse()
        {
            StandardFunctions.WriteProcessStarted();

            //Load log in DataTable
            DataTable dt1 = CsvUtilities.LoadCsvInDataTable("C:\\zz\\u_ex201201_x.log", ' ', 5, 0);

            //Find unique ip's
            //DataTable dt2 = dt1.DefaultView.ToTable(true, "X-Forwarded-IP");
            //var ipArray = dt1
            //    .Select(r => new { col1 = r[3], col2 = r[14] })
            //    .Distinct()
            //    .Where(x => x[3] == "search-results"));

            var ipArray = (from DataRow r in dt1.Rows
                           select new { col1 = r[3], col2 = r[14] })
                                .Distinct();
            //.Where(x => x.col1. == "search-results");
            //.ToList();

            //Get list of Org Names

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
