using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export
{
    // Flat CSV dump of the ProductAddon mappings, one row per product that has at least one
    // Add On configured. Column layout is the exact inverse of ImportProductAddonViewModel,
    // so an export -> edit -> re-import round-trip works cleanly:
    //   "Product SKU"  - the product's AltRef/PartNo
    //   "Add On SKUs"  - comma-separated AltRef/PartNo values, in DisplayOrder
    public class ExportProductAddonViewModel : CommonViewModel
    {
        private const string AddOnSeparator = ",";

        public ExportProductAddonViewModel()
        {
            GetExportableData();
        }

        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public int ProductAddonCount { get; set; }
        public IList<ExportProductAddonRow> ExportContent { get; set; }

        private void GetExportableData()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var addOnRows = (from pa in db.ProductAddon
                                 join p in db.product on pa.ProductId equals p.productID
                                 join a in db.product on pa.AddonProductId equals a.productID
                                 orderby p.partNo, pa.DisplayOrder
                                 select new
                                 {
                                     ProductSKU = p.partNo,
                                     AddOnSKU = a.partNo
                                 }).ToList();

                ExportContent = addOnRows
                    .GroupBy(x => x.ProductSKU)
                    .Select(g => new ExportProductAddonRow
                    {
                        ProductSKU = g.Key,
                        AddOnSKUs = string.Join(AddOnSeparator, g.Select(x => x.AddOnSKU))
                    })
                    .ToList();
            }
        }

        public ExportProductAddonViewModel GetResultsCount()
        {
            ProductAddonCount = ExportContent.Count();

            return this;
        }

        private void SetFilePath()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProductAddonExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";
        }

        public void Export()
        {
            SetFilePath();

            // The PMSTempData folder is a shared prerequisite for every Import/Export action in
            // this app (none of them create it), so create it defensively here rather than
            // depending on it having been set up already.
            string directory = Path.GetDirectoryName(FilePath);
            if (Directory.Exists(LocalDirectory))
            {
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            else
            {
                LocalDirectory = "C:\\";
                FilePath = LocalDirectory + "\\PMSTempData\\ProductAddonExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";
                directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (var row in ExportContent)
                {
                    CsvRow csvRow = new CsvRow();
                    csvRow.Add(row.ProductSKU);
                    csvRow.Add(row.AddOnSKUs);
                    writer.WriteRow(csvRow);
                }
            }
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Product SKU");
            firstRow.Add("Add On SKUs");
            writer.WriteRow(firstRow);
        }
    }

    public class ExportProductAddonRow
    {
        public string ProductSKU { get; set; }
        public string AddOnSKUs { get; set; }
    }
}
