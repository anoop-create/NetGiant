using Google.Analytics.Data.V1Beta;
using Google.Api.Gax.Grpc;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.Apis
{
    public class GoogleAnalytics
    {
        public GoogleAnalytics()
        {
            string creds = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "GoogleAnalyticsApiCredentials").FirstOrDefault().settingValue;
            Client = new BetaAnalyticsDataClientBuilder
            {
                JsonCredentials = creds
            }.Build();
        }

        public BetaAnalyticsDataClient Client { get; set; }

        public DataTable GetTransactions(DateTime startDate)
        {
            DataTable gaData = new DataTable();
            gaData.Columns.Add("OrderNumber", Type.GetType("System.String"));
            gaData.Columns.Add("Source", Type.GetType("System.String"));
            gaData.Columns.Add("Medium", Type.GetType("System.String"));
            gaData.Columns.Add("Campaign", Type.GetType("System.String"));

            // Test data
            //if (Properties.Settings.Default.Environment != "Live")
            //{
            //    gaData.Rows.Add("C00HQ", "google", "cpc", "Dynamic Search Ads - HP");
            //    gaData.Rows.Add("C03EX", "bing", "Organic", "(not set)");
            //    return gaData;
            //}

            // Set range for 7 days worth of data from yesterday
            DateTime end = startDate.AddDays(-1);
            DateTime start = end.AddDays(-7);
            //start = end.AddDays(-100);   // <== Testing
            List<DateRange> dateRanges = new List<DateRange>();
            dateRanges.Add(new DateRange()
            {
                StartDate = start.Year.ToString() + "-" + start.Month.ToString("D2") + "-" + start.Day.ToString("D2"),
                EndDate = end.Year.ToString() + "-" + end.Month.ToString("D2") + "-" + end.Day.ToString("D2")
            });

            // Dimensions
            List<Dimension> dimensions = new List<Dimension>();
            dimensions.Add(new Dimension() { Name = "transactionid" });
            dimensions.Add(new Dimension() { Name = "sessionSource" });
            dimensions.Add(new Dimension() { Name = "sessionMedium" });
            dimensions.Add(new Dimension() { Name = "sessionCampaignName" });
            dimensions.Add(new Dimension() { Name = "date" });

            // Filter: ony select purchase events
            FilterExpression filter = new FilterExpression(new FilterExpression
            {
                Filter = new Filter
                {
                    FieldName = "eventName",
                    StringFilter = new Filter.Types.StringFilter
                    {
                        MatchType = Filter.Types.StringFilter.Types.MatchType.Exact,
                        Value = "purchase"
                    }
                }
            });

            string[] propertyId = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "GoogleAnalyticsPropertyId").FirstOrDefault().settingValue.Split('|');
            int limit = 10000;
            foreach (string id in propertyId)
            {
                BatchRunReportsRequest reportRequests = new BatchRunReportsRequest();

                RunReportRequest r = new RunReportRequest();
                r.Property = "properties/" + id;
                r.DateRanges.Add(dateRanges);
                r.Dimensions.Add(dimensions);
                r.DimensionFilter = filter;
                //r.Offset = 0;     // <== Specifying this at 0 seems to (sometimes) cause the API to time out
                //r.Limit = limit;  // <== Specifying this < 10,000 seems to cause the API to time out

                // Retrieve results: only the first 10000 records are retrieved by each request
                bool moreAvailable = true;
                try
                {
                    RunReportResponse reportResponse = Client.RunReport(r);
                    int processedCount = 0;
                    while (moreAvailable && processedCount < reportResponse.RowCount)
                    {
                        // Process results
                        foreach (Row rr in reportResponse.Rows)
                        {
                            gaData.Rows.Add(
                                rr.DimensionValues[0].Value
                                , rr.DimensionValues[1].Value
                                , rr.DimensionValues[2].Value
                                , rr.DimensionValues[3].Value == "(not set)" ? null : rr.DimensionValues[3].Value
                                );
                            processedCount += 1;
                        }

                        if (processedCount < reportResponse.RowCount)
                        {
                            r.Offset = r.Offset + limit;
                            reportResponse = Client.RunReport(r);
                        }
                        else
                        {
                            moreAvailable = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    StandardFunctions.WriteException(e);
                }
            }

            //CsvUtilities.WriteCsvFromDataTable(gaData, "C:\\Temp\\gadata.csv"); // <== Testing

            return gaData;
        }
    }
}
