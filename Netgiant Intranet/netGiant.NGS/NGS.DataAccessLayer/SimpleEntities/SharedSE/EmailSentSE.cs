using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGS.DataAccessLayer.SimpleEntities.SharedSE
{
    [Serializable]
    public class EmailSentSE
    {
        int m_emailSentID;
        DateTime m_emailSentDate;
        string m_emailSentTo;
        string m_emailSentBy;
        int m_RelatedQuestionFK;

        #region Properties

        public int EmailSentID
        {
            get { return m_emailSentID; }
            set { m_emailSentID = value; }
        }

        public DateTime EmailSentDate
        {
            get { return m_emailSentDate; }
            set { m_emailSentDate = value; }
        }

        public string EmailSentTo
        {
            get { return m_emailSentTo; }
            set { m_emailSentTo = value; }
        }

        public string RelatedUserID
        {
            get { return m_emailSentBy; }
            set { m_emailSentBy = value; }
        }

        public int RelatedQuestionID
        {
            get { return m_RelatedQuestionFK; }
            set { m_RelatedQuestionFK = value; }
        }

        #endregion
    }
}
