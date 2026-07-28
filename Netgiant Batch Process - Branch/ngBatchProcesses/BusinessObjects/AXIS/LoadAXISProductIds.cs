using System;
using System.Collections.Generic;
using ngBatchProcesses.BusinessObjects.Shared;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.Axis
{
    class LoadAXISProductIds
    {
        private Dictionary<string, string> _params;
        private StandardFunctions _stnFunc;

        public LoadAXISProductIds(Dictionary<string, string> parms)
        {
            _params = parms;
            _stnFunc = new StandardFunctions();
        }

        public void UpdateProductIds()
        {
            _stnFunc.AddToActivityLog(_params["type"] + " " + " Started");

            string connName = "axisdiplomat";

            //Execute Stored Procedure to return the equipment result in a datatable
            DataTable results;

            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetProductIds");
            }
            catch (Exception ex)
            {
                _stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetProductIds");
                _stnFunc.ProcessException(ex);
                _stnFunc.LogActivity();
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

            _stnFunc.AddToActivityLog(_params["type"] + " Process Finished");
            _stnFunc.LogActivity(_params["type"]);
        }
    }
}
