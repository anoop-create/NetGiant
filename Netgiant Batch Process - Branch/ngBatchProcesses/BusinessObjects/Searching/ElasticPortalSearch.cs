using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using Nest;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.Searching
{
    public class ElasticPortalSearch
    {
        public ElasticPortalSearch()
        {
            Node = new Uri(StandardFunctions.GetConfigurationSetting("ElasticsearchUri"));
            Settings = new ConnectionSettings(Node).RequestTimeout(TimeSpan.FromMinutes(10));
            Client = new ElasticClient(Settings);
            DefaultIndexName = "portalindex";
            _stdFunc = new StandardFunctions();
        }

        private Uri Node { get; }
        private ConnectionSettings Settings { get; }
        private ElasticClient Client { get; }
        private string DefaultIndexName { get; }

        private readonly StandardFunctions _stdFunc;
        private bool _errorOccurred;

        public void CreateIndex()
        {
            //Client.DeleteIndex(DefaultIndexName);
            _stdFunc.AddToActivityLog("Started batch program with switch - createelasticportalindex");
            var nestErrors = "";

            try
            {
                var tonergiantUsers = GetUserDetails("tonergiant", 1);
                var cartridgeMonkeyUsers = GetUserDetails("cartridgemonkey", 2);
                var netgiantUsers = GetUserDetails("netgiant", 3);

                var tgDescriptor = new BulkDescriptor();
                var cmDescriptor = new BulkDescriptor();
                var ngDescriptor = new BulkDescriptor();
                tgDescriptor.Index(DefaultIndexName);
                cmDescriptor.Index(DefaultIndexName);
                ngDescriptor.Index(DefaultIndexName);

                // TG
                tonergiantUsers.ForEach(x => tgDescriptor.Update<UserDetailLookup>(o => o.Doc(x).Upsert(x).Id(x.Account)));
                var tgResponse = Client.Bulk(tgDescriptor);
                _errorOccurred = !_errorOccurred ? !tgResponse.IsValid : _errorOccurred;
                nestErrors += !tgResponse.IsValid ? "TG: " + tgResponse.DebugInformation : "";

                Client.DeleteByQuery<UserDetailLookup>(q => q
                    .Index(DefaultIndexName)
                    .Query(rq => rq
                        .Bool(b => b
                            .MustNot(
                                bs => bs.Ids(x => x.Values(tonergiantUsers.Select(y => y.Account)))
                            )
                            .Must(
                                bs => bs.Match(x => x.Field(y => y.WebsiteId).Query("1"))
                            )
                        )
                    )
                );

                // CM
                cartridgeMonkeyUsers.ForEach(x => cmDescriptor.Update<UserDetailLookup>(o => o.Doc(x).Upsert(x).Id(x.Account)));
                var cmResponse = Client.Bulk(cmDescriptor);
                _errorOccurred = !_errorOccurred ? !cmResponse.IsValid : _errorOccurred;
                nestErrors += !cmResponse.IsValid ? "CM: " + cmResponse.DebugInformation : "";

                Client.DeleteByQuery<UserDetailLookup>(q => q
                    .Index(DefaultIndexName)
                    .Query(rq => rq
                        .Bool(b => b
                            .MustNot(
                                bs => bs.Ids(x => x.Values(cartridgeMonkeyUsers.Select(y => y.Account)))
                            )
                            .Must(
                                bs => bs.Match(x => x.Field(y => y.WebsiteId).Query("2"))
                            )
                        )
                    )
                );

                // NG
                netgiantUsers.ForEach(x => ngDescriptor.Update<UserDetailLookup>(o => o.Doc(x).Upsert(x).Id(x.Account)));
                var ngResponse = Client.Bulk(ngDescriptor);
                _errorOccurred = !_errorOccurred ? !ngResponse.IsValid : _errorOccurred;
                nestErrors += !ngResponse.IsValid ? "NG: " + ngResponse.DebugInformation : "";

                Client.DeleteByQuery<UserDetailLookup>(q => q
                    .Index(DefaultIndexName)
                    .Query(rq => rq
                        .Bool(b => b
                            .MustNot(
                                bs => bs.Ids(x => x.Values(netgiantUsers.Select(y => y.Account)))
                            )
                            .Must(
                                bs => bs.Match(x => x.Field(y => y.WebsiteId).Query("3"))
                            )
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                _stdFunc.AddToActivityLog("**Error** - " + ex.Message);
                _stdFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                _errorOccurred = true;
            }

            if (_errorOccurred)
                _stdFunc.AddToActivityLog("**NEST Error** - " + nestErrors);

            _stdFunc.AddToActivityLog("Finished batch program with switch - createelasticportalindex");
            SaveAndSendLog();
        }

        private List<UserDetailLookup> GetUserDetails(string connectionString, int websiteId)
        {
            const string sql = @"   SELECT  
                                            U.record Record,
                                            U.title Title,
                                            U.forename Forename,
                                            U.surname Surname,
                                            U.email Email,
                                            U.account Account,
                                            C.adr_postcode Postcode,
		                                    LastOrderDate.[date] LastOrderDate,
		                                    CASE C.grp
			                                    WHEN 0 THEN 'CM Web Sales'
			                                    WHEN 1 THEN 'CM Account Cust'
			                                    WHEN 2 THEN 'Dont Use'
			                                    WHEN 3 THEN 'CM Schools'
			                                    WHEN 4 THEN 'Amazon Store'
			                                    WHEN 5 THEN 'NG web Sales'
			                                    WHEN 6 THEN 'NG Account Cust'
			                                    WHEN 7 THEN 'NG Public Sectr'
			                                    WHEN 10 THEN 'TG Web Sales'
			                                    WHEN 11 THEN 'TG Account Cust'
			                                    WHEN 12 THEN 'TG Public Sectr'
			                                    WHEN 14 THEN 'TG MPS'
			                                    WHEN 15 THEN 'TG Amazon'
			                                    WHEN 16 THEN 'Do Not Use'
			                                    WHEN 20 THEN 'Do Not Use'
			                                    WHEN 39 THEN 'Cred Ins withdr'
			                                    WHEN 40 THEN 'Closed >CC'
			                                    WHEN 49 THEN 'Do not Supply'
			                                    WHEN 99 THEN 'Delete'
			                                    WHEN 201 THEN 'TEST TG WEB'
			                                    WHEN 202 THEN 'TEST TG ACCOUNT'
			                                    ELSE 'Unknown'
		                                    END CustomerGroup,
		                                    CASE WHEN LEN(C.adr_additional1) > 0 THEN C.adr_additional1 ELSE '' END +
		                                    CASE WHEN LEN(C.adr_additional2) > 0 THEN + ', ' + C.adr_additional2 ELSE '' END +
		                                    CASE WHEN LEN(C.adr_additional3) > 0 THEN + ', ' + C.adr_additional3 ELSE '' END +
		                                    CASE WHEN LEN(C.adr_additional4) > 0 THEN + ', ' + C.adr_additional4 ELSE '' END +
		                                    CASE WHEN LEN(C.adr_additional5) > 0 THEN + ', ' + C.adr_additional5 ELSE '' END +
		                                    CASE WHEN LEN(C.adr_town) > 0 THEN + ', ' + C.adr_town ELSE '' END +
		                                    CASE WHEN LEN(C.adr_county) > 0 THEN + ', ' + C.adr_county ELSE '' END 
		                                     BillingAddress,
	                                        adr_organisation OrgName
                                    FROM    dbo.Users U
                                    INNER
                                    JOIN    dbo.Customers C ON U.account = C.account
                                    OUTER
                                    APPLY	(SELECT TOP 1 [date] FROM dbo.Customer_Transactions CT WHERE CT.account = C.account ORDER BY CT.date DESC) LastOrderDate
                                    WHERE   U.active = 1";
                
            DataTable dt = new DataTable();
            try
            {
                dt = SQLUtilities.ExecuteReadInline(connectionString, sql, "users").Tables[0];
            }
            catch (Exception ex)
            {
                _stdFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetProductIds");
                _stdFunc.ProcessException(ex);
                _stdFunc.LogActivity();
            }

            return (from DataRow user in dt.Rows
                               select new UserDetailLookup
                               {
                                   Account = user["Account"].ToString(),
                                   Email = user["Email"].ToString(),
                                   Firstname = user["Forename"].ToString(),
                                   Record = user["Record"].ToString(),
                                   Surname = user["Surname"].ToString(),
                                   Title = user["Title"].ToString(),
                                   Postcode = user["Postcode"].ToString().Replace(" ", ""),
                                   FriendlyPostcode = user["Postcode"].ToString(),
                                   LastOrderDate = user["LastOrderDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(user["LastOrderDate"]),
                                   WebsiteId = websiteId,
                                   CustomerGroup = user["CustomerGroup"].ToString(),
                                   BillingAddress = user["BillingAddress"].ToString(),
                                   FullName = $"{user["Forename"]} {user["Surname"]}",
                                   OrgName = user["OrgName"].ToString()
                               }).ToList();
        }

        private void SaveAndSendLog()
        {
            var filePath = _stdFunc.LogActivity("createelasticportalindex");
            if (_errorOccurred)
                _stdFunc.SendSimpleEmail("createelasticportalindex", filePath);
        }

        private class UserDetailLookup
        {
            public string Record { get; set; }
            public string Title { get; set; }
            public string Firstname { get; set; }
            public string Surname { get; set; }
            public string Email { get; set; }
            public string Account { get; set; }
            public string Postcode { get; set; }
            public string FriendlyPostcode { get; set; }
            public int WebsiteId { get; set; }
            public DateTime? LastOrderDate { get; set; }
            public string CustomerGroup { get; set; }
            public string BillingAddress { get; set; }
            public string FullName { get; set; }
            public string OrgName { get; set; }
        }
    }
}

