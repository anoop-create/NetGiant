using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace netGiant.Api.BusinessLayer.Shared
{
    public class StandardFunctions
    {
        public static List<manufacturer> GetEquipmentManufacturers()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.manufacturer.Where(x => x.equipmentManuName != null).ToList();
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

        public static SqlConnection OpenDatabase(string connString)
        {
            SqlConnection sqlConn = new SqlConnection(GetMachineConnectionString(connString));
            sqlConn.Open();
            return sqlConn;
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

        public static string GetMachineConnectionString(string connString)
        {
            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            return machineConfig.ConnectionStrings.ConnectionStrings[connString].ConnectionString.ToString();
        }
    }
}
