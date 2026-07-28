using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using NGBP.DataAccessLayer.DataUtilities;
using System.Data;
using System.EnterpriseServices;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class ProviderServices
    {
        public ProviderServices()
        {
            FtpService = new FtpService();
            connectionString = SQLUtilities.GetMachineConnectionString("netgiantmasterdata");
        }

        public ProviderServices(int providerTypeFK)
            : this()
        {
            m_selectStatement = m_selectStatement + " WHERE pt.providerTypeID = " + providerTypeFK + " AND p.[active] = 1";
        }

        public FtpService FtpService { get; set; }
        public string connectionString { get; set; }

        string m_selectStatement = "SELECT	p.providerID, p.providerName, p.providerDesc, pt.providerTypeID, pt.providerTypeName, pt.dateLastUpdate " +
                                    "FROM	ngmd.provider p " +
		                                    "INNER JOIN ngmd.providerType pt " +
			                                    "ON pt.providerTypeID = p.providerTypeFK ";

        void readProvider(SqlDataReader reader, ProviderSE provider)
        {
            provider.ProviderID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            provider.ProviderName = reader.IsDBNull(1) ? "" : reader.GetString(1);
            provider.ProviderDescription = reader.IsDBNull(2) ? "" : reader.GetString(2);
            provider.ProviderType = new ProviderTypeSE() 
            {
                ProviderTypeID = reader.GetInt32(3), 
                ProviderTypeName = reader.GetString(4),
                DateLastUpdate = reader.GetDateTime(5)
            };
            provider.RelatedFtpDetails = FtpService.GetFtpDetailsByID(provider.ProviderID);
        }

        public List<ProviderSE> GetAllProviders()
        {
            List<ProviderSE> providers = new List<ProviderSE>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = m_selectStatement;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                ProviderSE entity = new ProviderSE();
                                readProvider(reader, entity);

                                providers.Add(entity);
                            }
                        }

                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                    }
                }

                return providers;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        [AutoComplete]
        public List<KeyValuePair<string, string>> GetFieldMappingsByProvider(int providerID)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT fieldMappingID, fieldMappingTo, fieldMappingWith FROM ngmd.fieldMapping WHERE providerFK = @ProviderID;";

                cmd.Parameters.Add(new SqlParameter(
                    "@ProviderID", SqlDbType.Int, 0, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, providerID));

                if (conn.State == ConnectionState.Closed) conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    try
                    {
                        //dt.Load(reader);
                        while (reader.Read())
                        {
                            list.Add(new KeyValuePair<string, string>(reader.GetString(1), reader.GetString(2)));
                        }

                        reader.Close();
                    }

                    catch (Exception ex)
                    {
                        throw new ApplicationException(ex.Message);
                    }
                }
            }

            return list;
        }

        [AutoComplete]
        public void CopyProviderData(string csvFilePath)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.CopyProviderData";
                cmd.CommandTimeout = 1000;

                cmd.Parameters.Add(new SqlParameter(
                    "@ProviderCSV", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, csvFilePath));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
            }
        }

        public List<mfpnExtensions> GetAllMFPNExtensions()
        {
            List<mfpnExtensions> list = new List<mfpnExtensions>();

            string extensionsSelectStatement = "SELECT manuID, extension FROM ngmd.mfpnExtensions";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = extensionsSelectStatement;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                mfpnExtensions entity = new mfpnExtensions();
                                readExtensions(reader, entity);

                                list.Add(entity);
                            }
                        }

                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        void readExtensions(SqlDataReader reader, mfpnExtensions extensions)
        {
            extensions.ManuID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            extensions.Extension = reader.IsDBNull(1) ? "" : reader.GetString(1);
        }

        public List<manufacturer> GetAllManufacturers()
        {
            List<manufacturer> list = new List<manufacturer>();

            string manuSelectStatement = "SELECT manufacturerID, manufacturerName FROM ngmd.manufacturer";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = manuSelectStatement;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                manufacturer entity = new manufacturer();
                                readManufacturer(reader, entity);

                                list.Add(entity);
                            }
                        }

                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        void readManufacturer(SqlDataReader reader, manufacturer man)
        {
            man.ManuID = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            man.ManufacturerName = reader.IsDBNull(1) ? "" : reader.GetString(1);
        }

        public List<SupplierManuMapping> GetAllSupplierManuMappings()
        {
            List<SupplierManuMapping> list = new List<SupplierManuMapping>();

            string manuSelectStatement = "SELECT supplierManuRef, manufacturerFK, providerFK FROM ngmd.supplierManuMapping";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = manuSelectStatement;

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        try
                        {
                            while (reader.Read())
                            {
                                SupplierManuMapping entity = new SupplierManuMapping();
                                readSupplierManuMapping(reader, entity);

                                list.Add(entity);
                            }
                        }

                        catch (Exception ex)
                        {
                            throw new ApplicationException(ex.Message);
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        void readSupplierManuMapping(SqlDataReader reader, SupplierManuMapping supMap)
        {
            supMap.SupplierManuRef = reader.IsDBNull(0) ? "" : reader.GetString(0);
            supMap.ManufacturerFK = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            supMap.ProviderFK = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
        }

        public static void UpdateProviderFeedDateTime(string providerFK, string providerFeedDateTime)
        {
            try
            {
                List<KeyValuePair<string, string>> parms = new List<KeyValuePair<string, string>>();
                parms.Add(new KeyValuePair<string, string>("feedDateTime", providerFeedDateTime));
                parms.Add(new KeyValuePair<string, string>("providerFK", providerFK));

                SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.UpdateProviderFeedTime", parms);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("**Error** - Could not update the provider feed file datetime", ex);
            }
        }

        public static void UpdateSupplierStockQuantity()
        {
            try
            {
                SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.UpdateSupplierStockQuantity");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("**Error** - Could not update the supplier stock quantities", ex);
            }
        }

        public static void SetProvidersAlertStatus()
        {
            try
            {
                SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.SetProvidersAlertStatus");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("**Error** - Could not set the providers alert status", ex);
            }
        }
    }
}
