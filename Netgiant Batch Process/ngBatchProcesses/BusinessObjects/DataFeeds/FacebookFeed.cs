using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class FacebookFeed
    {
        private Dictionary<string, string> _params;

        public FacebookFeed(Dictionary<string, string> parms)
        {
            _params = parms;
        }

        public List<product> products { get; set; }

        public void CreateProductFeed()
        {
            StandardFunctions.WriteProcessStarted();

            DataTable products = GetProducts(Convert.ToInt32(_params["subtype"]));

            WriteCSV(products, _params["output"]);

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private DataTable GetProducts(int websiteId)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@websiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            DataTable dt = new DataTable();
            try
            {
                dt = SQLUtilities.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetFacebookFeed_Product",
                    sqlParms, "FacebookData").Tables[0];
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.GetFacebookFeed_Product", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
            }

            return dt;
        }

        private void WriteCSV(DataTable dt, string filepath)
        {
            //Write the CSV file
            using (CsvFileWriter writer = new CsvFileWriter(filepath, ','))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    CsvRow firstRow = new CsvRow();

                    //Write details
                    foreach (DataColumn dc in dt.Columns)
                    {
                        firstRow.Add(dc.ColumnName);
                    }
                    writer.WriteRow(firstRow);

                    //Loop through the datatable and write a row in the csv file for each
                    foreach (DataRow dr in dt.Rows)
                    {
                        //Exclude records with blank images
                        if (dr["image_link"].ToString() != "")
                        {
                            CsvRow newRow = new CsvRow();
                            foreach (DataColumn dc in dt.Columns)
                            {
                                newRow.Add(dr[dc.ColumnName].ToString());
                            }
                            writer.WriteRow(newRow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing CSV File: " + filepath + ": " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }
        }
    }
}
