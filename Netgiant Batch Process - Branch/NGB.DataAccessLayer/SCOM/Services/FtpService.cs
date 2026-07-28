using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class FtpService
    {
        string m_selectStatement = "SELECT ftpDetailID, ftpHost, ftpUser, ftpPassword, ftpFolder, ftpFilename, ftpZipFilename, dateLastUpdate, fileColumnHeader FROM ngmd.ftpDetails ";

        void readFtpDetails(SqlDataReader reader, FtpSE ftp)
        {
            ftp.FtpDetailID = reader.GetInt32(0);
            ftp.FtpHost = reader.GetString(1);
            ftp.FtpUser = reader.GetString(2);
            ftp.FtpPassword = reader.GetString(3);
            ftp.FtpFolder = reader.IsDBNull(4) ? "" : reader.GetString(4);
            ftp.FtpFilename = reader.GetString(5);
            ftp.FtpZipFilename = reader.GetString(6);
            ftp.DateLastUpdated = reader.GetDateTime(7);
            ftp.FileColumnHeader = reader.GetBoolean(8);
        }

        [AutoComplete]
        public FtpSE GetFtpDetailsByID(int id)
        {
            FtpSE ftp = new FtpSE();

            using (SqlConnection conn = new SqlConnection(SQLUtilities.GetMachineConnectionString("netgiantmasterdata")))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = m_selectStatement + "WHERE providerFK = @providerID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@providerID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, id));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        if (reader.Read())
                        {
                            readFtpDetails(reader, ftp);
                        }
                    }
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
            }

            return ftp;
        }
    }
}
