using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Orders;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class CampaignTrackingViewModel : CommonViewModel
    {
        public CampaignTrackingViewModel()
        {
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;
        public CampaignTracking CampaignTracking { get; set; }
        public IQueryable<TelerikCampaignTracking> CampaignTrackingList { get; set; }
        public List<CampaignTracking> CampaignTrackingForExport { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public void GetCampaignTracking()
        {
            CampaignTrackingList = _ctx.CampaignTracking.Select(x => new TelerikCampaignTracking
            {
                Id = x.CampaignTrackingId,
                OrderDate = x.OrderDate,
                OrderNumber = x.OrderNumber,
                OrderSourceFk = x.OrderSourceFk,
                OrderSource = x.Lookup.LookupName,
                Campaign = x.Campaign
            })
            .AsQueryable();
        }

        public void CreateCampaignTrackingCSVFile(List<TelerikCampaignTracking> ctList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\CampaignTrackingExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikCampaignTracking ct in ctList)
                {
                    InsertCSVData(writer, ct);
                }
            }

        }

        private void InsertCSVData(CsvFileWriter writer, TelerikCampaignTracking item)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(item.Id.ToString());
            newRow.Add(item.OrderDate.ToString("dd/MM/yyyy"));
            newRow.Add(item.OrderNumber);
            newRow.Add(item.OrderSource);
            newRow.Add(item.Campaign);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("CampaignTrackingId");
            firstRow.Add("Order Date");
            firstRow.Add("Order Number");
            firstRow.Add("Order Source");
            firstRow.Add("Campaign");

            writer.WriteRow(firstRow);
        }
    }

    public class TelerikCampaignTracking
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public int OrderSourceFk { get; set; }
        public string OrderSource { get; set; }
        public string Campaign { get; set; }
    }
}
