using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class SKUMappingServices
    {   
        [AutoComplete]
        public static void CreateSKUMappings(string csvFilePath)
        {
            using (SqlConnection conn = new SqlConnection(SQLUtilities.GetMachineConnectionString("netgiantmasterdata")))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.CreateSKUMappings";
                cmd.CommandTimeout = 1000;

                cmd.Parameters.Add(new SqlParameter(
                    "@CSVPath", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, csvFilePath));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                catch (Exception e)
                {
                    throw new ApplicationException(e.Message);
                }
            }
        }
    }
}
