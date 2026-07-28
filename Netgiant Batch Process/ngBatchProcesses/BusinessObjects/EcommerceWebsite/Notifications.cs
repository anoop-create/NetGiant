using Nest;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class Notifications
    {
        public Notifications(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
        }

        StringBuilder Body = new StringBuilder();

        public void Notify()
        {
            // Notify about upcoming Events in the next week
            DateTime today = DateTime.Now.Date;
            DateTime ed = today.Date.AddDays((int)DayOfWeek.Monday - (int)today.DayOfWeek + 14);
            List<Notification> ln = new List<Notification>();

            // Get Events starting next week
            List<Event> ev = EntityFunctions.GetEvent(x => x.DateActive > today && x.DateActive < ed);
            ln.Clear();
            ln.AddRange(ev.Select(x => new Notification
            {
                Type = "FutureEvent",
                Website = x.Website.Abbreviation,
                Start = x.DateActive,
                End = x.DateInactive,
                Ident = x.EventName,
                Description = x.Description
            }));

            Body.Append(new TagBuilder("h2") { InnerHtml = "Upcoming Events"} );
            BuildTable(ln);

            // Get Promotional Vouchers starting next week
            List<VoucherPromo> vp = EntityFunctions.GetVouchers(x => x.AccountNumber == null && x.ForGeneralUse == true && x.ValidFrom > today && x.ValidFrom < ed);
            ln.Clear();
            ln.AddRange(vp.Select(x => new Notification
            {
                Type = "FutureVoucher",
                Website = x.Website.Abbreviation,
                Start = x.ValidFrom,
                End = x.ValidTo,
                Ident = x.VoucherCode,
                Description = x.Description
            }));

            Body.Append(new TagBuilder("h2") { InnerHtml = "Upcoming Voucher Launches" });
            BuildTable(ln);

            // Get Active Events
            ev = EntityFunctions.GetEvent(x => x.IsActive == true).ToList();
            ln.Clear();
            ln.AddRange(ev.Select(x => new Notification
            {
                Type = "CurrentEvent",
                Website = x.Website.Abbreviation,
                Start = x.DateActive,
                End = x.DateInactive,
                Ident = x.EventName,
                Description = x.Description
            }));

            Body.Append(new TagBuilder("h2") { InnerHtml = "Current Events" });
            BuildTable(ln);


            // Get Active Promotional Vouchers
            vp = EntityFunctions.GetVouchers(x => x.AccountNumber == null && x.ForGeneralUse == true && x.ValidFrom < today && x.ValidTo > today);
            ln.Clear();
            ln.AddRange(vp.Select(x => new Notification
            {
                Type = "CurrentVoucher",
                Website = x.Website.Abbreviation,
                Start = x.ValidFrom,
                End = x.ValidTo,
                Ident = x.VoucherCode,
                Description = x.Description
            }));

            Body.Append(new TagBuilder("h2") { InnerHtml = "Current Vouchers" });
            BuildTable(ln);

            // Send the email
            Email.SendEmail(
                new List<string>() { "service.admin@netgiant.com" },
                "notifications@netgiant.com",
                "NetGiant Notifications",
                Body.ToString(),
                true);


            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private void BuildTable(List<Notification> ln)
        {
            if (ln.Count > 0)
            {
                TagBuilder table = new TagBuilder("table");
                TagBuilder tr = new TagBuilder("tr");
                TagBuilder td = new TagBuilder("td");
                TagBuilder th = new TagBuilder("th");

                table.Attributes.Add("cellpadding", "5px");
                table.Attributes.Add("cellspacing", "0");
                th.Attributes.Add("style", "border: 1px solid black;");
                td.Attributes.Add("style", "border: 1px solid black;");

                StringBuilder sb = new StringBuilder();

                // Headers                
                th.InnerHtml = "Website";
                tr.InnerHtml += th.ToString();
                th.InnerHtml = "Title";
                tr.InnerHtml += th.ToString();
                th.InnerHtml = "Start";
                tr.InnerHtml += th.ToString();
                th.InnerHtml = "End";
                tr.InnerHtml += th.ToString();
                th.InnerHtml = "Description";
                tr.InnerHtml += th.ToString();

                sb.Append(tr.ToString());

                // Data
                foreach (Notification n in ln.OrderBy(x => x.Website).ThenBy(x => x.Ident))
                {
                    tr.InnerHtml = "";
                    td.InnerHtml = n.Website;
                    tr.InnerHtml += td.ToString();
                    td.InnerHtml = n.Ident;
                    tr.InnerHtml += td.ToString();
                    td.InnerHtml = n.Start.ToString("ddd d MMM yyyy");
                    tr.InnerHtml += td.ToString();
                    td.InnerHtml = n.End.ToString("ddd d MMM yyyy");
                    tr.InnerHtml += td.ToString();
                    td.InnerHtml = n.Description;
                    tr.InnerHtml += td.ToString();

                    sb.Append(tr.ToString());
                }

                table.InnerHtml = sb.ToString();
                Body.Append(table);
            }
            else
            {
                Body.Append("None found");
            }
        }

        private class Notification
        {
            public string Type { get; set; }
            public string Website { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public string Ident { get; set; }
            public string Description { get; set; }
        }
    }
}
