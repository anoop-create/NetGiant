using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Utilities
{
    public class SQL
    {
        public static DataSet ExecuteReadStoredProcedure(string connString, string storedProcedureName,
                                                                List<SqlParameter> parms, string datasetName = "defaultDataSet",
                                                                int timeoutWait = 20)
        {
            DataSet ds = new DataSet();

            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings[connString].ToString()))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = storedProcedureName;
                    cmd.Connection = sqlConn;

                    if (timeoutWait > 0)
                    {
                        cmd.CommandTimeout = timeoutWait;
                    }
                    foreach (SqlParameter parm in parms)
                    {
                        cmd.Parameters.Add(parm);
                    }

                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        try
                        {
                            adapter.Fill(ds, datasetName);
                        }
                        catch (Exception e)
                        {
                            throw new ApplicationException(e.Message + "\n" + e.StackTrace);
                        }
                    }
                }
                    
            }
            return ds;
        }

        public static bool ExecuteStoredProcedure(string connString, string storedProcedureName,
                                                                List<SqlParameter> parms,
                                                                int channelFK, 
                                                                int timeoutWait = 200)
        {
            bool isSuccess = false;

            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings[connString].ToString()))
            {
                sqlConn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = storedProcedureName;
                cmd.Connection = sqlConn;

                if (timeoutWait > 0)
                {
                    cmd.CommandTimeout = timeoutWait;
                }
                foreach (SqlParameter parm in parms)
                {
                    cmd.Parameters.Add(parm);
                }
                try
                {
                    cmd.ExecuteNonQuery();
                    isSuccess = true;
                }
                catch (Exception e)
                {
                    CommonDataFunctions.CreateLogEntry(0, channelFK, "**ERROR in SP** " + storedProcedureName, "Error", true);
                    CommonDataFunctions.CreateLogEntry(0, channelFK, e.Message + e.InnerException?.ToString() + e.StackTrace, "Error", true);
                }
            }
            return isSuccess;
        }

        private static string GetMachineConnectionString(string name)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[name].ConnectionString.ToString();
        }

        public static void SQLBulkInsert(DataTable dt, string connectionStringName)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(ConfigurationManager.ConnectionStrings[connectionStringName].ToString()))
            {
                bulkCopy.BulkCopyTimeout = 600; // in seconds
                bulkCopy.DestinationTableName = dt.TableName;
                bulkCopy.WriteToServer(dt);
            }
        }

        //public static void SQLBulkUpdate(BulkUpdateCriteria criteria)
        //{
        //    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings[criteria.ConnectionString].ToString()))
        //    {
        //        using (SqlCommand cmd = new SqlCommand(criteria.SPName))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Connection = con;
        //            cmd.CommandTimeout = 180;
        //            cmd.Parameters.AddWithValue("ChannelFK", criteria.ChannelFK);
        //            cmd.Parameters.AddWithValue(criteria.ParameterName, criteria.Data);
        //            con.Open();
        //            cmd.ExecuteNonQuery();
        //            con.Close();
        //        }
        //    }
        //}
    }

    //public class BulkUpdateCriteria
    //{
    //    public DataTable Data { get; set; }
    //    public string ConnectionString { get; set; }
    //    public string ParameterName { get; set; }
    //    public string SPName { get; set; }
    //    public int ChannelFK { get; set; }
    //}
}
