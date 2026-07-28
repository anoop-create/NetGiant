using System;
using System.Data;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using ngBatchProcesses.BusinessObjects.Provider;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.Axis
{
    /// <summary>
    /// download data from the diplomat's exported csv file
    /// </summary>
    public class ImportProductFieldData
    {
        public ImportProductFieldData()
        {
            hasErrorOccurred = false;
        }
        
        public static bool hasErrorOccurred;

        public static void PopulateProductFields()
        {
            //public variables and instances
            Properties.Settings settings = Properties.Settings.Default;
            StandardFunctions stnFunc = new StandardFunctions();
            DataTable csvData = null;
            DataTable desiredCSVData = null;
            string desiredCSVPath = string.Empty;
            FileInfo file = null;

            string csvPath = settings.LocalDirectory + "ProductFieldData\\"; 

            try
            {
                DirectoryInfo directory = new DirectoryInfo(csvPath);
                file = directory.GetFiles("*.csv").OrderByDescending(f => f.LastWriteTime).First();
            }
            catch
            {
                stnFunc.AddToActivityLog("***Error*** Couldn't find a csv file in " + csvPath);
            }

            stnFunc.AddToActivityLog("Started Batch Program with switch: PopulateProductFields" + System.Environment.NewLine);

            if (file != null)
            {
                bool onlyProductFields = file.Name.Contains("prd_") ? true : false;
                
                // download data from the exported csv
                csvData = DownloadCSVData(csvPath + file, stnFunc);

                /// <TODO>
                /// currently desired columns are hardcoded in an array but in future we may want to use the table.
                /// best approach would be to have a table where desired columns can be stored and fetch the list of columns rather than hardcoding below
                /// NOTE: if you add/remove/update column below, please do not forget to update the procedures [ngmd].[CopyProductFieldsData] or [ngmd].[CopyEquipmentData]
                string[] desiredProductFieldColumns = { "Alt Ref", "Stock reference", "Specification 1", "Specification 2", "Specification 3", "Specification 4", 
                    "Specification 5", "Specification 6", "Default delivery days", "Discontinued Item", "Re-saleable", "Attribute 1", "Attribute 2",
                    "Attribute 3", "Attribute 4", "Attribute 5", "Attribute 6", "Attribute 7", "Attribute 8", "Attribute 9", "Attribute 10", "English Meta Title",
                    "English Meta Keywords", "English Meta Description", "Additional Information URL", "Suppress cnet image", "Suppress cnet description",
                    "Featured item", "Best seller"};
                string[] desiredEquipmentColumns = { "Name", "Manufacturer", "Family", "AltRef" };
                
                /// get the desired data based on the desired columns
                desiredCSVData = onlyProductFields ? GetDesiredData(csvData, stnFunc, desiredProductFieldColumns) :
                    GetDesiredData(csvData, stnFunc, desiredEquipmentColumns);

                if (null != desiredCSVData)
                {
                    // create a new desired csv file
                    desiredCSVPath = CreateDesiredCSV(desiredCSVData, stnFunc);

                    if (onlyProductFields)
                    {
                        // create temp product fields for the products if they don't exist
                        ProductFields.CreateTempFieldValues(desiredCSVPath, stnFunc);

                        // update PMS product field tables based on the new desired csv
                        CopyProductFieldData(desiredCSVPath, stnFunc);
                    }
                    else
                    {
                        List<string> nonExistingManufacturers = CopyEquipmentData(desiredCSVPath, stnFunc);

                        if (nonExistingManufacturers.Count > 0)
                        {
                            foreach (string manu in nonExistingManufacturers)
                            {
                                stnFunc.AddToActivityLog(string.Format("***Warning*** manufacturer {0} does not exist.", manu));
                            }
                        }
                    }

                    // archive csv file and delete
                    stnFunc.CopyFileAndDelete(desiredCSVPath, settings.LocalDirectory + "ProductFieldData\\Archive\\");
                    stnFunc.ClearFilesBasedOnDays(7, settings.LocalDirectory + "ProductFieldData\\Archive\\");
                    
                    // delete exported csv file
                    if (!settings.Environment.Equals("Local", StringComparison.InvariantCultureIgnoreCase))
                    {
                        System.IO.File.Delete(csvPath + file);
                        stnFunc.AddToActivityLog("Successfully deleted file from: " + csvPath);
                    }
                }
                else
                {
                    stnFunc.AddToActivityLog("***Error*** Unable to create desired csv file. Either column does not exist or column name is misspelled");
                }
            }

            stnFunc.AddToActivityLog("Finished Batch Program switch: populateproductfields");
            string acitivityLogFileName = stnFunc.LogActivity();

            if (hasErrorOccurred)
                stnFunc.SendSimpleEmail("Updated product fields data", acitivityLogFileName);
        }
        
        protected static DataTable DownloadCSVData(string filePath, StandardFunctions stnFunc)
        {
            DataTable dt = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(filePath))
                using (StreamReader reader = new StreamReader(filePath))
                {
                    csvReader.SetDelimiters(new string[] { FtpUtilities.DetectDelimiter(reader, File.ReadAllLines(filePath).Count()).ToString() });
                    csvReader.TrimWhiteSpace = true;

                    //column headers
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        dt.Columns.Add(datecolumn);
                    }

                    //column data
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();

                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }

                        dt.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** Unable to download data from " + filePath);
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                hasErrorOccurred = true;
            }

            return dt;
        }

        protected static DataTable GetDesiredData(DataTable csvData, StandardFunctions stnFunc, string[] desiredColumns)
        {
            DataColumnCollection columns = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in desiredColumns)
                {
                    if (columns.Contains(col))
                    {
                        mappedColumns.Add(colIndex, col);
                        colIndex++;
                    }
                }

                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());
                stnFunc.AddToActivityLog("Successfully retrieved the desired columns from csv.");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** Unable to get desired column data. Either column name is missing or column name is not spelled right.");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                hasErrorOccurred = true;
            }
            
            return csvData;
        }

        protected static string CreateDesiredCSV(DataTable desiredCSVData, StandardFunctions stnFunc)
        {
            Properties.Settings settings = Properties.Settings.Default;
            string newCSVFilePath = settings.Environment.Equals("Local", StringComparison.InvariantCultureIgnoreCase) ? settings.LocalDirectory :
                settings.SQLServerFilePath;
            newCSVFilePath += "ProductFieldData\\newDesiredCSV.csv";

            try
            {
                StringBuilder sb = new StringBuilder();
                IEnumerable<string> columnNames = desiredCSVData.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in desiredCSVData.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(newCSVFilePath, sb.ToString());
                stnFunc.AddToActivityLog("Desired csv file has been created: " + newCSVFilePath);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** unable to create desired csv file.");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                hasErrorOccurred = true;
            }

            return newCSVFilePath;
        }

        protected static void CopyProductFieldData(string desiredCSVPath, StandardFunctions stnFunc)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(SQLUtilities.GetMachineConnectionString("netgiantmasterdata")))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "ngmd.CopyProductFieldsData";
                    cmd.CommandTimeout = 1000;

                    cmd.Parameters.Add(new SqlParameter(
                        "@DesiredCSV", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, desiredCSVPath));

                    if (conn.State == ConnectionState.Closed) conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                stnFunc.AddToActivityLog("Copied product fields data successfully");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** unable to copy product fields data.");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                hasErrorOccurred = true;
            }
        }

        protected static List<string> CopyEquipmentData(string desiredCSVPath, StandardFunctions stnFunc)
        {
            List<string> manufacturers = new List<string>();

            try
            {
                using (SqlConnection conn = new SqlConnection(SQLUtilities.GetMachineConnectionString("netgiantmasterdata")))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "ngmd.CopyEquipmentData";
                    cmd.CommandTimeout = 1000;

                    cmd.Parameters.Add(new SqlParameter(
                        "@DesiredCSV", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, desiredCSVPath));

                    if (conn.State == ConnectionState.Closed) conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (reader.Read())
                        {
                            manufacturers.Add(reader.GetString(0));
                        }
                    }

                    conn.Close();
                }

                stnFunc.AddToActivityLog("Copied equipment data successfully");
            }
            catch(Exception ex)
            {
                stnFunc.AddToActivityLog("***Error*** unable to copy equipment data.");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                hasErrorOccurred = true;
            }

            return manufacturers;
        }
    }
}
