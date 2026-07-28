using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace NGBP.DataAccessLayer.DataUtilities
{
    public class SQLUtilitiesDyn : IDisposable
    {
        public SQLUtilitiesDyn()
        {
            Messages = new List<string>();
        }
        public List<string> Messages { get; set; }

        public DataSet ExecuteReadStoredProcedure(string connString,
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

                    sqlConn.FireInfoMessageEventOnUserErrors = true;
                    sqlConn.InfoMessage += new SqlInfoMessageEventHandler(CaptureInfoMessage);
                    cmd.StatementCompleted += OnStatementCompleted;

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
                        catch (SqlException e)
                        {
                            throw new ApplicationException(e.Message + "\n");
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

        public string GetMachineConnectionString(string connString)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[connString].ConnectionString;
        }

        private void CaptureInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            Messages.Add(e.Message);
        }

        private void OnStatementCompleted(object sender, StatementCompletedEventArgs e)
        {
            Messages.Add("Record Count: " + e.RecordCount);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
        }
    }
    public static class SQLUtilities
    {
        public static string GetMachineConnectionString(string connString)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[connString].ConnectionString;
        }

        private static SqlConnection OpenDatabase(string connString)
        {
            SqlConnection sqlConn = new SqlConnection(GetMachineConnectionString(connString));
            sqlConn.Open();
            return sqlConn;
        }

        private static void CloseDatabase(SqlConnection sqlConn)
        {
            sqlConn.Close();
        }

        private static SqlCommand SetupStoredProcedureCommand(string spName, SqlConnection sqlConn, int timeoutWait = -1)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = spName;
            cmd.Connection = sqlConn;

            if (timeoutWait > -1)
            {
                cmd.CommandTimeout = timeoutWait;
            }

            return cmd;
        }
        /// <summary>
        /// This executes a stored procedure that doesn't doesn't return any data. It reports success/failure
        /// </summary>
        /// <param name="connString">The connection string name used to open a sql connection, from machine config</param>
        /// <param name="SPName">The name of the stored procedure</param>
        /// <param name="parms">SQL parameters</param>
        /// <param name="timeoutWait">SQL timeout in seconds</param>
        /// 
        public static bool ExecuteStoredProcedure(string connString,
                                                    string storedProcedureName,
                                                    List<SqlParameter> parms,
                                                    int timeoutWait = 200)
        {
            bool isSuccess = true;
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
                }
                catch (Exception e)
                {
                    isSuccess = false;
                }
            }
            return isSuccess;
        }

        public static void ExecuteSimpleStoredProcedure(string connString, string SPName, int timeoutWait = -1)
        {
            try
            {
                SqlConnection sqlConn = OpenDatabase(connString);
                SqlCommand cmd = SetupStoredProcedureCommand(SPName, sqlConn, timeoutWait);
                cmd.ExecuteNonQuery();

                CloseDatabase(sqlConn);
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }

        public static void ExecuteSimpleStoredProcedure(string connString, string SPName,
                                                            List<KeyValuePair<string, string>> args, int timeoutWait = -1)
        {
            try
            {
                SqlConnection sqlConn = OpenDatabase(connString);
                SqlCommand cmd = SetupStoredProcedureCommand(SPName, sqlConn, timeoutWait);

                foreach (KeyValuePair<string, string> arg in args)
                {
                    SqlParameter param = new SqlParameter("@" + arg.Key, SqlDbType.VarChar, 200);
                    param.Value = arg.Value;
                    cmd.Parameters.Add(param);
                }

                cmd.ExecuteNonQuery();

                CloseDatabase(sqlConn);
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }

        /// <summary>
        /// This executes a stored procedure which returns results. The results are returned in a data table
        /// </summary>
        /// <returns></returns>
        public static DataTable ExecuteStoredProcedureQuery(string connString, string SPName, int timeout = 1000)
        {
            try
            {
                SqlConnection sqlConn = OpenDatabase(connString);
                SqlCommand cmd = SetupStoredProcedureCommand(SPName, sqlConn);
                cmd.CommandTimeout = timeout;

                //sqlConn.InfoMessage += new SqlInfoMessageEventHandler(CaptureInfoMessage);
                //cmd.StatementCompleted += OnStatementCompleted;

                SqlDataReader reader = cmd.ExecuteReader();
                DataTable results = new DataTable();

                results.Load(reader);

                CloseDatabase(sqlConn);

                return results;
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }

        public static DataTable ExecuteStoredProcedureQuery(string connString, string SPName,
                                                                List<KeyValuePair<string, string>> args, int timeout = 1000)
        {
            try
            {
                using (SqlConnection sqlConn = OpenDatabase(connString))
                using (SqlCommand cmd = SetupStoredProcedureCommand(SPName, sqlConn))
                {
                    cmd.CommandTimeout = timeout;

                    foreach (KeyValuePair<string, string> arg in args)
                    {
                        SqlParameter param = new SqlParameter("@" + arg.Key, SqlDbType.VarChar, 200);
                        param.Value = arg.Value;
                        cmd.Parameters.Add(param);
                    }

                    SqlDataReader reader = cmd.ExecuteReader();
                    DataTable results = new DataTable();

                    results.Load(reader);

                    return results;
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }

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
                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
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
                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
                        }
                    }
                }

            }
            return ds;
        }

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

        private static void CaptureInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            var x = e.Message;
        }
        private static void OnStatementCompleted(object sender, StatementCompletedEventArgs e)
        {
            var x = e.RecordCount;
        }
    }
}
