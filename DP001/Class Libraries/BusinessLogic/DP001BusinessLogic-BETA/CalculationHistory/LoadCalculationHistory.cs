using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DP001BusinessLogic
{
    public class LoadCalculationHistory
    {
        public LoadCalculationHistory(Dictionary<string, string> parms)
        {
            _suppliedParams = parms;
            InitializeTenant();
        }

        private readonly Dictionary<string, string> _suppliedParams;
        private Tenant _tenant;
        private Channel _channel;
        private static bool _errorOccured;
        private static List<DownloadedFileData> _feedFiles;

        public bool Load()
        {
            LoadCalculationHistoryData();
            return _errorOccured;
        }

        private void LoadCalculationHistoryData()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CreateCalculationHistory", "Information");

            try
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
                sqlParm1.Value = _channel.ChannelID;
                sqlParms.Add(sqlParm1);
                var isSuccess = SQL.ExecuteStoredProcedure("DP001", "CreateCalculationHistory", sqlParms, _channel.ChannelID);

                if (!isSuccess)
                    CommonDataFunctions.CreateLogEntry(_channel, "Unable to complete process due to errors found. Please contact support.", "Notification");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to complete CreateCalculationHistory. Error: " +
                                                             e.Message + " Stack: " + e.StackTrace, "Error");
                _errorOccured = true;
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CreateCalculationHistory", "Information");
        }

        private void InitializeTenant()
        {
            try
            {
                _tenant = new Tenant();
                _channel = _tenant.GetChannelRecord(Convert.ToInt32(_suppliedParams["channelid"]));
                _tenant.SetupTenantDelegates(_channel);
                _feedFiles = new List<DownloadedFileData>();

                _errorOccured = false;
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to initialize tenant. Error: " +
                                                             e.Message + " Stack: " + e.StackTrace, "Error");
                _errorOccured = true;
            }
        }
    }
}
