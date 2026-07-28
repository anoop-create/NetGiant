using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;

namespace DP001BusinessLogic.Pricing
{
    public class VerifyPrices
    {
        private Tenant _tenant;
        private int _channelId;
        private Channel _channel;
        private string _email;

        public VerifyPrices(Dictionary<string, string> parms)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "START2 Constructor " + parms["channelid"], "Information", true);
            _channelId = Convert.ToInt32(parms["channelid"]);
            _tenant = new Tenant();
            _channel = _tenant.GetChannelRecord(_channelId);
            _email = "service.admin@netgiant.com";
            CommonDataFunctions.CreateLogEntry(_channel, "END Constructor", "Information", true);
        }

        public void Verify()
        {
            DataSet ds = new DataSet("verifyprices");

            CommonDataFunctions.CreateLogEntry(_channel, "START VerifyPrices", "Information");

            List<SqlParameter> sqlparms = new List<SqlParameter>();
            SqlParameter parm1 = new SqlParameter("@ChannelID", SqlDbType.Int);
            parm1.Value = _channelId;
            sqlparms.Add(parm1);

            ds = SQL.ExecuteReadStoredProcedure("DP001", "VerifyPriceChanges", sqlparms, "verifyprices");

            if (ds.Tables.Count > 0)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "START VerifyPrices Email", "Information");
                CommonDataFunctions.CreateLogEntry(_channel, "Sending Email to: " + _email, "Information");

                var subject = "Price Verification Exception for : " + _channel.ChannelName;
                var body = "<div>Please find attached pricing exeptions for the channel: <b>" + _channel.ChannelName + "</b></div>" +
                            "<div>Below is a sample of the complete set of exceptions</div>" +
                            "<div>Total number of exceptions: <b>" + ds.Tables[1].Rows[0]["ExceptionCount"] + "</b></div><br /><br >";
                var emailTo = new List<string> { _email };

                var memoryStream = new MemoryStream();

                try
                {
                    var writer = new Csv.CsvFileWriter(memoryStream, '\t');

                    body += ProcessDataSet(ds, writer);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    CommonDataFunctions.CreateLogEntry(_channel, "Could not create in memory csv. ERROR: " + e.StackTrace, "Notification", true);
                }

                Email.SendEmail(body, subject, emailTo, "noreply@priceology.netgiant.com", memoryStream, "Exceptions");
            }
            CommonDataFunctions.CreateLogEntry(_channel, "END VerifyPrices", "Information");
        }

        private static string ProcessDataSet(DataSet ds, Csv.CsvFileWriter writer)
        {
            string body = "";
            for (int i = 0; i < 1; i++)
            {
                DataTable dt = ds.Tables[i];

                body += "<table>";
                bool firstRow = true;
                int counter = 0;
                foreach (DataRow dr in dt.Rows)
                {

                    if (firstRow)
                    {
                        var header = new Csv.CsvRow();
                        body += "<thead>";
                        foreach (DataColumn dc in dt.Columns)
                        {
                            header.Add(dc.ColumnName);
                            body += "<th>" + dc.ColumnName + "</th>";
                        }
                        writer.WriteRow(header);
                        body += "</thead>";
                    }
                    firstRow = false;

                    var newRow = new Csv.CsvRow();
                    foreach (DataColumn dc in dt.Columns)
                    {
                        newRow.Add(dr[dc.ColumnName].ToString());
                    }
                    writer.WriteRow(newRow);

                    if (counter < 50)
                    {
                        body += "<tr>";
                        foreach (DataColumn dc in dt.Columns)
                        {
                            newRow.Add(dr[dc.ColumnName].ToString());
                            body += "<td>" + dr[dc.ColumnName].ToString() + "</td>";
                        }
                        body += "</tr>";
                    }

                    counter += 1;
                }

                writer.WriteRow(new Csv.CsvRow());
                writer.WriteRow(new Csv.CsvRow());
                writer.WriteRow(new Csv.CsvRow());
                body += "</table><br /><br /><br />";
            }

            return body;
        }
    }
}
