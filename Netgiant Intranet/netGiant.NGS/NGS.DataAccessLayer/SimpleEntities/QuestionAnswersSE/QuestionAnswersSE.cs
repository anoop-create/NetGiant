using System;

namespace NGS.DataAccessLayer.SimpleEntities.QuestionAnswersSE
{
    [Serializable]
    public class QuestionAnswersSE
    {
        //Fields
        int m_questionAnswerId;
        string m_question;
        string m_answer;
        string m_email;
        DateTime m_askedDate;
        DateTime? m_repliedDate;
        byte m_showOnAllSites;
        int m_relatedGranularityId;
        string m_relatedUserId;
        int m_sourceWebsiteId;
        int m_productId;
        string m_altRef;

        #region Properties

        public int QuestionAnswerID
        {
            get { return m_questionAnswerId; }
            set { m_questionAnswerId = value; }
        }
        
        public string Question
        {
            get { return m_question; }
            set { m_question = value; }
        }

        public string Answer
        {
            get { return m_answer; }
            set { m_answer = value; }
        }

        public string Email
        {
            get { return m_email; }
            set { m_email = value; }
        }

        public DateTime AskedDate
        {
            get { return m_askedDate; }
            set { m_askedDate = value; }
        }

        public DateTime? RepliedDate
        {
            get { return m_repliedDate; }
            set { m_repliedDate = value; }
        }

        public byte ShowOnAllSites
        {
            get { return m_showOnAllSites; }
            set { m_showOnAllSites = value; }
        }

        public int RelatedGranularityID
        {
            get { return m_relatedGranularityId; }
            set { m_relatedGranularityId = value; }
        }

        public string RelatedUserID
        {
            get { return m_relatedUserId; }
            set { m_relatedUserId = value; }
        }

        public int SourceWebsiteID
        {
            get { return m_sourceWebsiteId; }
            set { m_sourceWebsiteId = value; }
        }

        public int ProductID
        {
            get { return m_productId; }
            set { m_productId = value; }
        }

        public string AltRef
        {
            get { return m_altRef; }
            set { m_altRef = value; }
        }

        #endregion
    }
}
