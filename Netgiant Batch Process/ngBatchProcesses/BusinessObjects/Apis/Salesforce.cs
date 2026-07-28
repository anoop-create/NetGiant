using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using System.IO;
using System.Web.Mvc;
using System.Web;
using System.Globalization;
using System.Xml;
using System.Data.Entity;

namespace ngBatchProcesses.BusinessObjects.Apis
{
    public class Salesforce
    {
        public Salesforce()
        {

        }
        [DataContract]
        public class OAuthUsernamePasswordResponse
        {
            [DataMember]
            public string access_token { get; set; }
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string instance_url { get; set; }
            [DataMember]
            public string issued_at { get; set; }
            [DataMember]
            public string signature { get; set; }          
        }

        private static string AccessToken = "";
        public static string JobId = "";
        private static string InstanceUrl = "";
        public bool IsTimeout { get; set; } = false;
        public int BatchCount { get; set; }
        const string ApiVn = "48.0"; // As at 15/4/2025 Version = 63.0

        public bool Authenticate()
        {
            bool isSuccess = false;

            // Just for testing the developer environment, we use the simple username-password OAuth flow.
            // In production environments, make sure to use a stronger OAuth flow, such as User-Agent
            string strContent = "grant_type=password" +
                "&client_id=" + EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "SalesforceApiConsumerKey").FirstOrDefault().settingValue +
                "&client_secret=" + EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "SalesforceApiConsumerSecret").FirstOrDefault().settingValue +
                "&username=" + EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "SalesforceApiUser").FirstOrDefault().settingValue +
                "&password=" + EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "SalesforceApiPassword").FirstOrDefault().settingValue;

            string loginUri = Properties.Settings.Default.Environment == "Live" ? "https://login.salesforce.com" : "https://test.salesforce.com";
            loginUri += "/services/oauth2/token?" + strContent;
            HttpWebRequest request = WebRequest.Create(loginUri) as HttpWebRequest;
            request.Method = "POST";

            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Invalid response from server. Status Code: " + response.StatusCode + ", Description " + response.StatusCode, ErrorCode = "ERROR" });
                        return isSuccess;
                    }

                    // Parse the JSON response and extract the access token and instance URL
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(OAuthUsernamePasswordResponse));
                    OAuthUsernamePasswordResponse objResponse = jsonSerializer.ReadObject(response.GetResponseStream()) as OAuthUsernamePasswordResponse;
                    AccessToken = objResponse.access_token;
                    InstanceUrl = objResponse.instance_url;
                }
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
            }

            if (AccessToken != "")
            {
                isSuccess = true;
            }

            return isSuccess;
        }
        
        public string HttpGet(string objectName, string searchField, string searchValue)
        {
            string requestUri = InstanceUrl + "/services/data/v" + ApiVn + "/sobjects/" + objectName + "/" + searchField + "/" + HttpUtility.UrlEncode(searchValue);
            return HttpGetEx(requestUri);
        }        

        public string HttpGet(string soqlQuery)
        {
            // Ensure the SOQL Query is correctly encoded when this routine is called
            string requestUri = InstanceUrl + "/services/data/v" + ApiVn + "/query/?q=" + soqlQuery;
            return HttpGetEx(requestUri);
        }

        public string HttpGetEx(string requestUri)
        {
            if (!IsTimeout)
            {
                WebRequest req = WebRequest.Create(requestUri);
                req.Method = "GET";
                req.Headers.Add("Authorization: OAuth " + AccessToken);
                //req.Headers.Add("X-SFDC-Session: " + AccessToken);

                // Do the GET request
                WebResponse resp;
                try
                {
                    resp = req.GetResponse();
                    if (resp == null) return "{}";
                    StreamReader sr = new StreamReader(resp.GetResponseStream());
                    return sr.ReadToEnd().Trim();
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        IsTimeout = true;
                        StandardFunctions.WriteException(e);
                    }
                    else
                    {
                        HttpWebResponse webResponse = (HttpWebResponse)e.Response;
                        if (webResponse.StatusCode != HttpStatusCode.NotFound)
                        {
                            StandardFunctions.WriteException(e);
                        }
                    }
                }
            }
            return "{}";
        }

        public bool CreateJob(string objectName, string extId)
        {
            bool isSuccess = false;
            string str = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            str = str + "<jobInfo xmlns=\"http://www.force.com/2009/06/asyncapi/dataload\">";
            str = str + "<operation>upsert</operation>";
            str = str + "<object>" + objectName + "</object>";
            str = str + "<externalIdFieldName>" + extId + "</externalIdFieldName>";
            str = str + "<contentType>CSV</contentType>";
            str = str + "</jobInfo>";
            try
            {
                string requestUri = InstanceUrl + "/services/async/" + ApiVn + "/job";
                XmlDocument reqDoc = new XmlDocument();
                reqDoc.LoadXml(str);
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(reqDoc.InnerXml);
                XmlDocument responseXmlDocument = HttpBulkRequest(bytes, requestUri, "POST");

                JobId = GetXMLItem(responseXmlDocument, "id");

                DateTime createdDate = DateTime.Parse(GetXMLItem(responseXmlDocument, "createdDate"));
                SalesforceBatchJob sbj = new SalesforceBatchJob()
                {
                    JobId = JobId,
                    DateCreated = createdDate,
                    Status = GetXMLItem(responseXmlDocument, "state"),
                    Object = GetXMLItem(responseXmlDocument, "object"),
                    Operation = GetXMLItem(responseXmlDocument, "operation").ToUpper()
                };

                isSuccess = EntityFunctions.SaveSalesForceBatchJob(sbj);
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
            }
            // responseHttpRequest.Dispose();

            return isSuccess;
        }

        private string GetXMLItem(XmlDocument xml, string itemName)
        {
            return xml.GetElementsByTagName(itemName).Item(0).InnerText;
        }

        public bool UpdateJobStatus(string state)
        {
            bool isSuccess = false;
            string str = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
            str = str + "<jobInfo xmlns=\"http://www.force.com/2009/06/asyncapi/dataload\">";
            str = str + "<state>" + state + "</state>";
            str = str + "</jobInfo>";
            string requestUri = InstanceUrl + "/services/async/" + ApiVn + "/job/" + JobId;
            XmlDocument reqDoc = new XmlDocument();
            try
            {
                reqDoc.LoadXml(str);
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(reqDoc.InnerXml);
                XmlDocument responseXmlDocument = HttpBulkRequest(bytes, requestUri, "POST");
                string st = responseXmlDocument.GetElementsByTagName("state").Item(0).InnerText;
                if (st == state)
                {
                    isSuccess = true;
                }
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
            }

            return isSuccess;
        }

        public bool AddBatch(byte[] bytes)
        {
            bool isSuccess = true;
            string requestUri= InstanceUrl + "/services/async/" + ApiVn + "/job/" + JobId + "/batch";
            XmlDocument responseXmlDocument = HttpBulkRequest(bytes, requestUri, "POST");
            BatchCount++;

            return isSuccess;
        }

        public void GetJob(SalesforceBatchJob sbj)
        {
            string requestUri = InstanceUrl + "/services/async/" + ApiVn + "/job/" + sbj.JobId;
            XmlDocument responseXmlDocument = HttpBulkRequest(null, requestUri, "GET");

            if (responseXmlDocument.ChildNodes.Count == 0)
            {
                return;
            }

            int totalBatches = int.Parse(GetXMLItem(responseXmlDocument, "numberBatchesTotal"));
            int completedBatches = int.Parse(GetXMLItem(responseXmlDocument, "numberBatchesCompleted"));
            int failedBatches = int.Parse(GetXMLItem(responseXmlDocument, "numberBatchesFailed"));

            if (totalBatches != completedBatches + failedBatches)
            {
                return;
            }

            sbj.Status = GetXMLItem(responseXmlDocument, "state");
            sbj.BatchesCompleted = int.Parse(GetXMLItem(responseXmlDocument, "numberBatchesCompleted"));
            sbj.BatchesFailed = int.Parse(GetXMLItem(responseXmlDocument, "numberBatchesFailed"));
            sbj.RecordsProcessed = int.Parse(GetXMLItem(responseXmlDocument, "numberRecordsProcessed"));
            sbj.RecordsFailed = int.Parse(GetXMLItem(responseXmlDocument, "numberRecordsFailed"));

            DateTime createdDate = DateTime.Parse(GetXMLItem(responseXmlDocument, "createdDate"));

            if (sbj.Status == "Open" && createdDate < DateTime.Now.AddHours(-2))
            {
                // Job has been open too long - Abort
                JobId = sbj.JobId;
                if (!UpdateJobStatus("Aborted"))
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to abort job, Job ID: " + sbj.JobId, ErrorCode = "ERROR" });
                }
                else
                {
                    sbj.Status = "Aborted";
                }
            }

            return;
        }

        // Used for BULK API calls
        public XmlDocument HttpBulkRequest(byte[] bytes, string requestUri, string method)
        {
            XmlDocument responseXmlDocument = new XmlDocument();
            try
            {
                WebRequest requestHttp = WebRequest.Create(requestUri);
                requestHttp.Method = method;
                //requestHttp.Timeout = Timeout.Infinite;
                requestHttp.ContentType = "text/csv; charset=UTF-8";
                requestHttp.Headers.Add(("X-SFDC-Session: " + AccessToken));
                if (bytes != null)
                {
                    requestHttp.ContentLength = bytes.Length;
                    Stream strmHttpContent = requestHttp.GetRequestStream();
                    strmHttpContent.Write(bytes, 0, bytes.Length);
                    strmHttpContent.Close();

                }
                using (WebResponse responseHttpRequest = requestHttp.GetResponse())
                {
                    Stream responseStream = responseHttpRequest.GetResponseStream();
                    responseXmlDocument.Load(responseStream);
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse wr = e.Response;
                    string msg = new System.IO.StreamReader(wr.GetResponseStream()).ReadToEnd().Trim();
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Protocol error, Msg: " + msg, ErrorCode = "ERROR" });
                }
                StandardFunctions.WriteException(e);
            }
            return responseXmlDocument;
        }

        // Used for REST API calls
        public bool HttpPost(string objectName, object o)
        {
            bool isSuccess = false;
            if (!IsTimeout)
            {                
                string requestUri = InstanceUrl + "/services/data/v" + ApiVn + "/sobjects/" + objectName;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUri);
                req.Method = "POST";
                req.Headers.Add("Authorization: OAuth " + AccessToken);
                req.ContentType = "application/json";

                // Data
                string postData = JsonConvert.SerializeObject(o);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = byteArray.Length;
                Stream dataStream = req.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Get Response
                try
                {
                    HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                    if (response.StatusCode == HttpStatusCode.Created) // 201 = Success
                    {
                        isSuccess = true;
                    }
                }
                catch (WebException e)
                {                    
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        IsTimeout = true;
                        StandardFunctions.WriteException(e);
                    }
                    else
                    {
                        HttpWebResponse webResponse = (HttpWebResponse)e.Response;
                        if (webResponse.StatusCode != HttpStatusCode.NotFound)
                        {
                            StandardFunctions.WriteException(e);
                        }
                    }
                }
            }
            return isSuccess;
        }

        public bool HttpPatch(string objectName, string id, object o)
        {
            bool isSuccess = false;
            if (!IsTimeout)
            {
                string requestUri = InstanceUrl + "/services/data/v" + ApiVn + "/sobjects/" + objectName + "/" + id;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUri);
                req.Method = "PATCH";
                req.Headers.Add("Authorization: Bearer " + AccessToken);
                req.ContentType = "application/json";

                // Data
                string postData = JsonConvert.SerializeObject(o);
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                req.ContentLength = byteArray.Length;
                Stream dataStream = req.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                // Get Response
                try
                {
                    HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                    if (response.StatusCode == HttpStatusCode.NoContent) // 204 = Success
                    {
                        isSuccess = true;
                    }
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        IsTimeout = true;
                        StandardFunctions.WriteException(e);
                    }
                    else
                    {
                        HttpWebResponse webResponse = (HttpWebResponse)e.Response;
                        if (webResponse.StatusCode != HttpStatusCode.NotFound)
                        {
                            StandardFunctions.WriteException(e);
                        }
                    }
                }
            }
            return isSuccess;
        }

        public bool HttpDelete(string tableName, string id)
        {
            bool isSuccess = false;
            if (!IsTimeout)
            {
                string requestUri = InstanceUrl + "/services/data/v" + ApiVn + "/sobjects/" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tableName) + "/" + id;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUri);
                req.Method = "DELETE";
                req.Headers.Add("Authorization: OAuth " + AccessToken);
                req.ContentType = "application/json";

                // Get Response
                try
                {
                    HttpWebResponse response = (HttpWebResponse)req.GetResponse();
                    if (response.StatusCode == HttpStatusCode.NoContent) // 204 = Success
                    {
                        isSuccess = true;
                    }
                }
                catch (WebException e)
                {
                    if (e.Status == WebExceptionStatus.Timeout)
                    {
                        IsTimeout = true;
                        StandardFunctions.WriteException(e);
                    }
                    else
                    {
                        HttpWebResponse webResponse = (HttpWebResponse)e.Response;
                        if (webResponse.StatusCode != HttpStatusCode.NotFound)
                        {
                            StandardFunctions.WriteException(e);
                        }
                    }
                }
            }
            return isSuccess;
        }
    }
}

