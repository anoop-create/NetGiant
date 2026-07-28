using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGBP.DataAccessLayer.DataUtilities
{
    public class ExcelUtilities
    {
        public static DataTable LoadWorksheetInDataTable(string fileName, bool hasHeaders = true, string sheetName = "", string format = "xlsx")
        {
            DataTable sheetData = new DataTable();
            using (OleDbConnection conn = ReturnConnection(fileName, format))
            {
                conn.Open();
                if (string.IsNullOrEmpty(sheetName))
                {
                    // Figure out the 1st Sheet name
                    DataTable dtSchema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                    sheetName = dtSchema.Rows[0]["TABLE_NAME"].ToString();
                }
                else
                {
                    sheetName += "$";
                }
                // retrieve the data using data adapter
                OleDbDataAdapter sheetAdapter = new OleDbDataAdapter("select * from [" + sheetName + "]", conn);
                sheetAdapter.Fill(sheetData);
                conn.Close();
            }

            // Add headers and remove Row 1
            if (hasHeaders)
            {
                int i = 0;
                foreach (DataColumn dc in sheetData.Columns)
                {
                    dc.ColumnName = sheetData.Rows[0][i].ToString();
                    i += 1;
                }
                sheetData.Rows.Remove(sheetData.Rows[0]);
            }

            // Remove any empty rows
            for (int i = sheetData.Rows.Count - 1; i >= 0; i--)
            {
                if (IsEmptyRow(sheetData.Rows[i]))
                {
                    sheetData.Rows.Remove(sheetData.Rows[i]);
                }
            }

            return sheetData;
        }

        private static OleDbConnection ReturnConnection(string fileName, string format)
        {
            //if (format == "xls")
            //{
            //    return new OleDbConnection("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileName + "; Jet OLEDB:Engine Type=5;Extended Properties=\"Excel 8.0;\"");
            //}
            //return new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName + ";Extended Properties=Excel 12.0;");
            return new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileName + ";Extended Properties='Excel 12.0 Xml;HDR=No;IMEX=1;';");
        }

        private static bool IsEmptyRow(DataRow dr)
        {
            if (dr == null)
            {
                return true;
            }
            else
            {
                foreach (var value in dr.ItemArray)
                {
                    if (value != null)
                    {
                        if (value.ToString() != "")
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
