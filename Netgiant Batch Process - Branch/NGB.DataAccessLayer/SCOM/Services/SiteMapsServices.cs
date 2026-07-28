using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    public class SiteMapsServices
    {
        public SiteMapsServices()
        {
            ConnectionString = SQLUtilities.GetMachineConnectionString("netgiantmasterdata");
        }

        private string ConnectionString { get; }

        [AutoComplete]
        public DataSet GetSiteMapsData(int websiteID)
        {
            DataSet ds = new DataSet();

            try
            {
                using(var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "ngmd.GetSiteMapsData";
                    cmd.CommandTimeout = 3000;

                    cmd.Parameters.Add(new SqlParameter(
                        "@websiteID", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, websiteID));
                    
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    try
                    {
                        adapter.Fill(ds, "sitemaps");
                    }
                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
            }

            return ds;
        }
    }
}
