using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DataAccess.Utilities
{
    public class SQL
    {
        /// <summary>
        /// Executes stored procedure and returns a DataSet
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="parms"></param>
        /// <param name="datasetName"></param>
        /// <param name="timeoutWait"></param>
        /// <returns></returns>                 
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

        /// <summary>
        /// Executes inline SQL and returns DataSet
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="sqlStatement"></param>
        /// <param name="datasetName"></param>
        /// <param name="timeoutWait"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Executes stored procedure and returns bool
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="storedProcedureName"></param>
        /// <param name="parms"></param>
        /// <param name="timeoutWait"></param>
        /// <returns></returns>
        public static bool ExecuteStoredProcedure(string connString, string storedProcedureName,
                                                                List<SqlParameter> parms,
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
                    throw new ApplicationException(e.Message + "\n" + e.StackTrace);
                    //    CommonDataFunctions.CreateLogEntry(0, channelFK, "**ERROR in SP** " + storedProcedureName, "Error", true);
                    //    CommonDataFunctions.CreateLogEntry(0, channelFK, e.Message + e.InnerException?.ToString() + e.StackTrace, "Error", true);
                }
            }
            return isSuccess;
        }

        /// <summary>
        /// Executes inline SQL and returns bool
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="sql"></param>
        /// <param name="timeoutWait"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Executes inline SQL and encapsulates it within a sql transaction and returns bool
        /// </summary>
        /// <param name="connString"></param>
        /// <param name="sql"></param>
        /// <param name="timeoutWait"></param>
        /// <returns></returns>
        public static bool ExecuteInlineTransaction(string connString, string sqlStatement,
            int timeoutWait = 200)
        {
            bool isSuccess;

            try
            {

                var finalStatement = @" BEGIN TRANSACTION [Tran1] 
                                        BEGIN TRY" + 
                                            sqlStatement + 
                                     @" COMMIT TRANSACTION [Tran1]
                                        END TRY
                                        BEGIN CATCH
                                        ROLLBACK TRANSACTION[Tran1]
                                        END CATCH ";


                using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings[connString].ToString()))
                {
                    using (var cmd = new SqlCommand(finalStatement, sqlConn))
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

        /// <summary>
        /// Executes SQL bulk insert
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="connectionStringName"></param>
        public static void SQLBulkInsert(DataTable dt, string connectionStringName)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(ConfigurationManager.ConnectionStrings[connectionStringName].ToString()))
            {
                bulkCopy.BulkCopyTimeout = 600; // in seconds
                bulkCopy.DestinationTableName = dt.TableName;
                bulkCopy.WriteToServer(dt);
            }
        }

        /// <summary>
        /// Gets the connection string
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetMachineConnectionString(string name)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[name].ConnectionString.ToString();
        }
    }
}

