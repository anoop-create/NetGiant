using System;
using System.Collections.Generic;
using NGS.DataAccessLayer.SimpleEntities.QuestionAnswersSE;
using NGS.DataAccessLayer.Services.QuestionAnswersServices;
using NGS.BusinessLayer.BusinessObjects.Shared;
using System.Net;
using System.IO;
using System.Web.UI.WebControls;
using System.Data;

namespace NGS.BusinessLayer.BusinessObjects
{
    [Serializable]
    public class QuestionAnswers
    {
        public QuestionAnswers()
        {
            Init();
        }

        void Init()
        {
            m_simpleEntity = new QuestionAnswersSE();
        }

        #region Public Methods

        public static List<KeyValuePair<int, string>> GetAllGranuality()
        {
            List<KeyValuePair<int, string>> granuality = new List<KeyValuePair<int,string>>();
            QuestionAnswerServices svc = new QuestionAnswerServices();
            granuality = svc.GetAllGranularities();

            return granuality;
        }

        public static QuestionAnswers GetQuestionAnswerByID(int qaID)
        {
            QuestionAnswersSE entity = new QuestionAnswersSE();

            QuestionAnswerServices qaSvc = new QuestionAnswerServices();
            entity = qaSvc.GetQuestionAnswerByID(qaID);

            QuestionAnswers qa = null;

            if (entity != null)
            {
                qa = new QuestionAnswers();
                qa.m_simpleEntity = entity;
            }

            return qa;
        }

        public static QuestionAnswers GetQuestionAnswersByAltRef(string altRef)
        {
            QuestionAnswersSE entity = new QuestionAnswersSE();

            QuestionAnswerServices qaSvc = new QuestionAnswerServices();
            entity = qaSvc.GetQuestionAnswerByAltRef(altRef);

            QuestionAnswers qa = null;

            if (entity != null)
            {
                qa = new QuestionAnswers();
                qa.m_simpleEntity = entity;
            }

            return qa;
        }

        public static List<QuestionAnswers> GetAllQuestionAnswers()
        {
            List<QuestionAnswersSE> list = new List<QuestionAnswersSE>();
            QuestionAnswerServices qaSvc = new QuestionAnswerServices();
            list = qaSvc.GetAllQuestionAnswers();

            List<QuestionAnswers> coll = new List<QuestionAnswers>();
            foreach (QuestionAnswersSE entity in list)
            {
                QuestionAnswers qa = new QuestionAnswers();
                qa.m_simpleEntity = entity;
                coll.Add(qa);
            }   

            return coll;
        }

        public static List<QuestionAnswers> Search(string altRef, string question, byte unAnsweredQuestions)
        {
            List<QuestionAnswersSE> list = new List<QuestionAnswersSE>();
            QuestionAnswerServices qaSvc = new QuestionAnswerServices();
            list = qaSvc.Search(altRef, question, unAnsweredQuestions);

            List<QuestionAnswers> coll = new List<QuestionAnswers>();
            foreach (QuestionAnswersSE entity in list)
            {
                QuestionAnswers qa = new QuestionAnswers();
                qa.m_simpleEntity = entity;
                coll.Add(qa);
            }

            return coll;
        }

        public static List<KeyValuePair<int, int>> GetQAWebsitesMapping(int questionAnswerID)
        {
            QuestionAnswerServices svc = new QuestionAnswerServices();
            return svc.GetWebsiteMappings(questionAnswerID);
        }

        public void Delete()
        {
            QuestionAnswerServices svc = new QuestionAnswerServices();
            svc.Delete(svc.GetQuestionAnswerByID(this.QuestionAnswerID));
        }

        public void Save()
        {
            QuestionAnswerServices svc = new QuestionAnswerServices();
            svc.Save(m_simpleEntity);
        }

        public void AddWebsites(int questionAnswersID, int websiteID, byte showOnAll)
        {
            QuestionAnswerServices svc = new QuestionAnswerServices();
            svc.AddSelectedWebsites(questionAnswersID, websiteID, showOnAll);
        }

        public static KeyValuePair<string, string> GetMembershipUser(string userId)
        {
            KeyValuePair<string, string> member = new KeyValuePair<string, string>();
            QuestionAnswerServices svc = new QuestionAnswerServices();
            member = svc.GetMembershipUser(userId);
            return member;
        }

        #region Email

        public static string GetProductURL(string domain, int productID)
        {
            string url = string.Format("https://{0}/ajaxFunctions.asp?a=getProductURL&b={1}", domain, productID.ToString());

            WebRequest request = WebRequest.Create(url);
            
            if (url.Contains("beta"))
            {
                CredentialCache cc = new CredentialCache();
                cc.Add(new Uri(url), "Basic", new NetworkCredential("webadmin", "shadow"));
                request.Credentials = cc;
            }
            else
            {
                request.Credentials = CredentialCache.DefaultCredentials;
            }

            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            response.Close();

            return responseFromServer;
        }

        public static string GetProductTitle(string domain, int productID)
        {
            string url = string.Format("https://{0}/ajaxFunctions.asp?a=getProductTitle&b={1}", domain, productID.ToString());

            WebRequest request = WebRequest.Create(url);

            if (url.Contains("beta"))
            {
                CredentialCache cc = new CredentialCache();
                cc.Add(new Uri(url), "Basic", new NetworkCredential("webadmin", "shadow"));
                request.Credentials = cc;
            }
            else
            {
                request.Credentials = CredentialCache.DefaultCredentials;
            }

            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            response.Close();

            return responseFromServer;
        }

        public static void SendQAEmail(string domain, int productID, string website, string sendTo, string body)
        {
            string productURL = GetProductURL(domain, productID);
            string productTitle = GetProductTitle(domain, productID);
            
            string brand = website.ToLower().Contains("tonergiant") ? "TonerGiant" : website.ToLower().Contains("cartridgemonkey") ? 
                "CartridgeMonkey" : website.ToLower().Contains("netgiant") ? "NetGiant" : "";

            string supportEmail = brand.Equals("TonerGiant") ? "support@tonergiant.co.uk" : brand.Equals("CartridgeMonkey") ? "support@cartridgemonkey.com" :
                brand.Equals("NetGiant") ? "support@netgiant.com" : "atif.baig@netgiant.com";

            BusinessObjects.Shared.Email email = new Shared.Email();
            email.Body = body;
            email.Subject = string.Format("{0} - You asked a question", brand);
            email.SendFrom = supportEmail;
            email.SendTo = sendTo;
            email.SendQAEmail(productTitle, productURL, brand, supportEmail);
        }

        #endregion

        #endregion

        #region ObjectData Methods

        public static int GetQACount()
        {
            int count;
            QuestionAnswerServices svc = new QuestionAnswerServices();
            count = svc.GetQACount();
            return count;
        }

        public static int GetUnAnsweredQACount()
        {
            int count;
            QuestionAnswerServices svc = new QuestionAnswerServices();
            count = svc.GetUnAnsweredQACount();
            return count;
        }

        public static int GetFilterQACount(string altRef, string filter)
        {
            int count;
            QuestionAnswerServices svc = new QuestionAnswerServices();
            count = svc.GetFilteredQACount(altRef, filter);
            return count;
        }

        public static DataTable GetQASummary(int startRow, int pageSize)
        {
            DataTable dt = new DataTable();
            QuestionAnswerServices svc = new QuestionAnswerServices();
            dt = svc.GetQASummary(startRow, pageSize);
            return dt;
        }

        public static DataTable GetUnAnsweredQASummary(int startRow, int pageSize)
        {
            DataTable dt = new DataTable();
            QuestionAnswerServices svc = new QuestionAnswerServices();
            dt = svc.GetUnAnsweredQASummary(startRow, pageSize);
            return dt;
        }

        public static DataTable GetFilteredQASummary(int startRow, int pageSize, string altRef, string filter)
        {
            DataTable dt = new DataTable();
            QuestionAnswerServices svc = new QuestionAnswerServices();
            dt = svc.GetFilteredQASummary(startRow, pageSize, altRef, filter);
            return dt;
        }

        #endregion

        internal QuestionAnswersSE m_simpleEntity;

        #region Properties

        public int QuestionAnswerID
        {
            get { return m_simpleEntity.QuestionAnswerID; }
            set { m_simpleEntity.QuestionAnswerID = value; }
        }
        
        public string Question
        {
            get { return m_simpleEntity.Question; }
            set { m_simpleEntity.Question = value; }
        }

        public string Answer
        {
            get { return m_simpleEntity.Answer; }
            set { m_simpleEntity.Answer = value; }
        }

        public string Email
        {
            get { return m_simpleEntity.Email; }
            set { m_simpleEntity.Email = value; }
        }

        public DateTime AskedDate
        {
            get { return m_simpleEntity.AskedDate; }
            set { m_simpleEntity.AskedDate = value; }
        }

        public DateTime? RepliedDate
        {
            get { return m_simpleEntity.RepliedDate; }
            set { m_simpleEntity.RepliedDate = value; }
        }

        public int RelatedGranularityID
        {
            get { return m_simpleEntity.RelatedGranularityID; }
            set { m_simpleEntity.RelatedGranularityID = value; }
        }

        public string RelatedUserID
        {
            get { return m_simpleEntity.RelatedUserID; }
            set { m_simpleEntity.RelatedUserID = value; }
        }

        public byte ShowOnAllWebsites
        {
            get { return m_simpleEntity.ShowOnAllSites; }
            set { m_simpleEntity.ShowOnAllSites = value; }
        }

        public int SourceWebsiteID
        {
            get { return m_simpleEntity.SourceWebsiteID; }
            set { m_simpleEntity.SourceWebsiteID = value; }
        }

        public int ProductID
        {
            get { return m_simpleEntity.ProductID; }
            set { m_simpleEntity.ProductID = value; }
        }

        public string AltRef
        {
            get { return m_simpleEntity.AltRef; }
            set { m_simpleEntity.AltRef = value; }
        }

        #endregion
    }
}
