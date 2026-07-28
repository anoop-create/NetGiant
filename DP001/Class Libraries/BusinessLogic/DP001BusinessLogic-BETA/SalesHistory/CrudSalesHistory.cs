using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;

namespace DP001BusinessLogic
{
    public class CrudSalesHistory
    {

        public enum SummarizeBy
        {
            Day,
            Month,
            Year
        }

        public enum GroupBy
        {
            Product,
            Category,
            [Display(Name = "Category and Brand")]
            CategoryThenBrand,
            Brand,
            Rule
        }

        public bool Create(List<SalesHistory> salesHistory, Channel channel)
        {
            DataTable dt = new DataTable("StagingSalesHistory");

            using (DP001Entities db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("StartDate", typeof(DateTime)));
                dt.Columns.Add(new DataColumn("EndDate", typeof(DateTime)));
                dt.Columns.Add(new DataColumn("ClientProductId", typeof(string)));
                dt.Columns.Add(new DataColumn("Quantity", typeof(int)));
                dt.Columns.Add(new DataColumn("AverageCostPrice", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AveragePrice", typeof(decimal)));

                foreach (SalesHistory sh in salesHistory)
                {
                    dt.Rows.Add(sh.ChannelFk, sh.StartDate, sh.EndDate, sh.ClientProductId, sh.Quantity, sh.AverageCostPrice, sh.AveragePrice);
                }
            }

            SQL.SQLBulkInsert(dt, "DP001");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = channel.ChannelID;
            sqlParms.Add(sqlParm1);

            CommonDataFunctions.CreateLogEntry(channel, "END CreateSalesHistoryData - COMPUTE", "Information");
            CommonDataFunctions.CreateLogEntry(channel, "START CreateSalesHistoryData - SQL", "Information");

            var isSuccess = SQL.ExecuteStoredProcedure("DP001", "CreateUpdateSalesHistory", sqlParms, channel.ChannelID);

            if (!isSuccess)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Unable to load sales history due to errors found. Please contact support.", "Notification");
            }

            CommonDataFunctions.CreateLogEntry(channel, "END CreateSalesHistoryData - SQL", "Information");

            return isSuccess;
        }

        // This function will be used by Sales History once we have a decision / direction to take regarding the showing 'No Sales' entries. Items that haven't sold etc...

        // DON'T DELETE
        //public IQueryable<SalesHistoryGroup> ReadSalesHistoryQuery(
        //    int channelFk,
        //    DP001Entities ctx,
        //    SummarizeBy? summarizeByPeriod,
        //    DateTime? dateFrom,
        //    DateTime? dateTo,
        //    GroupBy? groupBy)
        //{
        //    var baseQuery = ctx.GetSalesHistoryData(dateFrom, dateTo, channelFk)
        //        .AsQueryable();

        //    IQueryable<IGrouping<SalesHistoryGroupByEntity, GetSalesHistoryData_Result>> groupedQuery;
        //    IQueryable<SalesHistoryGroup> finalQuery;

        //    switch (groupBy)
        //    {
        //        case GroupBy.Category:
        //            switch (summarizeByPeriod)
        //            {
        //                case SummarizeBy.Day:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Day,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName
        //                    });
        //                    break;
        //                case SummarizeBy.Year:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Year,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName
        //                    });
        //                    break;
        //                default:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Month,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName
        //                    });
        //                    break;
        //            }

        //            finalQuery = groupedQuery.Select(x =>
        //                new SalesHistoryGroup
        //                {
        //                    SalesHistoryid = 0,
        //                    EndDate = x.Key.EndDate ?? DateTime.Now,
        //                    Quantity = x.Sum(y => y.Qty),
        //                    TotalPrice = Math.Round(x.Sum(y => y.AveragePrice), 2) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1),
        //                    TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    ProductName = "",
        //                    PartNumber = "",
        //                    ClientProductId = "",
        //                    BrandName = "",
        //                    CategoryName = x.Key.CategoryName,
        //                    RuleName = ""
        //                });

        //            break;
        //        case GroupBy.Brand:
        //            switch (summarizeByPeriod)
        //            {
        //                case SummarizeBy.Day:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Day,
        //                        GroupingId = x.BrandId,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //                case SummarizeBy.Year:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Year,
        //                        GroupingId = x.BrandId,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //                default:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Month,
        //                        GroupingId = x.BrandId,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //            }

        //            finalQuery = groupedQuery.Select(x =>
        //                new SalesHistoryGroup
        //                {
        //                    SalesHistoryid = 0,
        //                    EndDate = x.Key.EndDate ?? DateTime.Now,
        //                    Quantity = x.Sum(y => y.Qty),
        //                    TotalPrice = Math.Round(x.Sum(y => y.AveragePrice), 2) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1),
        //                    TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    ProductName = "",
        //                    PartNumber = "",
        //                    ClientProductId = "",
        //                    BrandName = x.Key.BrandName,
        //                    CategoryName = "",
        //                    RuleName = ""
        //                });

        //            break;
        //        case GroupBy.Rule:
        //            switch (summarizeByPeriod)
        //            {
        //                case SummarizeBy.Day:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Day,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //                case SummarizeBy.Year:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Year,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //                default:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Month,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //            }

        //            finalQuery = groupedQuery.Select(x =>
        //                new SalesHistoryGroup
        //                {
        //                    SalesHistoryid = 0,
        //                    EndDate = x.Key.EndDate ?? DateTime.Now,
        //                    Quantity = x.Sum(y => y.Qty),
        //                    TotalPrice = Math.Round(x.Sum(y => y.AveragePrice), 2) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1),
        //                    TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    ProductName = "",
        //                    PartNumber = "",
        //                    ClientProductId = "",
        //                    BrandName = "",
        //                    CategoryName = "",
        //                    RuleName = x.Key.RuleName
        //                });

        //            break;
        //        case GroupBy.CategoryThenBrand:
        //            switch (summarizeByPeriod)
        //            {
        //                case SummarizeBy.Day:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Day,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //                case SummarizeBy.Year:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Year,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //                default:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Month,
        //                        GroupingId = x.CategoryId,
        //                        CategoryName = x.CategoryName,
        //                        BrandName = x.BrandName
        //                    });
        //                    break;
        //            }

        //            finalQuery = groupedQuery.Select(x =>
        //                new SalesHistoryGroup
        //                {
        //                    SalesHistoryid = 0,
        //                    EndDate = x.Key.EndDate ?? DateTime.Now,
        //                    Quantity = x.Sum(y => y.Qty),
        //                    TotalPrice = Math.Round(x.Sum(y => y.AveragePrice), 2) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1),
        //                    TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    ProductName = "",
        //                    PartNumber = "",
        //                    ClientProductId = "",
        //                    BrandName = x.Key.BrandName,
        //                    CategoryName = x.Key.CategoryName,
        //                    RuleName = ""
        //                });

        //            break;
        //        default:
        //            switch (summarizeByPeriod)
        //            {
        //                case SummarizeBy.Day:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Day,
        //                        GroupingId = x.ProductInventoryId,
        //                        ProductName = x.ProductDescription,
        //                        PartNumber = x.ManufacturerPartNo,
        //                        ClientProductId = x.ClientProductId,
        //                        BrandName = x.BrandName,
        //                        CategoryName = x.CategoryName,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //                case SummarizeBy.Year:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Year,
        //                        GroupingId = x.ProductInventoryId,
        //                        ProductName = x.ProductDescription,
        //                        PartNumber = x.ManufacturerPartNo,
        //                        ClientProductId = x.ClientProductId,
        //                        BrandName = x.BrandName,
        //                        CategoryName = x.CategoryName,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //                default:
        //                    groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
        //                    {
        //                        EndDate = x.Month,
        //                        GroupingId = x.ProductInventoryId,
        //                        ProductName = x.ProductDescription,
        //                        PartNumber = x.ManufacturerPartNo,
        //                        ClientProductId = x.ClientProductId,
        //                        BrandName = x.BrandName,
        //                        CategoryName = x.CategoryName,
        //                        RuleName = x.PriceRuleName
        //                    });
        //                    break;
        //            }

        //            finalQuery = groupedQuery.Select(x =>
        //                new SalesHistoryGroup
        //                {
        //                    SalesHistoryid = 0,
        //                    EndDate = x.Key.EndDate ?? DateTime.Now,
        //                    Quantity = x.Sum(y => y.Qty),
        //                    TotalPrice = Math.Round(x.Sum(y => y.AveragePrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice) / (x.Count(c => c.Qty > 0) > 0 ? x.Count(c => c.Qty > 0) : 1), 2),
        //                    ProductName = x.Key.ProductName,
        //                    PartNumber = x.Key.PartNumber,
        //                    ClientProductId = x.Key.ClientProductId,
        //                    BrandName = x.Key.BrandName,
        //                    CategoryName = x.Key.CategoryName,
        //                    RuleName = x.Key.RuleName
        //                });

        //            break;
        //    }

        //    return finalQuery;
        //}

        public IQueryable<SalesHistoryGroup> ReadSalesHistoryQuery(
            Expression<Func<SalesHistory, bool>> where,
            DP001Entities ctx,
            SummarizeBy? summarizeByPeriod,
            DateTime? dateFrom,
            DateTime? dateTo,
            GroupBy? groupBy)
        {
            var baseQuery = ctx.SalesHistories
                .Include("ProductInventory.Brand")
                .Include("ProductInventory.ProductCategory")
                .AsQueryable();

            baseQuery = baseQuery.Where(where);

            IQueryable<IGrouping<SalesHistoryGroupByEntity, SalesHistory>> groupedQuery;
            IQueryable<SalesHistoryGroup> finalQuery = null;

            switch (groupBy)
            {
                case GroupBy.Category:
                    switch (summarizeByPeriod)
                    {
                        case SummarizeBy.Day:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Day,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName
                            });
                            break;
                        case SummarizeBy.Year:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Year,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName
                            });
                            break;
                        default:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Month,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName
                            });
                            break;
                    }

                    finalQuery = groupedQuery.Select(x =>
                        new SalesHistoryGroup
                        {
                            SalesHistoryid = 0,
                            EndDate = x.Key.EndDate ?? DateTime.Now,
                            Quantity = x.Sum(y => y.Quantity),
                            TotalPrice = Math.Round(x.Sum(y => y.AveragePrice ?? 0), 2) / x.Count(),
                            TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice ?? 0) / x.Count(), 2),
                            ProductName = "",
                            PartNumber = "",
                            ClientProductId = "",
                            BrandName = "",
                            CategoryName = x.Key.CategoryName,
                            RuleName = ""
                        });

                    break;
                case GroupBy.Brand:
                    switch (summarizeByPeriod)
                    {
                        case SummarizeBy.Day:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Day,
                                GroupingId = x.ProductInventory.BrandFK,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                        case SummarizeBy.Year:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Year,
                                GroupingId = x.ProductInventory.BrandFK,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                        default:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Month,
                                GroupingId = x.ProductInventory.BrandFK,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                    }

                    finalQuery = groupedQuery.Select(x =>
                        new SalesHistoryGroup
                        {
                            SalesHistoryid = 0,
                            EndDate = x.Key.EndDate ?? DateTime.Now,
                            Quantity = x.Sum(y => y.Quantity),
                            TotalPrice = Math.Round(x.Sum(y => y.AveragePrice ?? 0), 2) / x.Count(),
                            TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice ?? 0) / x.Count(), 2),
                            ProductName = "",
                            PartNumber = "",
                            ClientProductId = "",
                            BrandName = x.Key.BrandName,
                            CategoryName = "",
                            RuleName = ""
                        });

                    break;
                case GroupBy.Rule:
                    switch (summarizeByPeriod)
                    {
                        case SummarizeBy.Day:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Day,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                        case SummarizeBy.Year:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Year,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                        default:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Month,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                    }

                    finalQuery = groupedQuery.Select(x =>
                        new SalesHistoryGroup
                        {
                            SalesHistoryid = 0,
                            EndDate = x.Key.EndDate ?? DateTime.Now,
                            Quantity = x.Sum(y => y.Quantity),
                            TotalPrice = Math.Round(x.Sum(y => y.AveragePrice ?? 0), 2) / x.Count(),
                            TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice ?? 0) / x.Count(), 2),
                            ProductName = "",
                            PartNumber = "",
                            ClientProductId = "",
                            BrandName = "",
                            CategoryName = "",
                            RuleName = x.Key.RuleName
                        });

                    break;
                case GroupBy.CategoryThenBrand:
                    switch (summarizeByPeriod)
                    {
                        case SummarizeBy.Day:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Day,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                        case SummarizeBy.Year:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Year,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                        default:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Month,
                                GroupingId = x.ProductInventory.ProductCategoryFK,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                BrandName = x.ProductInventory.Brand.BrandName
                            });
                            break;
                    }

                    finalQuery = groupedQuery.Select(x =>
                        new SalesHistoryGroup
                        {
                            SalesHistoryid = 0,
                            EndDate = x.Key.EndDate ?? DateTime.Now,
                            Quantity = x.Sum(y => y.Quantity),
                            TotalPrice = Math.Round(x.Sum(y => y.AveragePrice ?? 0), 2) / x.Count(),
                            TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice ?? 0) / x.Count(), 2),
                            ProductName = "",
                            PartNumber = "",
                            ClientProductId = "",
                            BrandName = x.Key.BrandName,
                            CategoryName = x.Key.CategoryName,
                            RuleName = ""
                        });

                    break;
                default:
                    switch (summarizeByPeriod)
                    {
                        case SummarizeBy.Day:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Day,
                                GroupingId = x.ProductInventoryFk,
                                ProductName = x.ProductInventory.Description,
                                PartNumber = x.ProductInventory.ManufacturerPartNo,
                                ClientProductId = x.ProductInventory.ClientProductID,
                                BrandName = x.ProductInventory.Brand.BrandName,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                        case SummarizeBy.Year:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Year,
                                GroupingId = x.ProductInventoryFk,
                                ProductName = x.ProductInventory.Description,
                                PartNumber = x.ProductInventory.ManufacturerPartNo,
                                ClientProductId = x.ProductInventory.ClientProductID,
                                BrandName = x.ProductInventory.Brand.BrandName,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                        default:
                            groupedQuery = baseQuery.GroupBy(x => new SalesHistoryGroupByEntity
                            {
                                EndDate = x.Month,
                                GroupingId = x.ProductInventoryFk,
                                ProductName = x.ProductInventory.Description,
                                PartNumber = x.ProductInventory.ManufacturerPartNo,
                                ClientProductId = x.ProductInventory.ClientProductID,
                                BrandName = x.ProductInventory.Brand.BrandName,
                                CategoryName = x.ProductInventory.ProductCategory.CategoryName,
                                RuleName = x.ProductInventory.PriceRule.RuleName
                            });
                            break;
                    }

                    finalQuery = groupedQuery.Select(x =>
                        new SalesHistoryGroup
                        {
                            SalesHistoryid = 0,
                            EndDate = x.Key.EndDate ?? DateTime.Now,
                            Quantity = x.Sum(y => y.Quantity),
                            TotalPrice = Math.Round(x.Sum(y => y.AveragePrice ?? 0) / x.Count(), 2),
                            TotalCostPrice = Math.Round(x.Sum(y => y.AverageCostPrice ?? 0) / x.Count(), 2),
                            ProductName = x.Key.ProductName,
                            PartNumber = x.Key.PartNumber,
                            ClientProductId = x.Key.ClientProductId,
                            BrandName = x.Key.BrandName,
                            CategoryName = x.Key.CategoryName,
                            RuleName = x.Key.RuleName
                        });

                    break;
            }

            finalQuery = finalQuery.Where(x => x.EndDate >= dateFrom && x.EndDate <= dateTo && x.ProductName != null);
            return finalQuery;
        }
    }

    public class SalesHistoryGroup
    {
        public long SalesHistoryid { get; set; }
        public DateTime EndDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? TotalCostPrice { get; set; }
        public string PartNumber { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string ClientProductId { get; set; }
        public string BrandName { get; set; }
        public string CategoryName { get; set; }
        public string RuleName { get; set; }
    }

    public class SalesHistoryGroupByEntity
    {
        public long? GroupingId { get; set; }
        public DateTime? EndDate { get; set; }
        public string CategoryName { get; set; }
        public string ProductName { get; set; }
        public string PartNumber { get; set; }
        public string ClientProductId { get; set; }
        public string BrandName { get; set; }
        public string RuleName { get; set; }
        public string AdditionalGrouping { get; set; }
    }
}

