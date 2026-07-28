using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DP001DataAccess.Utilities;
using System.Configuration;

namespace DP001BusinessLogic.Shared
{
    public class ExportUtilities
    {
        public static Stream ExportToSpreadsheet<T>(IEnumerable<T> data, bool displayHeaders)
        {
            var localDrive = ConfigurationManager.AppSettings["LocalDirectory"];
            var uId = Guid.NewGuid();
            //var filePath = string.Format(@"{0}\DP001TempData\PriceologyExport_{1}.csv", localDrive, uId);
            var fileStream = new MemoryStream();

            var firstRow = new Csv.CsvRow();

            var writer = new Csv.CsvFileWriter(fileStream, ',');

            // Get Headings for Csv File
            data.First()
                .GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(x => x.Name)
                .ToList()
                .ForEach(x => firstRow.Add(x.Replace("_", " ")));

            writer.WriteRow(firstRow);

            // Get data for Csv File
            foreach (var row in data)
            {
                var newRow = new Csv.CsvRow();

                row.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Select(x => x.GetValue(row, null))
                    .ToList()
                    .ForEach(x => newRow.Add(x != null ? x.ToString() : ""));

                writer.WriteRow(newRow);

            }

            fileStream.Position = 0;

            return fileStream;

            // ---- The code below will export to an xlsx output for reading in Excel software.
            // ---- Left here in case needed in the future.

            //using (ExcelPackage p = new ExcelPackage())
            //{
            //    p.Workbook.Properties.Author = "Priceology";
            //    p.Workbook.Properties.Title = "Priceology Export";
            //    p.Workbook.Properties.Company = "Priceology";

            //    p.Workbook.Worksheets.Add("Export");
            //    ExcelWorksheet ws = p.Workbook.Worksheets[1];
            //    ws.Name = "Export";
            //    ws.Cells["A1"].LoadFromCollection(data, displayHeaders); 

            //    var bin = p.GetAsByteArray();
            //    var localDrive = CommonFunctions.GetMachineAppSetting("LocalDirectory");
            //    var uId = Guid.NewGuid();
            //    var filePath = string.Format(@"{0}\DP001TempData\PriceologyExport_{1}.xlsx", localDrive, uId);

            //    File.WriteAllBytes(filePath, bin);

            //    return filePath;
            //}
        }
    }
}
