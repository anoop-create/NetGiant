using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Axis;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Caching;

namespace ngBatchProcesses.BusinessObjects
{
    public class ProcessTrackingEmails
    {
        public ProcessTrackingEmails(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            if (!Parms.ContainsKey("action"))
            {
                Parms.Add("action", "");
            }

            RootDirectory = Properties.Settings.Default.LocalDirectory;
            WorkingPath = RootDirectory + "DeliveryTracking\\New\\";
            ArchivePath = RootDirectory + "DeliveryTracking\\Archive\\";

            LoadArchiveDirectoryInfo();
            TGTemplate = EntityFunctions.GetNgmdCMSEntry(1, "EmailData", "TrackingLinkEmail");
            CMTemplate = EntityFunctions.GetNgmdCMSEntry(2, "EmailData", "TrackingLinkEmail");
            TGFrom = EntityFunctions.GetNgmdCMSEntry(1, "CheckoutData", "SalesEmail");
            CMFrom = EntityFunctions.GetNgmdCMSEntry(2, "CheckoutData", "SalesEmail");

            DateTime dt = new DateTime();
            if (!DateTime.TryParse(GetLastRunDate().settingValue, out dt))
            {
                dt = DateTime.Now - TimeSpan.FromDays(1);
            }
            LastRunDate = dt;
        }
        private Dictionary<string, string> Parms { get; set; }
        private string RootDirectory { get; set; }
        private string WorkingPath { get; set; }
        private string ArchivePath { get; set; }
        private int recordCount { get; set; } = 0;
        private int emailCount { get; set; } = 0;
        private int ftpEmailCount { get; set; } = 0;

        //private List<EmailTemplatesStruct> EmailTemps = new List<EmailTemplatesStruct>();
        private DataTable dtProductLines = new DataTable();
        private List<FileDate> ArchiveContents {get; set;}
        private List<provider> lCour = new List<provider>();
        private DateTime LastRunDate { get;set; }
        private DateTime FtpFileDate { get; set; }
        private string TGTemplate { get; set; }
        private string CMTemplate { get; set; }
        private string TGFrom { get; set; }
        private string CMFrom { get; set; }
        private bool TGSample { get; set; } = false;
        private bool CMSample { get; set; } = false;

        public void Process()
        {
            StandardFunctions.WriteProcessStarted();

            // Build a list of ftp files to download
            List<provider> lprov = EntityFunctions.GetProviderList(x => x.active, "Dispatch Supplier");
            lCour = EntityFunctions.GetProviderList(x => x.active, "Courier");
            StandardFunctions stnFunc = new StandardFunctions();

            foreach (provider p in lprov)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processing Provider: " + p.providerName });

                List<fieldMapping> lfm = p.fieldMapping.ToList();
                DataTable dt = new DataTable();
                DataTable dtEmail = new DataTable();

                dt.Columns.Add("orderNumber", typeof(string));
                dt.Columns.Add("firstName", typeof(string));
                dt.Columns.Add("surname", typeof(string));
                dt.Columns.Add("shortName", typeof(string));
                dt.Columns.Add("trackingLink", typeof(string));
                dt.Columns.Add("productRows", typeof(string));
                dt.Columns.Add("signature", typeof(string));
                dt.Columns.Add("purchaseOrderNumber", typeof(string));
                dt.Columns.Add("courier", typeof(string));
                dt.Columns.Add("emailAddress", typeof(string));
                dt.Columns.Add("trackingNumber", typeof(string));
                dt.Columns.Add("customerRef", typeof(string));
                dt.Columns.Add("customerGroup", typeof(int));
                dt.Columns.Add("groupDescription", typeof(string));
                dt.Columns.Add("productDescription", typeof(string));
                dt.Columns.Add("productQuantity", typeof(string));

                dt.Columns["orderNumber"].DefaultValue = "";
                dt.Columns["firstName"].DefaultValue = "";
                dt.Columns["surname"].DefaultValue = "";
                dt.Columns["shortName"].DefaultValue = "";
                dt.Columns["trackingLink"].DefaultValue = "";
                dt.Columns["productRows"].DefaultValue = "";
                dt.Columns["signature"].DefaultValue = "";
                dt.Columns["purchaseOrderNumber"].DefaultValue = "";
                dt.Columns["courier"].DefaultValue = "";
                dt.Columns["emailAddress"].DefaultValue = "";
                dt.Columns["trackingNumber"].DefaultValue = "";
                dt.Columns["customerRef"].DefaultValue = "";
                dt.Columns["customerGroup"].DefaultValue = 0;
                dt.Columns["groupDescription"].DefaultValue = "";
                dt.Columns["productDescription"].DefaultValue = "";
                dt.Columns["productQuantity"].DefaultValue = "";

                foreach (ftpDetails ftpd in p.ftpDetails)
                {
                    try
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processing " + ftpd.ftpFilename + " for FTP No. " + ftpd.ftpDetailID.ToString() });

                        string newFilename = ftpd.ftpDetailID.ToString() + "_" + ftpd.ftpFilename;
                        if (Parms["action"] != "bypassfilechecks" && FileAlreadyProcessedOrFileNotAvailable(ftpd, newFilename))
                        {
                            continue;
                        }

                        // Download the file
                        string ftpFilename = (String.IsNullOrEmpty(ftpd.ftpFolder) ? "" : "/" + ftpd.ftpFolder + "/") + ftpd.ftpFilename;
                        Tuple<bool, string> rtn = FtpUtilities.DownloadFTPFile(
                            ftpd.ftpHost,
                            ftpd.ftpUser,
                            ftpd.ftpPassword,
                            ftpFilename,
                            WorkingPath + newFilename,
                            false);
                        if (rtn.Item1)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FluentFTP Download Successful for: " + ftpFilename });
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR FluentFTP unable to Download FTP File: " + ftpFilename + ". " + rtn.Item2, ErrorCode = "ERROR" });
                        }
                        FtpFileDate = FtpUtilities.FileLastModifiedDate;

                        if (File.Exists(WorkingPath + newFilename))
                        {
                            // Forward the file to purchasing
                            List<string> toAddresses = new List<string>
                            {
                                "purchasing@netgiant.com"
                            };
                            string subject = p.providerName + " Delivery Tracking Information " + DateTime.Now.ToShortDateString();
                            string body = "Dispatch tracking data for " + p.providerDesc + " on " + DateTime.Now.ToShortDateString();

                            Email.SendEmail(toAddresses, "service.admin@netgiant.com", subject, body, true, WorkingPath + newFilename);
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Email Sent to purchasing" });

                            // Process the file
                            string filetype = newFilename.Split('.')[1].ToLower();
                            DataTable dtDispatchReport = new DataTable();
                            switch (filetype)
                            {
                                case "xlsx":
                                case "xls":
                                    {
                                        dtDispatchReport = ExcelUtilities.LoadWorksheetInDataTable(WorkingPath + newFilename);
                                        break;
                                    }
                                case "csv":
                                    {
                                        dtDispatchReport = CsvUtilities.LoadCsvInDataTable(WorkingPath + newFilename);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            dt.Clear();
                            dt = StandardFunctions.RationaliseTable(dtDispatchReport, dt, lfm);
                        }

                        // Eliminate duplicates
                        if (dt.Rows.Count > 0)
                        {
                            dtEmail = dt.AsEnumerable()
                               .GroupBy(r => new { Col1 = r["purchaseOrderNumber"] })
                               .Select(g => g.OrderBy(r => r["purchaseOrderNumber"]).First())
                               .CopyToDataTable();
                        }

                        // Process DataTable                
                        using (ngmdEntities db = new ngmdEntities())
                        {
                            foreach (DataRow dr in dtEmail.Rows)
                            {
                                recordCount += 1;
                                string poNo = dr["purchaseOrderNumber"].ToString().Trim();
                                if (poNo != "")
                                {
                                    // Populate dtFileLines if possible
                                    dtProductLines.Clear();
                                    dtProductLines = dt.AsEnumerable()
                                        .Where(x => x["purchaseOrderNumber"].ToString() == dr["purchaseOrderNumber"].ToString())
                                        .CopyToDataTable();

                                    // We need to retrieve order no., email address and name from DB
                                    string sql = @"SELECT A.email AS [emailAddress],
                                        A.fornm AS [firstName],
                                        A.surnm AS [surname],
                                        C.snm AS [shortName],
                                        O.drf AS [orderNumber],
                                        C.cusrf AS [customerRef],
                                        C.grp AS [customerGroup],
                                        '' AS [groupDescription],
                                        O.odt AS [orderDate],
                                        CASE
                                            WHEN O.csg IN (10,11,12,14) THEN 1
                                            WHEN O.csg IN (0,1,3) THEN 2
                                            WHEN O.csg IN (5,6,7) THEN 3
                                            ELSE 1
                                        END AS [websiteId]
                                    FROM [AXIS14080CO1].[dbo].accpom00 PO
                                    INNER JOIN [AXIS14080CO1].[dbo].accsom00 O ON O.drf = PO.oso
                                    INNER JOIN [AXIS14080CO1].[dbo].acccus01 C ON C.cusrf = O.cusrf
                                    INNER JOIN [AXIS14080CO1].[dbo].accaad01 A ON A.adref = O.cusrf AND A.no = O.conno
                                    WHERE PO.pon = '" + poNo + @"'
                                    ";

                                    DataTable axisData = SQLUtilities.ExecuteReadInline("axisdiplomat", sql, "ds", 60).Tables[0];
                                    provider cour = new provider();
                                    if (!string.IsNullOrEmpty(dr["courier"].ToString()))
                                    {
                                        cour = lCour.FirstOrDefault(x => x.providerName.Contains(dr["courier"].ToString()));
                                    }
                                    else
                                    {
                                        cour = lCour.FirstOrDefault(x => x.providerName.Contains("Dummy Courier"));
                                    }
                                    if (axisData.Rows.Count > 0 && cour != null)
                                    {
                                        CustomerDetailsStruct cds = new CustomerDetailsStruct();
                                        cds.CustRef = axisData.Rows[0]["customerRef"].ToString();
                                        cds.Email = axisData.Rows[0]["emailAddress"].ToString();
                                        cds.Firstname = axisData.Rows[0]["firstName"].ToString();
                                        cds.Surname = axisData.Rows[0]["surname"].ToString();
                                        cds.CustShortName = axisData.Rows[0]["shortName"].ToString();
                                        cds.GroupCode = int.Parse(axisData.Rows[0]["customerGroup"].ToString());
                                        cds.WebsiteId = int.Parse(axisData.Rows[0]["websiteId"].ToString());
                                        cds.GroupDesc = axisData.Rows[0]["groupDescription"].ToString();
                                        cds.OrdNo = axisData.Rows[0]["orderNumber"].ToString();
                                        cds.Courier = cour.providerDesc;

                                        if (cds.Email != "")
                                        {
                                            cds.TrackingLink = string.IsNullOrEmpty(cour.url) ? dr["trackingLink"].ToString() : cour.url + dr["trackingNumber"].ToString();
                                            SendTrackingEmail(cds);

                                            try
                                            {
                                                OrderTracking ot = new OrderTracking
                                                {
                                                    WebsiteFk = int.Parse(axisData.Rows[0]["websiteId"].ToString()),
                                                    CourierFk = cour.providerID,
                                                    OrderNumber = cds.OrdNo,
                                                    PurchaseOrderNumber = poNo,
                                                    OrderDate = DateTime.Parse(axisData.Rows[0]["orderDate"].ToString()),
                                                    CustomerRef = cds.CustRef,
                                                    Email = cds.Email,
                                                    FirstName = cds.Firstname,
                                                    Surname = cds.Surname,
                                                    TrackingCode = dr["trackingNumber"].ToString(),
                                                    TrackingLink = cds.TrackingLink,
                                                    IsSent = true
                                                };
                                                db.Entry(ot).State = EntityState.Added;
                                            }
                                            catch (Exception e)
                                            {
                                                StandardFunctions.WriteException(e);
                                            }
                                        }
                                        else
                                        {
                                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Email not sent for " + poNo });
                                            continue;
                                        }
                                    }
                                }
                            }
                            try
                            {
                                db.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to save OrderTracking for Provider: " + p.providerName + ", Filename: " + ftpd.ftpFilename, ErrorCode = "ERROR" });
                                StandardFunctions.WriteException(ex);
                            }
                        }

                        ftpd.dateLastFeedFile = FtpFileDate;
                        if (!EntityFunctions.SaveFtpDetails(ftpd))
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to update FTP Feed File Date", ErrorCode = "WARNING" });
                        }
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteException(ex);
                    }

                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Emails Sent for FTP No. " + ftpd.ftpDetailID.ToString() + " = " + ftpEmailCount.ToString() });
                    ftpEmailCount = 0;
                }
            }
            stnFunc = null;
            if (!SetLastRunDate())
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error attempting to set LastRunDate", ErrorCode = "ERROR" });
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count = " + recordCount.ToString() + ", Emails Sent = " + emailCount.ToString() });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private void SendTrackingEmail(CustomerDetailsStruct custDetails)
        {
            try
            {
                string body = custDetails.WebsiteId == 1 ? TGTemplate : CMTemplate;
                string from = custDetails.WebsiteId == 1 ? TGFrom : CMFrom;
                List<string> toAddresses = new List<string>
                {
                    custDetails.Email
                };
                string subject = "Delivery Tracking Information";
                body = ReplaceTemplateValues(body, custDetails);

                // Send samples of the email
                string bcc = "";
                if (!TGSample && custDetails.WebsiteId == 1)
                {
                    TGSample = true;
                    bcc = "devteam@netgiant.com";
                }
                if (!CMSample && custDetails.WebsiteId == 2)
                {
                    CMSample = true;
                    bcc = "devteam@netgiant.com";
                }

                Email.SendEmail(toAddresses, from, subject, body, true, "", bcc);

                emailCount += 1;
                ftpEmailCount += 1;
                //StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Email Sent to - " + custDetails.Email });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Email not sent to - " + custDetails.Email + "- " + " Cust Ref=" + custDetails.CustRef, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        private string ReplaceTemplateValues(string emailTemplate, CustomerDetailsStruct custDetails)
        {
            emailTemplate = emailTemplate.Replace("[ordernumber]", custDetails.OrdNo)
                .Replace("[firstname]", custDetails.Firstname)
                .Replace("[trackinglink]", custDetails.TrackingLink)
                .Replace("[courier]", custDetails.Courier)
                .Replace("[productsrows]", GetProducts());
            return emailTemplate;
        }

        private string GetProducts()
        {
            StringBuilder sbProducts = new StringBuilder();

            if (dtProductLines.Rows.Count > 0)
            {
                foreach (DataRow row in dtProductLines.Rows)
                {
                    if (row["productDescription"].ToString() != "")
                    {
                        if (sbProducts.Length == 0)
                        {
                            sbProducts.AppendLine("<tr><td>The following items are included in this shipment;<br/><br/></td></tr><tr><td>");
                        }
                        sbProducts.AppendLine("<p>");
                        sbProducts.AppendLine(row["productQuantity"].ToString());
                        sbProducts.AppendLine(" x ");
                        sbProducts.AppendLine(row["productDescription"].ToString());
                        sbProducts.AppendLine("</p>");

                        sbProducts.AppendLine("<br/></td></tr>");
                    }
                }
            }

            return sbProducts.ToString();
        }

        private bool FileAlreadyProcessedOrFileNotAvailable(ftpDetails ftpd, string newFilename)
        {
            // Check that we haven't already processing this file today
            DateTime today = DateTime.Now.Date;
            if (ArchiveContents.Where(x => x.FileName.Contains(newFilename) && x.DateCreated == today).FirstOrDefault() != null)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "File not processed: Already processed today." });
                return true;
            }

            // Check that the ftp location contains a file that was created since the last run
            string ftpPath = (String.IsNullOrEmpty(ftpd.ftpFolder) ? "" : "/" + ftpd.ftpFolder + "/") + ftpd.ftpFilename;
            DateTime ftpDate =  FtpUtilities.ExtractFileTimeStamp(ftpd.ftpHost,
                                                    ftpd.ftpUser,
                                                    ftpd.ftpPassword,
                                                    ftpPath);
            if (ftpDate < LastRunDate)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "File not processed: Latest FTP file has already been processed." });
                return true;
            }

            return false;
        }


        private void LoadArchiveDirectoryInfo()
        {
            string[] files = Directory.GetFiles(ArchivePath);
            ArchiveContents = new List<FileDate>();
            foreach (string filepath in files)
            {
                ArchiveContents.Add(new FileDate { FileName = filepath, DateCreated = File.GetCreationTime(filepath).Date }); 
            }
        }

        private configurationSetting GetLastRunDate()
        {
            return EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "TrackingEmailsLastRunDate").FirstOrDefault();
        }

        private bool SetLastRunDate()
        {
            configurationSetting cs = GetLastRunDate();
            cs.settingValue = DateTime.Now.ToString();

            return EntityFunctions.SaveConfigurationSetting(cs);
        }

        //Declare structs
        private class FileDate
        {
            public string FileName;
            public DateTime DateCreated;
        }

        private struct CustomerDetailsStruct
        {
            public string Firstname;
            public string Surname;
            public string Email;
            public int? GroupCode;
            public int WebsiteId;
            public string GroupDesc;
            public string CustRef;
            public string OrdNo;
            public string CustShortName;
            public string Courier;
            public string TrackingLink;
        }
    }
}
