using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class SQLUtilities
    {
        public static DataSet ExecuteReadInline(string connString,
            string sqlStatement,
            string datasetName = "defaultDataSet",
            int timeoutWait = 20)
        {
            DataSet ds = new DataSet();

            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings[connString].ToString()))
            {
                using (var cmd = new SqlCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sqlStatement;
                    cmd.Connection = sqlConn;

                    if (timeoutWait > 0)
                    {
                        cmd.CommandTimeout = timeoutWait;
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

        public static bool ExecuteInlineProcedure(string connString, string sqlStatement,
            int timeoutWait = 200)
        {
            bool isSuccess = false;

            try
            {
                using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings[connString].ToString()))
                {
                    using (var cmd = new SqlCommand(sqlStatement, sqlConn))
                    {
                        cmd.Connection.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                isSuccess = true;
            }
            catch (Exception e)
            {
                isSuccess = false;
                throw new ApplicationException(e.Message + "\n" + e.StackTrace);
            }

            return isSuccess;
        }

        public static DataSet ExecuteReadStoredProcedure(string connString,
            string storedProcedureName,
            List<SqlParameter> parms,
            string datasetName = "defaultDataSet",
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
    }
}
