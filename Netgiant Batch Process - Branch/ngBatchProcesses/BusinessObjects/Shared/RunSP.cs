using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class RunSP
    {
        public static void ExecuteStoredProcedure(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;
            List<SqlParameter> sqlparameters = new List<SqlParameter>();

            if (parms.ContainsKey("spparams"))
            {
                List<Tuple<string, string, string>> spparams = parms["spparams"].Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
                                                                                     .Select(x => x.Split('~'))
                                                                                     .Select(x => new Tuple<string, string, string>(x[0], x[1], x[2]))
                                                                                     .ToList();

                for (int i = 0; i < spparams.Count; i++)
                {
                    SqlParameter param = new SqlParameter(spparams[i].Item1, GetSQLDataType(spparams[i].Item3));
                    param.Value = spparams[i].Item2;
                    sqlparameters.Add(param);
                }
            }

            try
            {
                SQLUtilities.ExecuteStoredProcedure(parms["dbname"], parms["subtype"], sqlparameters);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog($"**ERROR** Occured executing stored procedure - {parms["subtype"]} on database {parms["dbname"]}");
                stnFunc.ProcessException(ex);
                errorHasOccurred = true;
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }
        }

        private static SqlDbType GetSQLDataType(string type)
        {
            return Enum.TryParse(type, out SqlDbType SQLType) ? SQLType : SqlDbType.VarChar;
        }
    }
}
