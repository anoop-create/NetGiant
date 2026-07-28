using System;
using System.Collections.Generic;
using System.Data;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class FreshRelevanceFeed
    {
        private StandardFunctions _stnFunc;
        private Dictionary<string, string> _params;

        public FreshRelevanceFeed(Dictionary<string, string> parms)
        {
            _stnFunc = new StandardFunctions();
            _params = parms;
        }

        public void GenerateFeeds()
        {
            _stnFunc.AddToActivityLog($"{_params["type"]} - Process Started");
            GenerateProductFeed();
            GeneratePersonFeed();
            GenerateTransactionFeed();
            _stnFunc.AddToActivityLog($"{_params["type"]} - Process Complete");
            _stnFunc.LogActivity(_params["type"]);
        }

        public void GenerateProductFeed()
        {
            WriteAndUploadFile("netgiantmasterdata", "product_import.txt", "ngmd.ng_FreshRelevance");
            _stnFunc.AddToActivityLog(_params["type"] + " " + " - product import file created");
        }

        public void GeneratePersonFeed()
        {
            WriteAndUploadFile("axisdiplomat", "person_import.txt", "ng_FreshRelevancePersonal");
            _stnFunc.AddToActivityLog(_params["type"] + " " + " - person import file created");
        }

        public void GenerateTransactionFeed()
        {
            WriteAndUploadFile("axisdiplomat", "transaction_import_" + DateTime.Today.ToString("yyyyMMdd") + ".txt", "ng_FreshRelevanceTrans");
            _stnFunc.AddToActivityLog(_params["type"] + " " + " - transaction import file created");
        }
        
        private void WriteAndUploadFile(string connection, string fileExtension, string spName)
        {
            var outputDelim = '\t';
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connection, spName);
            }
            catch (Exception ex)
            {
                _stnFunc.AddToActivityLog($"**ERROR** Executing stored procedure {spName}");
                _stnFunc.ProcessException(ex);
                _stnFunc.LogActivity();
                return;
            }

            //Write the Text file
            using (var writer = new CsvFileWriter(_params["output"] + "\\" + fileExtension, outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    var firstRow = new CsvRow();

                    //Write details
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
                    _stnFunc.AddToActivityLog("**Error** Writing Equipment CSV File: " + _params["output"] + ": " +
                                             ex.Message);
                    _stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                }
            }
        }
    }
}
