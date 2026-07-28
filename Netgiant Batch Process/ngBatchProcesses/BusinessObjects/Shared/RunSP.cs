using netGiant.Intranet.DataLayer.NetgiantMasterData;
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
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            List<SqlParameter> sqlparameters = new List<SqlParameter>();
            bool isSuccess = false;

            if (parms.ContainsKey("spparams"))
            {
                List<Tuple<string, string, string>> spparams = parms["spparams"].Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
                                                                                     .Select(x => x.Split('~'))
                                                                                     .Select(x => new Tuple<string, string, string>(x[0], x[1], x[2]))
                                                                                     .ToList();

                for (int i = 0; i < spparams.Count; i++)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Param: " + spparams[i].Item1 + ", " + spparams[i].Item2 + ", " + spparams[i].Item3 });
                    SqlParameter param = new SqlParameter(spparams[i].Item1, GetSQLDataType(spparams[i].Item3));
                    param.Value = spparams[i].Item2;
                    sqlparameters.Add(param);
                }
            }

            try
            {
                isSuccess = SQLUtilities.ExecuteStoredProcedure(parms["dbname"], parms["subtype"], sqlparameters);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR Occured executing stored procedure - {parms["subtype"]} on database {parms["dbname"]}", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
            if (!isSuccess)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = $"ERROR Occured executing stored procedure - {parms["subtype"]} on database {parms["dbname"]}", ErrorCode = "ERROR" });
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private static SqlDbType GetSQLDataType(string type)
        {
            return Enum.TryParse(type, out SqlDbType SQLType) ? SQLType : SqlDbType.VarChar;
        }
    }
}
