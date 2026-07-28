using System;
using System.Collections.Generic;
using ngBatchProcesses.BusinessObjects.Shared;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace ngBatchProcesses.BusinessObjects.Axis
{
    class LoadAXISProductIds
    {
        private Dictionary<string, string> _params;

        public LoadAXISProductIds(Dictionary<string, string> parms)
        {
            _params = parms;
        }

        public void UpdateProductIds()
        {
            StandardFunctions.WriteProcessStarted();
            string connName = "axisdiplomat";

            //Execute Stored Procedure to return the equipment result in a datatable
            DataTable results;

            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetProductIds");
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ng_GetProductIds", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });

                return;
            }

            //Setup the CSV file and the delim to use, 
            char outputDelim = ',';
            CsvFileWriter writer = new CsvFileWriter(_params["output"], outputDelim);

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

            //Close the writer and dispose of the datatable containing the results
            writer.Close();
            results.Dispose();

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
