using MailChimp.Net;
using MailChimp.Net.Core;
using MailChimp.Net.Models;
using netGiant.Intranet.DataLayer.CustomerData;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class MailingLists
    {
        public void LoadList(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            List<StagingMailingList> lml = MailChimpGetList(parms).Result;
            if (lml.Count() > 0)
            {
                try
                {
                    if (StandardFunctions.BulkInsertMailingList(lml))
                    {
                        List<SqlParameter> sqlParms = new List<SqlParameter>();
                        SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.VarChar);
                        sqlParm.Value = Int32.Parse(parms["websiteid"]);
                        sqlParms.Add(sqlParm);

                        SQLUtilities.ExecuteStoredProcedure("customersqldata", "dbo.UpdateMailingList", sqlParms);
                    }
                    else
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: error during insertion loop", ErrorCode = "ERROR" });
                        errorHasOccurred = true;
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: error during insertion loop", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                    errorHasOccurred = true;
                }

                if (!errorHasOccurred)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = lml.Count + " records successfully added" });
                }
            }
            else
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: unable to delete old mailing list", ErrorCode = "ERROR" });
                errorHasOccurred = true;
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private async Task<List<StagingMailingList>> MailChimpGetList(Dictionary<string, string> parms)
        {
            List<StagingMailingList> lml = new List<StagingMailingList>();
            string apikey = EntityFunctions.GetNgmdCMSEntry(Int32.Parse(parms["websiteid"]), "MiscData", "MailChimpApiKey");
            string listid = EntityFunctions.GetNgmdCMSEntry(Int32.Parse(parms["websiteid"]), "MiscData", "MailChimpListId");

            MailChimpManager mcm = new MailChimpManager(apikey);

            try
            {
                int offset = 0;
                bool moreAvailable = true;
                while (moreAvailable)
                {
                    var members = await mcm.Members.GetAllAsync(listid, new MemberRequest
                    {
                        Status = Status.Subscribed,
                        Limit = 1000,
                        Offset = offset
                    }).ConfigureAwait(false);

                    lml.AddRange(members.Select(x => new StagingMailingList
                    {
                        MailingListId = 0,
                        WebsiteFk = Int32.Parse(parms["websiteid"]),
                        EmailAddress = x.EmailAddress.ToLower()
                    }));

                    if (members.Count() == 1000)
                    {
                        offset += 1000;
                    }
                    else
                    {
                        moreAvailable = false;
                    }
                }
            }
            catch (MailChimpException mce)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MAILCHIMP ERROR: " + mce.Detail + " " + mce.StackTrace, ErrorCode = "ERROR" });
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MAILCHIMP ERROR: " + ex.Message + " " + ex.StackTrace, ErrorCode = "ERROR" });
            }

            return lml;
        }
    }
}
