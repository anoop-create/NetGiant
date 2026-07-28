using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    class EquipmentFeeds
    {
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;

            //Execute Stored Procedure to return the equipment result in a datatable
            DataTable results;

            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery("tonergiant", "ng_GenerateEquipmentURLs");
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ng_GenerateEquipmentURLs", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            //Setup the CSV file and the delim to use, 
            char outputDelim = ',';

            using (CsvFileWriter writer = new CsvFileWriter(parms["output"], outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    CsvRow firstRow = new CsvRow();
                    foreach (DataColumn dc in results.Columns)
                    {
                        firstRow.Add(dc.ColumnName);
                    }
                    writer.WriteRow(firstRow);

                    //Loop through the datatable and write a row in the csv file for each
                    foreach (DataRow dr in results.Rows)
                    {
                        CsvRow newRow = new CsvRow();
                        foreach (DataColumn dc in results.Columns)
                        {
                            newRow.Add(dr[dc.ColumnName].ToString());
                        }
                        writer.WriteRow(newRow);
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing CSV File: " + parms["output"] + ": " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
