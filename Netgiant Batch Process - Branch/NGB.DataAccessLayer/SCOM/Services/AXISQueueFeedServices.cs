using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class AXISQueueFeedServices
    {
        public AXISQueueFeedServices()
        {
            connectionString = SQLUtilities.GetMachineConnectionString("netgiantmasterdata");
        }

        private string connectionString { get; }

        public DataTable GetAXISQueueFeedData()
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "ngmd.AXISQueueFeedData";
                    cmd.CommandTimeout = 3000;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            dt.Load(reader);
                            reader.Close();
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
            }

            return dt;
        }

        public static void ClearAxisQueueRecords(int daysOlderThan)
        {
            List<KeyValuePair<string, string>> parms = new List<KeyValuePair<string, string>>();
            parms.Add(new KeyValuePair<string,string>("daysOlderThan", daysOlderThan.ToString()));
            SQLUtilities.ExecuteSimpleStoredProcedure("netgiantmasterdata", "ngmd.ClearAxisQueueRecords", parms);
        }
    }
}
