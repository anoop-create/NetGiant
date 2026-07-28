using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using DP001BusinessLogic.Shared;

namespace DP001Batch.BusinessObjects.Shared
{
    class StandardFunctions
    {
        public static void TruncateTable(string tablename, string fieldname, int period, int debug)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Start of TruncateTable for " + tablename + ':' + fieldname + ':' + period.ToString(), "Information", true);
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@TableName", SqlDbType.VarChar);
            sqlParm1.Value = tablename;
            sqlParms.Add(sqlParm1);
            SqlParameter sqlParm2 = new SqlParameter("@FieldName", SqlDbType.VarChar);
            sqlParm2.Value = fieldname;
            sqlParms.Add(sqlParm2);
            SqlParameter sqlParm3 = new SqlParameter("@RetentionPeriod", SqlDbType.Int);
            sqlParm3.Value = period;
            sqlParms.Add(sqlParm3);
            SqlParameter sqlParm4 = new SqlParameter("@Debug", SqlDbType.Int);
            sqlParm4.Value = debug;
            sqlParms.Add(sqlParm4);
            if (SQL.ExecuteStoredProcedure("DP001", "TruncateTable", sqlParms, 0))
            {
                CommonDataFunctions.CreateLogEntry(0, 0, "Table successfully truncated" + tablename, "Information", true);
            }
            else
            {
                CommonDataFunctions.CreateLogEntry(0, 0, "Table could not be truncated" + tablename, "Error", true);
            }
            CommonDataFunctions.CreateLogEntry(0, 0, "End of TruncateTable for " + tablename + ':' + fieldname + ':' + period.ToString(), "Information", true);
        }

        public static void GeneralMaintenance(Dictionary<string, string> parms)
        {
            if (parms["period"] == "d")
            {
                // Tenant Checks
                // 1. Check all ftp connections are valid
                // 2. Check if subscription is about to expire 

                // Cleanup SQL Backups on the Azure Blob Storage
                CommonFunctions.CleanupSqlBackupFiles();
            }

            if (parms["period"] == "m")
            {
                // Truncate Log, TenantAudit
                TruncateTable("Log", "DateTime", -180, Int32.Parse(parms["debug"]));
                TruncateTable("TenantAudit", "Timestamp", -180, Int32.Parse(parms["debug"]));
                // Delete Old Tenants
            }
        }

        public static void LogApplicationInsightsException(Exception e)
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                TelemetryConfiguration.Active.InstrumentationKey = "632f1bbf-373e-4e2a-b176-4c9e62d0ac00";

                var telemClient = new TelemetryClient();
                telemClient.TrackException(e);
                telemClient.Flush();
            }
        }
    }
}
