using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.OleDb;

namespace NGSBP.DataAccessLayer.DataUtilities
{
    public class SQLUtilities
    {
        public static string GetMachineConnectionString(string connString)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[connString].ConnectionString.ToString();
        }
        public static SqlConnection OpenDatabase(string connString)
        {
            SqlConnection sqlConn = new SqlConnection(GetMachineConnectionString(connString));
            sqlConn.Open();
            return sqlConn;
        }
        public static void CloseDatabase(SqlConnection sqlConn)
        {
            sqlConn.Close();
        }
        public static SqlCommand SetupStoredProcedureCommand(string spName, SqlConnection sqlConn, int timeoutWait = -1)
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
        /// This executes a stored procedure that doesn't accept any parameters and doesn't return any data
        /// </summary>
        /// <param name="connString">The connection string name used to open a sql connection, from machine config</param>
        /// <param name="SPName">The name of the stored procedure</param>
        /// <param name="timeoutWait">SQL timeout in seconds</param>
        public static void ExecuteSimpleStoredProcedure(string connString, string SPName, int timeoutWait = -1)
        {
            try
            {
                SqlConnection sqlConn = OpenDatabase(connString);
                SqlCommand cmd = SetupStoredProcedureCommand(SPName, sqlConn, timeoutWait);
                Int32 rowsAffected;

                rowsAffected = cmd.ExecuteNonQuery();

                CloseDatabase(sqlConn);
            }
            catch (Exception e)
            {
                throw (new ApplicationException(e.Message));
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

                Int32 rowsAffected;

                rowsAffected = cmd.ExecuteNonQuery();

                CloseDatabase(sqlConn);
            }
            catch (Exception e)
            {
                throw (new ApplicationException(e.Message));
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
                SqlDataReader reader = cmd.ExecuteReader();
                DataTable results = new DataTable();

                results.Load(reader);

                CloseDatabase(sqlConn);

                return results;
            }
            catch (Exception e)
            {
                throw (new ApplicationException(e.Message));
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
            catch (Exception e)
            {
                throw (new ApplicationException(e.Message));
            }
        }
    }
}
