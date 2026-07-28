using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using static DP001BusinessLogic.CrudSalesHistory;

namespace DP001BusinessLogic.ViewModels
{
    public class SalesHistoryViewModel
    {
        public SalesHistoryViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public SalesHistoryViewModel()
        {

        }

        public IQueryable<Telerik> TelerikSalesHistory { get; set; }
        public SummarizeBy SummarizeSalesHistoryBy { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public GroupBy GroupSalesHistoryBy { get; set; }
        public List<SelectListItem> MonthsFrom { get; set; }
        public List<SelectListItem> MonthsTo { get; set; }
        public List<SelectListItem> YearsFrom { get; set; }
        public List<SelectListItem> YearsTo { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public bool UserCanModify { get; set; }

        private readonly int _channelId;
        private readonly DP001Entities _ctx;

        public void InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            if (reportConfigId != null)
            {
                var config = CrudReportConfiguration.Read(x => x.ReportConfigurationId == reportConfigId).FirstOrDefault();

                if (config != null)
                {
                    if (config.Lookup.LookupName == "Private")
                    {
                        if (config.UserId != userId)
                            return;
                    }

                    if (config.Lookup.LookupName == "Shared")
                    {
                        if (config.TenantFk != tenantFk)
                            return;
                    }

                    ReportConfiguration = config;

                    if (ReportConfiguration.UserId == userId)
                        UserCanModify = true;
                }
            }

            SummarizeSalesHistoryBy = SummarizeBy.Month;
            GroupSalesHistoryBy = GroupBy.Product;

            var today = DateTime.Today;
            MonthsFrom = Enumerable.Range(0, 14)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Select(date => new SelectListItem
                {
                    Text = date.ToString("MMMM yyyy"),
                    Value = new DateTime(date.Year, date.Month, 1).ToString()
                })
                .ToList();

            MonthsTo = Enumerable.Range(0, 14)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Select(date => new SelectListItem
                {
                    Text = date.ToString("MMMM yyyy"),
                    Value = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)).ToString()
                })
                .ToList();

            YearsFrom = Enumerable.Range(0, 2)
                .Select(i => DateTime.Now.AddYears(-i))
                .Select(date => new SelectListItem
                {
                    Text = date.ToString("yyyy"),
                    Value = new DateTime(date.Year, 1, 1).ToString()
                })
                .ToList();

            YearsTo = Enumerable.Range(0, 2)
                .Select(i => DateTime.Now.AddYears(-i))
                .Select(date => new SelectListItem
                {
                    Text = date.ToString("yyyy"),
                    Value = new DateTime(date.Year, 12, 31).ToString()
                })
                .ToList();

            DateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            DateTo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Sales History");
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
        }

        public void Get()
        {
            var crud = new CrudSalesHistory();
            TelerikSalesHistory = crud.ReadSalesHistoryQuery(x =>
                        x.ChannelFk == _channelId,
                    _ctx,
                    SummarizeSalesHistoryBy,
                    DateFrom,
                    DateTo,
                    GroupSalesHistoryBy)
                .AsTelerikViewModel();

            //var crud = new CrudSalesHistory();
            //TelerikSalesHistory = crud.ReadSalesHistoryQuery(
            //        _channelId,
            //        _ctx,
            //        SummarizeSalesHistoryBy,
            //        DateFrom,
            //        DateTo,
            //        GroupSalesHistoryBy)
            //    .AsTelerikViewModel();
        }

        public class Telerik
        {
            // Sales Related Properties
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string ClientProductId { get; set; }
            public int? Quantity { get; set; }
            public decimal? AverageCostPrice { get; set; }
            public decimal? AveragePrice { get; set; }
            public DateTime? Month { get; set; }
            public DateTime? Day { get; set; }
            public DateTime? Year { get; set; }
            public string RuleName { get; set; }
            public decimal? AverageGrossMarginValue { get; set; }
            public decimal? AverageGrossMarginPercent { get; set; }
            public decimal? TotalSalesValue { get; set; }

            // Product Related Properties
            public string PartNumber { get; set; }
            public string ProductDescription { get; set; }
            public string BrandName { get; set; }
            public string CategoryName { get; set; }
        }
    }

    public static class SalesHistoryExtensions
    {
        public static IQueryable<SalesHistoryViewModel.Telerik> AsTelerikViewModel(this IQueryable<SalesHistoryGroup> productQuery)
        {
            return productQuery.Select(o => new SalesHistoryViewModel.Telerik
            {             
                StartDate = o.EndDate,
                EndDate = o.EndDate,
                ClientProductId = o.ClientProductId,
                Quantity = o.Quantity,
                AverageCostPrice = o.TotalCostPrice,
                AveragePrice = o.TotalPrice,
                Day = DbFunctions.CreateDateTime(o.EndDate.Year, o.EndDate.Month, o.EndDate.Day, 0, 0, 0).Value,
                Month = DbFunctions.CreateDateTime(o.EndDate.Year, o.EndDate.Month, 1, 0, 0, 0).Value,
                Year = DbFunctions.CreateDateTime(o.EndDate.Year, 1, 1, 0, 0, 0).Value,
                PartNumber = o.PartNumber,
                ProductDescription = o.ProductName,
                BrandName = o.BrandName,
                CategoryName = o.CategoryName,
                RuleName = o.RuleName,
                AverageGrossMarginValue = Math.Round((decimal)(o.Quantity * o.TotalPrice - o.Quantity * o.TotalCostPrice), 2),
                AverageGrossMarginPercent = o.TotalPrice > 0 ? Math.Round((decimal)((o.TotalPrice - o.TotalCostPrice) / o.TotalPrice * 100), 2) : 0,
                TotalSalesValue = Math.Round((decimal)(o.Quantity * o.TotalPrice), 2)
            });
        }
    }
}

