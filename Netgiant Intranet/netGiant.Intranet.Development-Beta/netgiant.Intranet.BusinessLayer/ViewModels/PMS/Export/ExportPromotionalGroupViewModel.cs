using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export
{
    public class ExportPromotionalGroupViewModel : CommonViewModel
    {
        public ExportPromotionalGroupViewModel()
        {
            GetExportableData();
        }

        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public int PromotionalGroupCount { get; set; }
        public IList<ExportPromotionalGroup> ExportContent { get; set; }

        private void GetExportableData()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                ExportContent = (from w in db.websiteInventory
                                 join p in db.product on w.productFK equals p.productID
                                 join pro in db.promotionalGroup on w.promotionalGroupFK equals pro.promotionalGroupId
                                 where w.promotionalGroupFK != null
                                 select new ExportPromotionalGroup
                                 {
                                     WebsiteId = w.websiteFK,
                                     AltRef = p.partNo,
                                     PromoName = pro.promotionalGroupName
                                 }).ToList();
            }
        }

        public ExportPromotionalGroupViewModel GetResultsCount()
        {
            PromotionalGroupCount = ExportContent.Count();

            return this;
        }

        private void SetFilePath()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProductExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";
        }

        public void Export()
        {
            SetFilePath();

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (var promo in ExportContent)
                {
                    CsvRow row = new CsvRow();
                    row.Add(Convert.ToString(promo.WebsiteId));
                    row.Add(promo.AltRef);
                    row.Add(promo.PromoName);
                    writer.WriteRow(row);
                }
            }
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Website ID");
            firstRow.Add("Alt Ref");
            firstRow.Add("Promo Name");
            writer.WriteRow(firstRow);
        }
    }

    public class ExportPromotionalGroup
    {
        public int WebsiteId { get; set; }
        public string AltRef { get; set; }
        public string PromoName { get; set; }
    }
}
